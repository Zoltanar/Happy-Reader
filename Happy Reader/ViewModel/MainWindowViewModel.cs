using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using static Happy_Reader.StaticMethods;
using OriginalTextObject = System.Collections.Generic.List<(string Original, string Romaji)>;
using static Happy_Apps_Core.StaticHelpers;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace Happy_Reader
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public User User { get; private set; }
        public ListedVN Game { get; private set; }

        public NotificationEventHandler NotificationEvent;
        public ObservableCollection<dynamic> EntriesList { get; }
        public ObservableCollection<TitledImage> UserGameItems { get; set; } = new ObservableCollection<TitledImage>();
        public ObservableCollection<VNTile> ListedVNs { get; set; } = new ObservableCollection<VNTile>();
        public TranslationTester Tester { get; set; } = new TranslationTester();

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        private OutputWindow _outputWindow;

        private bool _onlyGameEntries;
        private bool _closingDone;
        private Process _hookedProcess;
        public ClipboardManager ClipboardManager;

        public bool OnlyGameEntries
        {
            get => _onlyGameEntries;
            set
            {
                if (_onlyGameEntries == value) return;
                _onlyGameEntries = value;
                SetEntries();
            }
        }

        private bool _captureClipboard;

        public bool CaptureClipboard
        {
            get => _captureClipboard;
            set
            {
                if (value) ClipboardManager.ClipboardChanged += ClipboardChanged;
                else ClipboardManager.ClipboardChanged -= ClipboardChanged;
                _captureClipboard = value;
                OnPropertyChanged();
            }
        }

        public void SetEntries()
        {
            try
            {
                var items = (OnlyGameEntries && Game != null
                    ? (string.IsNullOrWhiteSpace(Game.Series)
                        ? Data.GetGameOnlyEntries(Game)
                        : Data.GetSeriesOnlyEntries(Game))
                    : Data.Entries).ToArray();
                var items2 = items.Select(i => new
                {
                    i.Id,
                    i.User,
                    i.Type,
                    i.Game,
                    Role = i.RoleString,
                    i.Input,
                    i.Output,
                    i.SeriesSpecific,
                    i.Private
                });
                EntriesList.Clear();
                foreach (var item in items2)
                {
                    EntriesList.Add(item);
                }
            }
            catch (Exception ex)
            {
                LogToFile(ex);
            }
        }

        public MainWindowViewModel()
        {
            EntriesList = new ObservableCollection<dynamic>();
            Application.Current.Exit += SaveCacheOnExit;
        }

        public void SaveCacheOnExit(object sender, ExitEventArgs args)
        {
            if (_closingDone) return;
            foreach (var translation in Translator.GetCache())
            {
                if (Data.CachedTranslations.Local.Contains(translation)) continue;
                Data.CachedTranslations.Add(translation);
            }
            Data.SaveChanges();
        }

        public string Translate(string currentText, out OriginalTextObject originalText)
        {
            originalText = new OriginalTextObject();
            if (Game == null) return "User or Game is null.";
            var returned = Translator.Translate(User, Game, currentText, out originalText);
            return returned.Last();
        }

        public void HookV2([NotNull]Process process, UserGame userGame)
        {
            _hookedProcess = process;
            if (_hookedProcess == null) throw new Exception("Process was not started.");
            if (_outputWindow == null) { Application.Current.Dispatcher.Invoke(() => _outputWindow = new OutputWindow()); }
            Game = userGame.VN;
        }

        public void Closing()
        {
            foreach (var translation in Translator.GetCache())
            {
                if (Data.CachedTranslations.Local.Contains(translation)) continue;
                Data.CachedTranslations.Add(translation);
            }
            Data.SaveChanges();
            _outputWindow?.Close();
            _closingDone = true;
        }

        public TitledImage AddGameFile(string file)
        {
            if (Data.UserGames.Select(x => x.FilePath).Contains(file))
            {
                StatusText = "This file has already been added.";
                return null;
            }
            //todo cleanup
            var filename = Path.GetFileNameWithoutExtension(file);
            ListedVN[] fileResults = LocalDatabase.VisualNovels.Where(VisualNovelDatabase.ListVNByNameOrAliasFunc(filename)).ToArray();
            ListedVN[] folderResults = { };
            ListedVN vn = null;
            if (fileResults.Length == 1) vn = fileResults.First();
            else
            {
                var parent = Directory.GetParent(file);
                var folder = parent.Name.Equals("data", StringComparison.OrdinalIgnoreCase) ? Directory.GetParent(parent.FullName).Name : parent.Name;
                folderResults = LocalDatabase.VisualNovels.Where(VisualNovelDatabase.ListVNByNameOrAliasFunc(folder)).ToArray();
                if (folderResults.Length == 1) vn = folderResults.First();
            }
            ListedVN[] allResults = fileResults.Concat(folderResults).ToArray();
            if (vn == null && allResults.Length > 0) vn = allResults[0];
            var userGame = new UserGame(file, vn) { Id = Data.UserGames.Max(x => x.Id) + 1 };
            Data.UserGames.Add(userGame);
            Data.SaveChanges();
            return new TitledImage(userGame);
        }

        public async Task Loaded()
        {
            StatusText = "Loading VN Database...";
            await Task.Run(() =>
            {
                LocalDatabase = new VisualNovelDatabase();
                Settings.UserID = 47063;
                User = LocalDatabase.Users.Single(x => x.Id == Settings.UserID);
                StatusText = "Loading Cached Translations...";
                Data.CachedTranslations.Load();
                Translator.LoadTranslationCache(Data.CachedTranslations.Local);
                StatusText = "Populating Proxies...";
                PopulateProxies();
                StatusText = "Loading Dumpfiles...";
                DumpFiles.Load();
                StatusText = "Opening VNDB Connection...";
                Conn = new VndbConnection(null, null, null);
                Conn.Login(ClientName, ClientVersion);
            });
            StatusText = "Loading Entries...";
            SetEntries();
            StatusText = "Loading User Games...";
            foreach (var game in Data.UserGames.OrderBy(x => x.VNID ?? 0))
            {
                game.VN = game.VNID != null
                    ? LocalDatabase.VisualNovels.SingleOrDefault(x => x.VNID == game.VNID)
                    : null;
                var ti = new TitledImage(game);
                UserGameItems.Add(ti);
                if (game.VN != null) ListedVNs.Add(new VNTile(game.VN));
            }
            StatusText = "Loading finished.";
            var monitor = new Thread(MonitorStart) { IsBackground = true };
            monitor.Start();
            //NotificationEvent.Invoke(this, $"Took {watch.Elapsed:ss\\:fff} (Dumpfiles took {dumpfilesLoadTime:ss\\:fff})", "Loading complete.");
        }

        private void PopulateProxies()
        {
            var array = JArray.Parse(File.ReadAllText(ProxiesJson));
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (JObject item in array)
            {
                var r = item["role"].ToString();
                var i = item["input"].ToString();
                var o = item["output"].ToString();
                var proxy = Data.Entries.SingleOrDefault(x => x.RoleString.Equals(r) && x.Input.Equals(i));
                if (proxy == null)
                {
                    proxy = new Entry { UserId = 0, Type = EntryType.Proxy, RoleString = r, Input = i, Output = o };
                    Data.Entries.Add(proxy);
                    Data.SaveChanges();
                }
            }
        }

        private void MonitorStart()
        {
            NotificationEvent.Invoke(this, $"Processes to monitor: {Data.UserGameProcesses.Length}");
            try
            {
                while (true)
                {
                    var processes = Process.GetProcesses();
                    foreach (var processName in Data.UserGameProcesses)
                    {
                        var process = processes.FirstOrDefault(x => x.ProcessName.Equals(processName));
                        if (process == null) continue;
                        try
                        {
                            if (process.Is64BitProcess()) continue;
                            if (process.HasExited) continue;
                            var userGame =
                                Data.UserGames.FirstOrDefault(x => x.FilePath.Equals(process.MainModule.FileName));
                            if (userGame == null || userGame.Process != null) continue;
                            userGame.Process = process;
                            HookV2(process, userGame);
                        }
                        catch (InvalidOperationException) { } //can happen if process is closed after getting reference
                    }
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                NotificationEvent.Invoke(this, ex.Message, "Error in MonitorStart");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void RemoveUserGame(TitledImage item)
        {
            UserGameItems.Remove(item);
            Data.UserGames.Remove((UserGame)item.DataContext);
            Data.SaveChanges();
        }

        public void ClipboardChanged(object sender, EventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return;
            if (_hookedProcess == null) return;
            var cpOwner = GetClipboardOwner();
            var b1 = cpOwner == null;
            var b2 = cpOwner?.Id == _hookedProcess.Id;
            var b3 = cpOwner?.ProcessName.ToLower().Equals("ithvnr") ?? false;
            if (b1 || !(b2 || b3)) return; //if process isn't hooked process or named ithvnr
            Debug.WriteLine($"Captured clipboard from {cpOwner.ProcessName} ({cpOwner.Id})");
            var rct = GetWindowDimensions(_hookedProcess);
            if (rct.ZeroSized) return; //todo show it somehow or show error.
            if (!_outputWindow.IsLoaded)
            {
                _outputWindow = new OutputWindow();
                _outputWindow.Show();
            }
            try
            {
                var text = Clipboard.GetText();
                var latinOnly = new System.Text.RegularExpressions.Regex(@"[a-zA-Z0-9:/\\\\r\\n .!?,;()""]+");
                if (string.IsNullOrWhiteSpace(text)) return;
                if (latinOnly.IsMatch(text)) return;
                var unRepeatedString = CheckRepeatedString(text);
                var translated = Translate(unRepeatedString, out OriginalTextObject originalText);
                _outputWindow.SetLocation(rct.Left, rct.Bottom, rct.Width);
                _outputWindow.SetText(new TranslationItem(Game.Title, originalText, translated));
            }
            catch (Exception ex)
            {
                LogToFile(ex);
            }
        }

        private string CheckRepeatedString(string text)
        {
            //check if repeated after name
            var firstBracket = text.IndexOfAny(new[] { '「', '『' });
            if (firstBracket > 0)
            {
                var name = text.Substring(0, firstBracket);
                var checkText = text.Substring(firstBracket);
                return name + ReduceRepeatedString(checkText);
            }
            return ReduceRepeatedString(text);
        }

        private string ReduceRepeatedString(string text)
        {
            if (text.Length % 2 != 0) return text;
            var halfLength = text.Length / 2;
            var p1 = text.Substring(0, halfLength);
            var p2 = text.Substring(halfLength);
            if (p1 == p2) return p1;
            return text;
        }

        public void TestTranslation()
        {
            Tester.Test(User, Game);
        }

    }
}
