using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;
using Happy_Reader.Interop;
using Happy_Apps_Core;
using static Happy_Reader.StaticMethods;
using OriginalTextObject = System.Collections.Generic.List<(string Original, string Romaji)>;
using static Happy_Apps_Core.StaticHelpers;
using JetBrains.Annotations;

namespace Happy_Reader
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public User User { get; private set; }
        public Game Game { get; private set; }

#pragma warning disable 67
        public NotificationEventHandler NotificationEvent;
#pragma warning restore 67
        public ObservableCollection<HookInfo> ContextsList { get; }
        public ObservableCollection<ComboBoxItem> ProcessList { get; }
        public ObservableCollection<dynamic> EntriesList { get; }
        public ObservableCollection<TitledImage> UserGameItems { get; set; } = new ObservableCollection<TitledImage>();

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
        private bool _eventsSetUp;
        private bool _closingDone;
        private Process _hookedProcess;

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

        private void SetEntries()
        {
            var items = OnlyGameEntries ? (string.IsNullOrWhiteSpace(Game.Series) ? GetGameOnlyItems() : GetSeriesOnlyItems()) : GetItems();
            EntriesList.Clear();
            foreach (var item in items)
            {
                EntriesList.Add(item);
            }

            IQueryable<dynamic> GetItems()
            {
                var entryItems = from i in Data.Entries
                                 from user in Data.Users
                                 where i.UserId == user.Id
                                 from game in Data.Games
                                 where i.GameId == game.Id
                                 select new
                                 {
                                     i.Id,
                                     User = user.Id,
                                     i.Type,
                                     Game = game.RomajiTitle ?? game.Title,
                                     Role = i.RoleString,
                                     i.Input,
                                     i.Output,
                                     i.SeriesSpecific,
                                     i.Private
                                 };
                return entryItems;
            }
            IQueryable<dynamic> GetGameOnlyItems()
            {
                var entryItems = from i in Data.Entries
                                 where i.GameId == Game.Id
                                 from user in Data.Users
                                 where i.UserId == user.Id
                                 from game in Data.Games
                                 where i.GameId == game.Id
                                 select new
                                 {
                                     i.Id,
                                     User = user.Id,
                                     i.Type,
                                     Game = game.RomajiTitle ?? game.Title,
                                     Role = i.RoleString,
                                     i.Input,
                                     i.Output,
                                     i.SeriesSpecific,
                                     i.Private
                                 };
                return entryItems;
            }
            IQueryable<dynamic> GetSeriesOnlyItems()
            {
                var series = Data.Games.Where(i => i.Series == Game.Series).Select(i => i.Id).ToList();
                var entryItems = from i in Data.Entries
                                 where series.Contains(i.GameId.Value)
                                 from user in Data.Users
                                 where i.UserId == user.Id
                                 from game in Data.Games
                                 where i.GameId == game.Id
                                 select new
                                 {
                                     i.Id,
                                     User = user.Id,
                                     i.Type,
                                     Game = game.RomajiTitle ?? game.Title,
                                     Role = i.RoleString,
                                     i.Input,
                                     i.Output,
                                     i.SeriesSpecific,
                                     i.Private
                                 };
                return entryItems;
            }
        }

        public MainWindowViewModel()
        {
            ContextsList = new ObservableCollection<HookInfo>();
            EntriesList = new ObservableCollection<dynamic>();
            ProcessList = new ObservableCollection<ComboBoxItem>();
            SetProcesses();
            SetEntries();
            HookInfo.SaveAllowedStatus += SaveAllowedStatus;
            Application.Current.Exit += SaveCacheOnExit;
        }
        
        public void SaveCacheOnExit(object sender, ExitEventArgs args)
        {
            if (_closingDone) return;
            foreach (var translation in HRGoogleTranslate.GoogleTranslate.GetCache())
            {
                if (Data.CachedTranslations.Local.Contains(translation)) continue;
                Data.CachedTranslations.Add(translation);
            }
            Data.SaveChanges();
        }

        private void SetProcesses()
        {
            ProcessList.Clear();
            foreach (var process in Process.GetProcesses())
            {
                if (ProcessIsBanned(process.ProcessName)) continue;
                ProcessList.Add(new ComboBoxItem
                {
                    Content = process.ProcessName,
                    Tag = process
                });
            }
        }


        public bool TrySaveSettings(string user, string game, out string response)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                response = "Username is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(game))
            {
                response = "Game name is required.";
                return false;
            }
            User = Data.GetUser(user);
            if (User == null)
            {
                response = "User not found.";
                return false;
            }
            Game = Data.GetGameByName(game);
            if (Game == null)
            {
                response = "User not found.";
                return false;
            }
            response = "Settings saved.";
            return true;
        }

        public string Translate(string currentText, out OriginalTextObject originalText)
        {
            originalText = new OriginalTextObject();
            if (User == null || Game == null) return "User or Game is null.";
            var output = Translator.Translate(User, Game, currentText, out OriginalTextObject original);
            originalText = original;
            return output;
        }

        public bool SetGame(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var filestream = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(filestream);
                    var hashHex = GetHashString(hash);
                    var gameFile = Data.GameFiles.SingleOrDefault(i => i.MD5 == hashHex);
                    if (gameFile == null || gameFile.GameId == 0)
                    {
                        Game = Data.Games.FirstOrDefault(i => i.Title == _hookedProcess.MainWindowTitle);
                        if (Game == null) CreateAndSetNewGame(_hookedProcess.MainWindowTitle);
                        if (gameFile == null)
                        {
                            gameFile = new GameFile { GameId = Game.Id, MD5 = hashHex };
                            Data.GameFiles.Add(gameFile);
                        }
                        else gameFile.GameId = Game.Id;
                        Data.SaveChanges();
                    }
                    else
                    {
                        Game = Data.Games.FirstOrDefault(i => i.Id == gameFile.GameId);
                        if (Game == null) CreateAndSetNewGame(_hookedProcess.MainWindowTitle, gameFile.GameId);
                    }

                }
            }
            return Game != null;

            void CreateAndSetNewGame(string name, long id = 0)
            {
                Game = new Game
                {
                    Title = name,
                    Timestamp = DateTime.UtcNow,
                    Wiki = "HRCreated",
                    Id = id == 0 ? Data.Games.Max(i => i.Id) + 1 : id
                };
                Data.Games.Add(Game);
                Data.SaveChanges();
            }
        }

        public static string GetHashString(byte[] hash)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public void BanProcess([NotNull]string processName)
        {
            for (int index = ProcessList.Count - 1; index >= 0; index--)
            {
                if ((ProcessList[index].Tag as Process)?.ProcessName == processName) ProcessList.RemoveAt(index);
            }
            StaticMethods.BanProcess(processName);
        }

        public void Hook([NotNull]Process process)
        {
            TextHookInteropCompat.TextHookInit();
            var res11 = TextHook.instance.init(false);
            if (res11 == 1001)
            {
                TextHookInteropCompat.TextHookDisconnect();
                var res12 = TextHook.instance.init(false);
                if (res12 == 1001) throw new Exception("Hook already running.");
            }
            _hookedProcess = process;
            if (_hookedProcess == null) throw new Exception("Process was not started.");
            _hookedProcess.WaitForInputIdle(5000);
            if (!TextHook.instance.connect(_hookedProcess.Id)) /* TODO log error*/ return;
            ConnectSuccess();
            if (_outputWindow == null) _outputWindow = new OutputWindow();
            var gameSet = SetGame(process.MainModule.FileName);
            if (!gameSet) throw new Exception("Couldn't set or create game.");

            foreach (var hook in Game.Hooks)
            {
                ContextsList.Add(new HookInfo(hook.Context, hook.Name, PrintSentence, hook.Allowed));
            }
        }

        private readonly object _dataLock = new object();
        private void SaveAllowedStatus(object sender, AllowedStatusEventArgs args)
        {
            lock (_dataLock)
            {
                var hook = Data.GameHooks.SingleOrDefault(i => i.GameId == Game.Id && i.Context == args.ContextId);
                if (hook == null)
                {
                    hook = new GameHook
                    {
                        Allowed = args.Allowed,
                        Context = args.ContextId,
                        GameId = Game.Id,
                        Name = args.Name
                    };
                    Data.GameHooks.Add(hook);
                }
                else hook.Allowed = args.Allowed;
                Data.SaveChanges();
            }

        }

        private void ConnectSuccess()
        {
            if (_eventsSetUp) return;
            _eventsSetUp = true;
            TextHook.instance.onNewContext += ctx =>
            {
                ctx.onSentence += Ctx_onSentence;
            };
            TextHook.instance.onDisconnect += () =>
            {
                //webBrowser1.callScript("disconnect");
                //TranslationForm.instance.Close();
            };
        }

        private readonly object _gccLock = new object();

        private void Ctx_onSentence(TextHookContext sender, string text)
        {
            var context = GetOrCreateContext(new HookInfo(sender.context, sender.name, PrintSentence));
            if (context == null || !context.Allowed) return;
            Debug.WriteLine($"Time {DateTime.Now:O}\t{text}");
            context.Timer.Stop();
            context.Timer.Start();
            context.AddText(text);
            context.Text.Append(text);

            HookInfo GetOrCreateContext(HookInfo hookInfo)
            {
                HookInfo result;
                lock (_gccLock)
                {
                    result = ContextsList.SingleOrDefault(i => i.ContextId == hookInfo.ContextId);
                    if (result != null) return result;
                    if (hookInfo.Name.Equals("UserHook", StringComparison.OrdinalIgnoreCase)) hookInfo.Allowed = true;
                    Application.Current.Dispatcher.Invoke(() => ContextsList.Add(hookInfo));
                }
                result = hookInfo;
                return result;
            }
        }

        private void SetOutputText(TranslationItem translationItem)
        {
            var windowHandle = _hookedProcess.MainWindowHandle;
            var rct = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(windowHandle, ref rct);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _outputWindow.SetLocation(rct.Left, rct.Bottom, rct.Right - rct.Left);
                _outputWindow.Show();
                _outputWindow.SetText(translationItem);
            });
        }

        private void PrintSentence(object sender, ElapsedEventArgs e)
        {
            var timer = (NamedTimer)sender;
            timer.Stop();
            var context = ContextsList.Single(i => i.ContextId == timer.Context);
            context.DisplayText = context.Text.ToString();
            var translated = Translate(context.Text.ToString(), out OriginalTextObject originalText);
            Console.WriteLine($@"[{context.ContextId:x}]{context.Name}: {context.Text} > {translated}");
            SetOutputText(new TranslationItem(context, originalText, translated));
            context.ClearText();
        }

        public void Closing()
        {
            foreach (var translation in HRGoogleTranslate.GoogleTranslate.GetCache())
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
            var filename = Path.GetFileNameWithoutExtension(file);
            ListedVN[] fileResults = LocalDatabase.VNList.Where(VNDatabase.ListVNByNameOrAliasFunc(filename)).ToArray();
            ListedVN[] folderResults = { };
            ListedVN vn = null;
            if (fileResults.Length == 1)
            {
                vn = fileResults.First();
            }
            else
            {
                folderResults = LocalDatabase.VNList.Where(VNDatabase.ListVNByNameOrAliasFunc(Directory.GetParent(file).Name)).ToArray();
                if (folderResults.Length == 1) vn = folderResults.First();
            }
            if (vn != null)
            {
                var userGame = new UserGame(file, vn){Id = Data.UserGames.Max(x=>x.Id)+1};
                Data.UserGames.Add(userGame);
                Data.SaveChanges();
                return new TitledImage(userGame);
            }
            if (folderResults.Length + fileResults.Length == 0)
            {
                //TODO ask user which of the results is the correct one
                //vn = fileResults[0];
                return null;
            }
            return null;
        }

        public async Task Loaded()
        {
            Stopwatch watch = Stopwatch.StartNew();
            TimeSpan dumpfilesLoadTime = new TimeSpan();
            StatusText = "Loading Cached Translations...";
            await Task.Run(() =>
            {
                Data.CachedTranslations.Load();
                HRGoogleTranslate.GoogleTranslate.LoadCache(Data.CachedTranslations.Local);
                StatusText = "Loading Dumpfiles...";
                var s1 = watch.Elapsed;
                DumpFiles.Load();
                dumpfilesLoadTime = s1 - watch.Elapsed;
                Settings.UserID = 47063;
                Settings.Username = "zolty";
                LocalDatabase = new VNDatabase(@"C:\Users\Gusty\Documents\VNPC-By Zoltanar\Visual Novel Database\HappySearchObjectClasses\Database\Test.sqlite");

                StatusText = "Opening VNDB Connection...";
                Conn = new VndbConnection(null, null, null);
                Conn.Login(ClientName, ClientVersion);
            });
            StatusText = "Loading User Games...";
            foreach (var game in Data.UserGames.OrderBy(x => x.VNID ?? 0))
            {
                game.VN = game.VNID != null ? LocalDatabase.VNList.SingleOrDefault(x => x.VNID == game.VNID) : GetNotFoundVN(game);
                var ti = new TitledImage(game);
                UserGameItems.Add(ti);
            }
            StatusText = "Loading finished.";
            var monitor = new Thread(MonitorStart) { IsBackground = true };
            monitor.Start();
            NotificationEvent.Invoke(this, $"Took {watch.Elapsed:ss\\:fff} (Dumpfiles took {dumpfilesLoadTime:ss\\:fff})", "Loading complete.");
        }

        private void MonitorStart()
        {
            while (true)
            {
                foreach (var process in Process.GetProcesses().Where(x => !ProcessIsBanned(x.ProcessName)))
                {
                    if (process.Is64BitProcess()) continue;
                    try
                    {
                        var userGame =
                            Data.UserGames.FirstOrDefault(x => x.FilePath.Equals(process.MainModule.FileName));
                        if (userGame == null || userGame.Process != null) continue;
                        userGame.Process = process;
                    }
                    catch (Win32Exception) { }
                }
                Thread.Sleep(5000);
            }
        }

        private ListedVN GetNotFoundVN(UserGame game) => new ListedVN(game.UserDefinedName ?? game.FileName);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
