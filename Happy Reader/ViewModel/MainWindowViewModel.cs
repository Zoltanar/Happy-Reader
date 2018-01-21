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
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Happy_Reader.View;

namespace Happy_Reader.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public User User { get; private set; }
        public UserGame UserGame { get; private set; }

        public StaticMethods.NotificationEventHandler NotificationEvent;
        public ObservableCollection<dynamic> EntriesList { get; } = new ObservableCollection<dynamic>();
        public ObservableCollection<UserGameTile> UserGameItems { get; } = new ObservableCollection<UserGameTile>();
        private readonly RecentStringList _vndbQueriesList = new RecentStringList(50);
        private readonly RecentStringList _vndbResponsesList = new RecentStringList(50);

        private readonly OutputWindow _outputWindow;
        private string _statusText;
        private bool _onlyGameEntries;
        private bool _captureClipboard;
        private Process _hookedProcess;
        public ClipboardManager ClipboardManager;
        private Thread _monitor;

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
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
        public GuiSettings GSettings => StaticMethods.GSettings;
        public BindingList<string> VndbQueries { get; set; }
        public BindingList<string> VndbResponses { get; set; }
        public TranslationTester TestViewModel { get; }
        public VNTabViewModel DatabaseViewModel { get; }
        public bool TranslateOn { get; set; } = true;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            Application.Current.Exit += ExitProcedures;
            VndbQueries = _vndbQueriesList.Items;
            VndbResponses = _vndbResponsesList.Items;
            TestViewModel = new TranslationTester(this);
            DatabaseViewModel = new VNTabViewModel(this);
            OnPropertyChanged(nameof(VndbQueries));
            _outputWindow = new OutputWindow(mainWindow);
        }

        public void SetEntries()
        {
            var items = (OnlyGameEntries && UserGame?.VN != null
                ? (string.IsNullOrWhiteSpace(UserGame?.VN.Series)
                    ? StaticMethods.Data.GetGameOnlyEntries(UserGame?.VN)
                    : StaticMethods.Data.GetSeriesOnlyEntries(UserGame?.VN))
                : StaticMethods.Data.Entries).ToArray();
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
                i.Regex
            });
            Application.Current.Dispatcher.Invoke(() =>
            {
                EntriesList.Clear();
                foreach (var item in items2) EntriesList.Add(item);
            });
        }

        public void ExitProcedures(object sender, ExitEventArgs args)
        {
            _monitor.Abort();
            _outputWindow?.Close();
            UserGame?.SaveTimePlayed(false);
            StaticMethods.SaveTranslationCache();
        }

        public Translation Translate(string currentText)
        {
            if (UserGame == null) return Translation.Error("UserGame is null.");
            Translation translation = Translator.Translate(User, UserGame.VN, currentText);
            return translation;
        }
        
        public UserGameTile AddGameFile(string file)
        {
            if (StaticMethods.Data.UserGames.Select(x => x.FilePath).Contains(file))
            {
                StatusText = "This file has already been added.";
                return null;
            }
            //todo cleanup
            var filename = Path.GetFileNameWithoutExtension(file);
            ListedVN[] fileResults = VisualNovelDatabase.ListVNByNameOrAliasFunc(filename).Compile().Invoke(StaticHelpers.LocalDatabase).ToArray();
            ListedVN[] folderResults = { };
            ListedVN vn = null;
            if (fileResults.Length == 1) vn = fileResults.First();
            else
            {
                var parent = Directory.GetParent(file);
                var folder = parent.Name.Equals("data", StringComparison.OrdinalIgnoreCase) ? Directory.GetParent(parent.FullName).Name : parent.Name;
                folderResults = VisualNovelDatabase.ListVNByNameOrAliasFunc(folder).Compile().Invoke(StaticHelpers.LocalDatabase).ToArray();
                if (folderResults.Length == 1) vn = folderResults.First();
            }
            ListedVN[] allResults = fileResults.Concat(folderResults).ToArray();
            if (vn == null && allResults.Length > 0) vn = allResults[0];
            var userGame = new UserGame(file, vn) { Id = StaticMethods.Data.UserGames.Max(x => x.Id) + 1 };
            StaticMethods.Data.UserGames.Add(userGame);
            StaticMethods.Data.SaveChanges();
            return new UserGameTile(userGame);
        }

        public async Task Initialize(Stopwatch watch)
        {
            CaptureClipboard = GSettings.CaptureClipboardOnStart;
            await DatabaseViewModel.Initialize();
            StaticMethods.Data.UserGames.Load();
            await Task.Run(() =>
            {
                StatusText = "Loading Cached Translations...";
                StaticMethods.Data.CachedTranslations.Load();
                Translator.LoadTranslationCache(StaticMethods.Data.CachedTranslations.Local);
                StatusText = "Populating Proxies...";
                PopulateProxies();
                StatusText = "Loading Dumpfiles...";
                DumpFiles.Load();
                StatusText = "Loading Entries...";
                SetEntries();
                StatusText = "Loading User Games...";
                foreach (var game in StaticMethods.Data.UserGames.Local)
                {
                    game.VN = game.VNID != null
                        ? StaticHelpers.LocalDatabase.VisualNovels.SingleOrDefault(x => x.VNID == game.VNID)
                        : null;
                }
            });
            foreach (var game in StaticMethods.Data.UserGames.Local.OrderBy(x => x.VNID ?? 0)) { UserGameItems.Add(new UserGameTile(game)); }
            TestViewModel.Initialize();
            OnPropertyChanged(nameof(TestViewModel));
            _monitor = new Thread(MonitorStart) { IsBackground = true };
            _monitor.Start();
            StatusText = "Loading complete.";
            NotificationEvent(this, $"Took {watch.Elapsed:ss\\:fff} seconds.", "Loading Complete");
        }

        public async Task SetUser(int userid, bool newId)
        {
            StaticHelpers.CSettings.UserID = userid;
            if (newId)
            {
                await StaticHelpers.LocalDatabase.VisualNovels.ForEachAsync(vn => vn.UserVNId = StaticHelpers.LocalDatabase.UserVisualNovels
                    .SingleOrDefault(x => x.UserId == StaticHelpers.CSettings.UserID && x.VNID == vn.VNID)?.Id);
                await StaticMethods.Data.SaveChangesAsync();
            }
            User = StaticHelpers.LocalDatabase.Users.Single(x => x.Id == StaticHelpers.CSettings.UserID);
        }

        private void PopulateProxies()
        {
            var array = JArray.Parse(File.ReadAllText(StaticMethods.ProxiesJson));
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (JObject item in array)
            {
                var r = item["role"].ToString();
                var i = item["input"].ToString();
                var o = item["output"].ToString();
                var proxy = StaticMethods.Data.Entries.SingleOrDefault(x => x.RoleString.Equals(r) && x.Input.Equals(i));
                if (proxy == null)
                {
                    proxy = new Entry { UserId = 0, Type = EntryType.Proxy, RoleString = r, Input = i, Output = o };
                    StaticMethods.Data.Entries.Add(proxy);
                    StaticMethods.Data.SaveChanges();
                }
            }
        }

        private void MonitorStart()
        {
            //NotificationEvent.Invoke(this, $"Processes to monitor: {StaticMethods.Data.UserGameProcesses.Length}");
            while (true)
            {
                try
                {
                    var processes = Process.GetProcesses();
                    foreach (var processName in StaticMethods.Data.UserGameProcesses)
                    {
                        var process = processes.FirstOrDefault(x => x.ProcessName.Equals(processName));
                        if (process == null) continue;
                        try
                        {
                            if (process.Is64BitProcess()) continue;
                            if (process.HasExited) continue;
                            if (UserGame?.Process != null) continue;
                            var userGame =
                                StaticMethods.Data.UserGames.FirstOrDefault(x => x.FilePath.Equals(process.MainModule.FileName));
                            if (userGame == null || userGame.Process != null) continue;
                            userGame.Process = process;
                            HookUserGame(userGame);
                        }
                        catch (InvalidOperationException) { } //can happen if process is closed after getting reference
                    }
                }
                catch (Exception ex)
                {
                    NotificationEvent.Invoke(this, ex.Message, "Error in MonitorStart");
                }
                Thread.Sleep(5000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void RemoveUserGame(UserGameTile item)
        {
            UserGameItems.Remove(item);
            StaticMethods.Data.UserGames.Remove((UserGame)item.DataContext);
            StaticMethods.Data.SaveChanges();
            OnPropertyChanged(nameof(TestViewModel));
        }

        public void ClipboardChanged(object sender, EventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || !TranslateOn) return;
            if (_hookedProcess == null) return;
            var cpOwner = StaticMethods.GetClipboardOwner();
            var b1 = cpOwner == null;
            var b2 = cpOwner?.Id == _hookedProcess.Id;
            var b3 = cpOwner?.ProcessName.ToLower().Equals("ithvnr") ?? false;
            if (!(b1 || b2 || b3)) return; //if process isn't hooked process or named ithvnr
#if LOGVERBOSE
            Debug.WriteLine($"Captured clipboard from {cpOwner?.ProcessName} ({cpOwner?.Id})");
#endif
            var rct = StaticMethods.GetWindowDimensions(_hookedProcess);
            if (rct.ZeroSized) return; //todo show it somehow or show error.
            if (!_outputWindow.IsVisible) _outputWindow.Show();
            try
            {
                var text = Clipboard.GetText();
                if (text.Length > GSettings.MaxClipboardSize) return; //todo show error
                var latinOnly = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9:/\\\\r\\n .!?,;()""]+$");
                if (string.IsNullOrWhiteSpace(text)) return;
                if (latinOnly.IsMatch(text)) return;
                var unRepeatedString = CheckRepeatedString(text);
                var translation = Translate(unRepeatedString);
                if (string.IsNullOrWhiteSpace(translation.Output)) return;
                _outputWindow.SetLocation(rct.Left, rct.Bottom, rct.Width);
                _outputWindow.SetText(translation);
            }
            catch (Exception ex)
            {
                StaticHelpers.LogToFile(ex);
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

        public void VndbAdvancedAction(string text, bool isQuery)
        {
            Application.Current.Dispatcher.Invoke(() => (isQuery ? _vndbQueriesList : _vndbResponsesList).AddWithId(text));
        }

        public Process HookUserGame(UserGame userGame)
        {
            UserGame = userGame;
            var process = StaticMethods.StartProcess(userGame.FilePath);
            if (userGame.ProcessName == null)
            {
                var game = StaticMethods.Data.UserGames.Single(x => x.Id == userGame.Id);
                game.ProcessName = process.ProcessName;
                StaticMethods.Data.SaveChanges();
            }
            _hookedProcess = process;
            _hookedProcess.EnableRaisingEvents = true;
            _hookedProcess.Exited += HookedProcessOnExited;
            return process;
        }

        private void HookedProcessOnExited(object sender, EventArgs eventArgs)
        {
            Application.Current.Dispatcher.Invoke(() => _outputWindow.Hide());
        }

        public void DebugButton()
        {
        }
    }
}
