using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using Happy_Reader.TranslationEngine;
using JetBrains.Annotations;
using Happy_Reader.View;
using Happy_Reader.View.Tabs;
using Happy_Reader.View.Tiles;
using IthVnrSharpLib;
using static Happy_Apps_Core.StaticHelpers;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private static readonly object HookLock = new();

        public event PropertyChangedEventHandler PropertyChanged;
        public readonly StaticMethods.NotificationEventHandler NotificationEvent;
        private readonly RecentStringList _vndbQueriesList = new(50);
        private readonly RecentStringList _vndbResponsesList = new(50);
        private static MultiLogger Logger => StaticHelpers.Logger;

        public readonly OutputWindow OutputWindowGen;
        private string _statusText;
        private Thread _monitor;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private string _lastUpdateText;
        private User _user;
        private bool _translatePaused;
        private bool _loadingComplete;
        private bool _closing;
        private bool _finalizing;
        private WinAPI.HookProcedureHandle _globalHook;
        private readonly object _exitLock = new();

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                Logger.ToDebug($"StatusText: {_statusText}");
                OnPropertyChanged();
            }
        }

        [NotNull] public UserGamesViewModel UserGamesViewModel { get; }
        [NotNull] public EntriesTabViewModel EntriesViewModel { get; }
        [NotNull] public TranslationTester TestViewModel { get; }
        [NotNull] public VNTabViewModel DatabaseViewModel { get; }
        [NotNull] public CharactersTabViewModel CharactersViewModel { get; }
        [NotNull] public ProducersTabViewModel ProducersViewModel { get; }
        [NotNull] public IthViewModel IthViewModel { get; }
        [NotNull] public SettingsViewModel SettingsViewModel { get; }
        [NotNull] public InformationViewModel InformationViewModel { get; }
        [NotNull] public ApiLogViewModel ApiLogViewModel { get; }
        [NotNull] public List<UserGame> RunningGames { get; } = new();
        private OutputWindowViewModel OutputWindowViewModelGen => (OutputWindowViewModel)OutputWindowGen.DataContext;

        public bool TranslatePaused
        {
            get => _translatePaused;
            set
            {
                _translatePaused = value;
                if (IthViewModel.HookManager != null) IthViewModel.HookManager.Paused = value;
                RunningGames.ForEach(g=>  g.OutputWindow?.ViewModel.OnPropertyChanged(nameof(OutputWindowViewModel.TranslatePaused)));
                IthViewModel.OnPropertyChanged(null);
            }
        }

        public PausableUpdateList<DisplayLog> LogsList { get; } = new();
        public string DisplayUser => $"User: {User?.ToString() ?? "(none)"}";

        public User User
        {
            get => _user;
            private set
            {
                _user = value;
                OnPropertyChanged(nameof(DisplayUser));
            }
        }

        public UserGame UserGame
        {
            get => RunningGames.LastOrDefault();
        }

        /// <summary>
        /// Adds or removes user game from running games list.
        /// </summary>
        /// <param name="userGame">UserGame to be added or removed</param>
        /// <param name="add">True if adding, false if removing</param>
        private void ChangeUserGameInRunningList(UserGame userGame, bool add)
        {
            if (add) RunningGames.Add(userGame);
            else RunningGames.Remove(userGame);
            var userGameForEntry = add ? userGame : RunningGames.LastOrDefault();
            var isVN = userGameForEntry?.HasVN ?? false;
            TestViewModel.EntryGame = new EntryGame(isVN ? userGameForEntry.VNID : (int?)userGameForEntry?.Id, !isVN, false);
            TestViewModel.OnPropertyChanged(nameof(TestViewModel.EntryGame));
            OnPropertyChanged(nameof(UserGame));
            OnPropertyChanged(nameof(RunningGames));
            OnPropertyChanged(nameof(UserGame.RunningStatus));
            if (!RunningGames.Any()) 
            { 
                //restart monitor
                if (_finalizing || _monitor != null && _monitor.IsAlive) return;
                _monitor = GetAndStartMonitorThread();
            }
        }

        public ClipboardManager ClipboardManager;

        public MainWindowViewModel(StaticMethods.NotificationEventHandler showNotification)
        {
            NotificationEvent = showNotification;
            Application.Current.Exit += ExitProcedures;
            StaticMethods.Data = new HappyReaderDatabase(StaticMethods.ReaderDatabaseFile, true);
            SettingsViewModel = Happy_Apps_Core.SettingsJsonFile.Load<SettingsViewModel>(AllSettingsJson);
            StaticMethods.Settings = SettingsViewModel;
            CSettings = SettingsViewModel.CoreSettings;
            IthVnrSharpLib.StaticHelpers.Initialise(IthVnrSettingsJson, StaticHelpers.Logger.ToFile, StaticHelpers.Logger.ToDebug, StaticHelpers.Logger.ToFile);
            StaticMethods.Settings.IthVnrSettings = IthVnrSharpLib.StaticHelpers.Settings;
            SettingsViewModel.TranslatorSettings.CaptureClipboardChanged = CaptureClipboardSettingChanged;
            StaticMethods.AllFilters = Happy_Apps_Core.SettingsJsonFile.Load<FiltersData>(StaticMethods.AllFiltersJson, StaticMethods.SerialiserSettings);
            InformationViewModel = new InformationViewModel();
            ApiLogViewModel = new ApiLogViewModel
            {
                VndbQueries = _vndbQueriesList.Items,
                VndbResponses = _vndbResponsesList.Items
            };
            Translator.Instance = new Translator(StaticMethods.Data, SettingsViewModel.TranslatorSettings);
            UserGamesViewModel = new UserGamesViewModel(this);
            EntriesViewModel = new EntriesTabViewModel();
            TestViewModel = new TranslationTester(this);
            DatabaseViewModel = new VNTabViewModel(this);
            CharactersViewModel = new CharactersTabViewModel(this);
            ProducersViewModel = new ProducersTabViewModel(this);
            IthViewModel = new IthViewModel(this, InitialiseOutputWindowForGame);
            Log.AddToList += AddLogToList;
        }

        public void CaptureClipboardSettingChanged(bool enabled)
        {
            if (ClipboardManager == null) return;
            if (enabled) ClipboardManager.ClipboardChanged += ClipboardChanged;
            else ClipboardManager.ClipboardChanged -= ClipboardChanged;
        }

        private void AddLogToList(Log log)
        {
            Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogsList.Insert(0, new DisplayLog(log));
                OnPropertyChanged(nameof(LogsList));
            });
        }

        public void ExitProcedures(object sender, ExitEventArgs args)
        {
            if (_finalizing) return;
            lock (_exitLock)
            {
                if (_finalizing) return;
                var exitWatch = Stopwatch.StartNew();
                Logger.ToFile($"[{nameof(MainWindowViewModel)}] Starting exit procedures...");
                //we save user game to variable to be used in try block, because HookedProcessOnExited will set the property to null
                var userGame = UserGame;
                if (userGame?.GameHookSettings.IsHooked ?? false) HookedProcessOnExited(userGame, args);
                IthViewModel.Dispose();
                try
                {
                    _closing = true;
                    RunningGames.ForEach(g => g.OutputWindow?.Close());
                    userGame?.SaveTimePlayed(false);
                    Translator.ExitProcedures(StaticMethods.Data.SaveChanges);
                    _monitor?.Join();
                }
                catch (Exception ex)
                {
                    Logger.ToFile(ex);
                }
                _finalizing = true;
                Logger.ToFile($"[{nameof(MainWindowViewModel)}] Completed exit procedures, took {exitWatch.Elapsed}");
            }
        }

        public async Task Initialize(Stopwatch watch, bool initialiseEntries, bool logVerbose)
        {
            StaticHelpers.Logger.LogVerbose = logVerbose;
            StaticHelpers.Logger.ToFile("Starting application...");
            Directory.CreateDirectory(StaticMethods.UserGameIconsFolder);
            await Task.Run(() =>
            {
                StatusText = "Loading data from dump files...";
                DumpFiles.Load();
            });
            await DatabaseViewModel.Initialize();
            await ProducersViewModel.Initialize();
            await CharactersViewModel.Initialize();
            if (initialiseEntries)
            {
                StatusText = "Loading Translation Plugins...";
                SettingsViewModel.TranslatorSettings.LoadTranslationPlugins(TranslationPluginsFolder);
                StaticMethods.MainWindow.SettingsTab.LoadTranslationPlugins(SettingsViewModel.TranslatorSettings.Translators);
                StatusText = "Initialising Translator...";
                Translator.Instance.Initialise(logVerbose);
                StatusText = "Populating Proxies...";
                StaticMethods.Data.PopulateProxies((ex) =>
                {
                    //it's ok to fail here, proxies can be added via the UI anyway.
                    Logger.ToFile(ex);
                    NotificationEvent(this, ex.Message, "Populate Proxies failed", true);
                });
                StatusText = "Loading Entries...";
                EntriesViewModel.SetEntryGames();
                EntriesViewModel.SetEntries();
            }
            await UserGamesViewModel.Initialize();
            InformationViewModel.Initialise(LocalDatabase, StaticMethods.Data);
            LoadLogs();
            SetLastPlayed();
            OnPropertyChanged(nameof(TestViewModel));
            IthViewModel.Initialize(RunTranslation);
            _monitor = GetAndStartMonitorThread();
            _loadingComplete = true;
            StatusText = "Loading complete.";
            NotificationEvent(this, $"Took {watch.Elapsed.ToSeconds()}.", "Loading Complete", true);
        }

        private bool GlobalMouseClick(MouseEventExtArgs args)
        {
            //On global mouse click, when a game is hooked and left button is pressed, disable combining output.
            //By default, when the output window receives multiple messages in a short time, it combines them (this helps when ITHVNR captures text from a single line in multiple parts).
            //If the user progresses through the messages faster than the mentioned short time, they would be erroneously combined.
            //this prevents that from occurring, assuming that every time the user left clicks the mouse, a new line is shown.
            if (args.Button != MouseButton.Left || !args.IsMouseButtonDown || !args.Clicked || (!UserGame?.GameHookSettings.IsHooked ?? true)) return true;
            RunningGames.ForEach(g=>g.OutputWindow.ViewModel.DisableCombine = true);
            return true;
        }

        private Thread GetAndStartMonitorThread()
        {
            var monitor = new Thread(MonitorStart) { IsBackground = true };
            monitor.SetApartmentState(ApartmentState.STA);
            monitor.Start();
            return monitor;
        }

        private void LoadLogs()
        {
            var logs = new List<DisplayLog>();
            var now = DateTime.UtcNow;
            foreach (var group in StaticMethods.Data.Logs.GroupBy(i => (Date: i.GetGroupDate(now), i.Kind, i.AssociatedId)))
            {
                if (group.Key.Kind != LogKind.TimePlayed || group.Key.Date == DateTime.Now.Date)
                {
                    logs.AddRange(group.Where(l => l.AssociatedIdExists).Select(l => new DisplayLog(l)));
                }
                else
                {
                    var templateLog = (Log)group.First().Clone();
                    if (!templateLog.AssociatedIdExists) continue;
                    var totalTimeSpan = new TimeSpan(group.Sum(l => ((TimeSpan)l.ParsedData).Ticks));
                    if (totalTimeSpan.TotalMinutes < 2) continue;
                    templateLog.ParsedData = totalTimeSpan;
                    templateLog.Timestamp = group.Key.Date;
                    logs.Add(new DisplayLog(templateLog));
                }
            }
            LogsList.AddRange(logs.OrderByDescending(l => l.Timestamp));
            OnPropertyChanged(nameof(LogsList));
        }

        private void SetLastPlayed()
        {
            var lastPlayed = StaticMethods.Data.Logs.Where(x => x.Kind == LogKind.TimePlayed).OrderByDescending(x => x.Timestamp).GroupBy(z => z.AssociatedId).Select(x => x.First()).ToList();
            UserGame.LastGamesPlayed.Clear();
            foreach (var log in lastPlayed)
            {
                var userGame = StaticMethods.Data.UserGames.FirstOrDefault(x => x.Id == log.AssociatedId);
                if (userGame != null) UserGame.LastGamesPlayed.Add(log.Timestamp, log.AssociatedId);
            }
        }
        public void SetUser(int userid)
        {
            CSettings.UserID = userid;
            var user = LocalDatabase.Users[CSettings.UserID];
            if (user == null)
            {
                user = new User { Username = CSettings.Username, Id = CSettings.UserID };
                LocalDatabase.Users.Add(user, true, true);
            }
            User = user;
            LocalDatabase.CurrentUser = user;
        }

        private void MonitorStart()
        {
            StaticHelpers.Logger.ToDebug($"MonitorStart starting with ID: {Thread.CurrentThread.ManagedThreadId}");
            while (true)
            {
                if (_closing) return;
                if (RunningGames.Any())
                {
                    StaticHelpers.Logger.ToDebug($"MonitorStart ending with ID: {Thread.CurrentThread.ManagedThreadId}");
                    return;
                }
                try
                {
                    if (MonitorLoop()) return;
                }
                catch (Exception ex)
                {
                    NotificationEvent.Invoke(this, ex.Message, "Error in MonitorStart/Loop", false);
                }
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// Returns true if loop should be stopped.
        /// </summary>
        private bool MonitorLoop()
        {
            var processes = Process.GetProcesses();
            try
            {
                var userGameProcesses = StaticMethods.Data.UserGames.Select(x => x.ProcessName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
                var gameProcess = processes.FirstOrDefault(p => userGameProcesses.Contains(p.ProcessName));
                try
                {
                    if (gameProcess == null || gameProcess.HasExited) return false;
                    var processFileName = StaticMethods.GetProcessFileName(gameProcess);
                    var possibleUserGames = StaticMethods.Data.UserGames.Where(x => x.ProcessName == gameProcess.ProcessName);
                    foreach (var userGame in possibleUserGames)
                    {
                        if (_closing) return true;
                        if (RunningGames.Any()) return true;
                        if (gameProcess.HasExited) continue;
                        if (!userGame.FilePath.Equals(processFileName, StringComparison.InvariantCultureIgnoreCase)) continue;
                        StaticMethods.DispatchIfRequired(() => HookUserGame(userGame, gameProcess, null, false), TimeSpan.FromSeconds(10));
                        return true;
                    }
                }
                catch (Win32Exception ex)
                {
                    //Only part of a ReadProcessMemory or WriteProcessMemory request was completed, Access is denied
                    if (ex.NativeErrorCode != 299 && ex.NativeErrorCode != 5) throw;
                }
                catch (InvalidOperationException) { } //can happen if process is closed after getting reference
            }
            finally
            {
                foreach (var process in processes)
                {
                    if (!RunningGames.Any(g=>g.Process == process)) process.Dispose();
                }
            }
            return false;
        }

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void ClipboardChanged(object sender, EventArgs e)
        {
            if (TranslatePaused || StaticMethods.CtrlKeyIsHeld()) return;
            if (UserGame?.Process == null) return;
            var cpOwner = StaticMethods.GetClipboardOwner();
            var noOwner = cpOwner == null;
            var userGameOwner = cpOwner?.Id == UserGame.Process.Id;
            var allowedOwner = cpOwner != null && SettingsViewModel.TranslatorSettings.ClipboardProcessNames.Contains(cpOwner.ProcessName);
            if (!(noOwner || userGameOwner || allowedOwner)) return; //if process isn't hooked process or in list of allowed names
            var text = RunWithRetries(
                Clipboard.GetText,
                () => Thread.Sleep(10),
                5,
                ex => ex is COMException comEx && (uint)comEx.ErrorCode == WinAPIConstants.CLIPBRD_E_CANT_OPEN);
            IthViewModel.HookManager.ClipboardOutput(text, cpOwner, userGameOwner ? UserGame.DisplayName : null);
            var timeSinceLast = DateTime.UtcNow - _lastUpdateTime;
            if (timeSinceLast.TotalMilliseconds < 100 && _lastUpdateText == text) return;
            Logger.Verbose($"Capturing clipboard from {cpOwner?.ProcessName ?? "??"}\t {DateTime.UtcNow:HH\\:mm\\:ss\\:fff}\ttimeSinceLast:{timeSinceLast.Milliseconds}\t{text}");
            _lastUpdateTime = DateTime.UtcNow;
            _lastUpdateText = text;

            RunTranslation(this, new TextOutputEventArgs(null, text, cpOwner?.ProcessName, true));
        }

        public bool RunTranslation(object sender, TextOutputEventArgs e)
        {
            try
            {
                var userGame = RunningGames.FirstOrDefault(g=>g.Process?.Id == e.TextThread.ProcessId);
                var outputWindow = userGame?.OutputWindow;
                if (TranslatePaused) return false;
                if (userGame == null || outputWindow == null) return false;
                if (StaticMethods.CtrlKeyIsHeld()) return false;
                Logger.Verbose($"{nameof(RunTranslation)} - {e}");
                if (userGame.Process == null ||
                    StaticMethods.DispatchIfRequired(() => outputWindow.ViewModel?.IsClipboardCopy(e) ?? false)) return false;
                TestViewModel.OriginalText = e.Text;
                var translation = Translator.Instance.Translate(User, userGame.EntryGame, e.Text, false, userGame?.GameHookSettings.RemoveRepetition ?? false);
                if (string.IsNullOrWhiteSpace(translation?.Output)) return false;
                StaticMethods.DispatchIfRequired(() => outputWindow.AddTranslation(translation,userGame.Process.Id), new TimeSpan(0, 0, 5));
            }
            catch (Exception ex)
            {
                Logger.ToFile(ex);
                return false;
            }
            return true;
        }

        public void VndbAdvancedAction(string text, bool isQuery)
        {
            if (!SettingsViewModel.GuiSettings.AdvancedMode) return;
            Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
            Application.Current.Dispatcher.Invoke(() => (isQuery ? _vndbQueriesList : _vndbResponsesList).AddWithId(text));
        }

        public void HookUserGame(UserGame userGame, Process process, bool? overrideUseLocaleEmulator, bool doNotHook)
        {
            lock (HookLock)
            {
                //game already running
                if (RunningGames.Contains(userGame) && userGame.Process != null) return;
                if (!File.Exists(userGame.FilePath))
                {
                    StatusText = $"{userGame.DisplayName} - File not found.";
                    return;
                }
                ChangeUserGameInRunningList(userGame, true);
                process ??= LaunchUserGame(userGame, overrideUseLocaleEmulator);
                //process can be closed at any point
                try
                {
                    if (process == null)
                    {
                        NotificationEvent(this,
                            $"Failed to get process for user game '{userGame.DisplayName}'",
                            "Process Launch Failed", false);
                        ChangeUserGameInRunningList(userGame, false);
                        return;
                    }
                    if (process.HasExited)
                    {
                        NotificationEvent(this,
                            "Game Process has exited prematurely, check that executable is the running process.",
                            "Process Has Exited", false);
                        ChangeUserGameInRunningList(userGame, false);
                        return;
                    }
                    userGame.ProcessName = process.ProcessName;
                    userGame.OutputWindow = new OutputWindow(InitialiseOutputWindowForGame, userGame);
                    userGame.SetActiveProcess(process, HookedProcessOnExited);
                    userGame.OnPropertyChanged(null);
                    OnPropertyChanged(nameof(RunningGames));
                    if (userGame.GameHookSettings.HookProcess == HookMode.None || doNotHook) return;
                    if (SettingsViewModel.GuiSettings.HookGlobalMouse) _globalHook = WinAPI.HookMouseEvents(GlobalMouseClick);
                    while (!_loadingComplete) Thread.Sleep(25);
                    SetIthUserGameParametersAndHook(userGame);
                }
                //todo catch more specific exception
                catch (Exception ex)
                {
                    userGame.Process = null;
                    userGame.OutputWindow?.Close();
                    userGame.OutputWindow = null;
                    ChangeUserGameInRunningList(userGame, false);
                    Logger.ToFile(ex);
                    throw;
                }
            }
        }

        private Process LaunchUserGame(UserGame userGame, bool? overrideUseLocaleEmulator)
        {
            bool useLocaleEmulator;
            if (overrideUseLocaleEmulator.HasValue) useLocaleEmulator = overrideUseLocaleEmulator.Value;
            else
            {
                var defaultLaunch = SettingsViewModel.GuiSettings.LaunchMode;
                useLocaleEmulator =     //override is NOT set to normal AND
                                        userGame.LaunchModeOverride != UserGame.LaunchOverrideMode.Normal
                                        //either Override is set to use LE OR
                                        && (userGame.LaunchModeOverride == UserGame.LaunchOverrideMode.UseLe
                                        //game will be hooked and default is to use LE for hooked OR
                                        || (defaultLaunch == GuiSettings.GameLaunchMode.UseLeForHooked && userGame.GameHookSettings.HookProcess != HookMode.None)
                                        //default is to use LE for all
                                        || defaultLaunch == GuiSettings.GameLaunchMode.UseLeForAll);
            }

            var sb = new StringBuilder($"Launching user game '{userGame}', ");
            var useLeString = useLocaleEmulator ? "Locale Emulator" : "Normal";
            if (overrideUseLocaleEmulator.HasValue) sb.Append("(Override) ");
            sb.Append(useLeString);
            Logger.ToFile(sb.ToString());
            return useLocaleEmulator ? userGame.StartProcessThroughLocaleEmulator() :
                !string.IsNullOrWhiteSpace(userGame.LaunchPath) ? userGame.StartProcessThroughProxy() :
                userGame.StartProcess(userGame.FilePath, string.Empty, false);
        }

        private void InitialiseOutputWindowForGame(int processId)
        {
            var userGame = RunningGames.FirstOrDefault(g => g.Process?.Id == processId);
            if(userGame?.Process == null || userGame.OutputWindow == null) return;
            userGame.Process.Refresh();
            if (userGame.OutputWindow.InitialisedWindowLocation) return;
            var success = NativeMethods.GetWindowRect(userGame.Process.MainWindowHandle, out var windowLocation);
            if (!success) userGame.OutputWindow.SetLocation(StaticMethods.OutputWindowStartPosition);
            else
            {
                NativeMethods.RECT outputWindowLocation;
                if (userGame.GameHookSettings.OutputRectangle.IsEmpty)
                {
                    var bottom = windowLocation.Height + windowLocation.Top;
                    outputWindowLocation = new NativeMethods.RECT
                    {
                        Left = windowLocation.Left,
                        Top = bottom - StaticMethods.OutputWindowStartPosition.Height,
                        Right = windowLocation.Width + windowLocation.Left,
                        Bottom = bottom
                    };
                }
                else outputWindowLocation = userGame.GameHookSettings.OutputRectangle.MovePosition(windowLocation);
                userGame.OutputWindow.SetLocation(outputWindowLocation);
            }
            if (!IthViewModel.Finalized && userGame.GameHookSettings.HookProcess == HookMode.VnrHook && !string.IsNullOrWhiteSpace(userGame.GameHookSettings.HookCodes))
            {
                var hookCodes = userGame.GameHookSettings.HookCodes.Split(' ');
                foreach (var hookCode in hookCodes)
                {
                    IthViewModel.Commands?.ProcessCommand(hookCode, userGame.Process.Id);
                }
            }
            userGame.OutputWindow.InitialisedWindowLocation = true;
        }

        private void SetIthUserGameParametersAndHook(UserGame userGame)
        {
            IthViewModel.MergeByHookCode = userGame.GameHookSettings.MergeByHookCode;
            IthViewModel.PrefEncoding = userGame.GameHookSettings.PrefEncoding;
            if (userGame.GameHookSettings.MatchHookCode && !string.IsNullOrWhiteSpace(userGame.GameHookSettings.HookCodes)) IthViewModel.GameHookCodes = userGame.GameHookSettings.HookCodes.Split(' ');
            else
            {
                IthViewModel.GameTextThreads = new ConcurrentList<GameTextThread>();
                IthViewModel.GameTextThreads.AddRange(StaticMethods.Data.GameThreads.Where(t => t.Item.GameId == userGame.Id).Select(t => t.Item));
            }
            switch (userGame.GameHookSettings.HookProcess)
            {
                case HookMode.VnrAgent:
                    {
                        if (!IthViewModel.EmbedHost.Initialized) IthViewModel.EmbedHost.Initialize();
                        IthViewModel.Commands?.ProcessCommand($"/PA{userGame.Process.Id}", userGame.Process.Id);
                        break;
                    }
                case HookMode.VnrHook:
                    {
                        var initialised = IthViewModel.InitialiseVnrHost();
                        if (initialised) IthViewModel.Commands?.ProcessCommand($"/P{userGame.Process.Id}", userGame.Process.Id);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HookedProcessOnExited(object sender, EventArgs eventArgs)
        {
            if (Application.Current?.Dispatcher == null) return;
            var userGame = (UserGame)sender;
            Application.Current.Dispatcher.Invoke(() =>
            {
                userGame.OutputWindow.Close();
                userGame.OutputWindow = null;
                StaticMethods.Data.SaveChanges();
            });
            _globalHook?.Dispose();
            IthViewModel.FinaliseVnrHost(1000);
            userGame.Process = null;
            ChangeUserGameInRunningList(userGame, false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                StaticMethods.MainWindow.UserGamesTabItem.GroupUserGames();
            });
        }

        public void RefreshActiveObjectImages()
        {
            foreach (var tile in UserGamesViewModel.UserGameItems)
            {
                tile.UserGame.OnPropertyChanged(nameof(Database.UserGame.Image));
            }
            foreach (var tile in DatabaseViewModel.Tiles.OfType<VNTile>())
            {
                tile.UpdateImageBinding();
            }
        }

        public void RefreshActiveObjectUserVns()
        {
            foreach (var tile in UserGamesViewModel.UserGameItems)
            {
                tile.UserGame.OnPropertyChanged($"{nameof(Database.UserGame.VN)}");
            }
            foreach (var tile in DatabaseViewModel.Tiles.OfType<VNTile>())
            {
                tile.VN.OnPropertyChanged(nameof(Database.UserGame.VN.UserVN));
            }
        }
    }
}
