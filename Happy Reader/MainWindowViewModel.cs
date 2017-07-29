using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;
using Happy_Reader.Interop;
using Happy_Reader.Properties;
using static Happy_Reader.StaticMethods;

namespace Happy_Reader
{
    internal class MainWindowViewModel
    {
        private User _user;
        public Game Game { get; private set; }
        public HappyReaderDatabase Data { get; private set; } = new HappyReaderDatabase();

        public ObservableCollection<HookInfo> ContextsList { get; }
        public ObservableCollection<ComboBoxItem> ProcessList { get; }
        public ObservableCollection<dynamic> EntriesList { get; }

        private OutputWindow _outputWindow;

        private bool _onlyGameEntries;
        private bool _eventsSetUp;
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
            _user = Data.GetUser(user);
            if (_user == null)
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

        public string Translate(string currentText)
        {
            if (_user == null || Game == null) return "User or Game is null.";
            return Data.Translate(_user, "en", Game, currentText);
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
                            gameFile = new GameFile {GameId = Game.Id, MD5 = hashHex};
                            Data.GameFiles.Add(gameFile);
                        }
                        else gameFile.GameId = Game.Id;
                        Data.SaveChanges();
                    }
                    else
                    {
                        Game = Data.Games.FirstOrDefault(i => i.Id == gameFile.GameId);
                    }
                    
                }
            }
            return Game != null;

            void CreateAndSetNewGame(string name)
            {
                Game = new Game
                {
                    Title = name,
                    Timestamp = DateTime.UtcNow,
                    Wiki = "HRCreated",
                    Id = Data.Games.Max(i=>i.Id)+1
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
            if(!TextHook.instance.connect(_hookedProcess.Id)) /* TODO log error*/ return;
            ConnectSuccess();
            if(_outputWindow == null) _outputWindow = new OutputWindow();
            SetGame(process.MainModule.FileName);
            foreach (var hook in Game.Hooks)
            {
                ContextsList.Add(new HookInfo(hook.Context,hook.Name,PrintSentence, hook.Allowed));
            }
        }

        private void SaveAllowedStatus(object sender, AllowedStatusEventArgs args)
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
                lock(_gccLock)
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

        private void SetOutputText(string character, string text)
        {
            var windowHandle = _hookedProcess.MainWindowHandle;
            var rct = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(windowHandle, ref rct);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _outputWindow.SetLocation(rct.Left, rct.Bottom, rct.Right - rct.Left);
                _outputWindow.Show();
                _outputWindow.SetText(character, text);
            });
        }

        private void SetOutputText(string text)
        {
            var windowHandle = _hookedProcess.MainWindowHandle;
            NativeMethods.RECT rct = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(windowHandle, ref rct);
            Application.Current.Dispatcher.Invoke(() =>
            {
                _outputWindow.SetLocation(rct.Left, rct.Bottom, rct.Right - rct.Left);
                _outputWindow.Show();
                _outputWindow.SetText(text);
            });
        }

        private void PrintSentence(object sender, ElapsedEventArgs e)
        {
            var timer = (NamedTimer)sender;
            timer.Stop();
            var context = ContextsList.Single(i => i.ContextId == timer.Context);
            context.DisplayText = context.Text.ToString();
            var translated = Translate(context.Text.ToString());
            Console.WriteLine($"[{context.ContextId:x}]{context.Name}: {context.Text} > {translated}");
            if (context.Parts.Count == 2) SetOutputText(context.Parts[0], context.Parts[1]);
            else SetOutputText($"[{context.ContextId:x}]{context.Name}: {translated}");
            context.ClearText();
        }
    }
}
