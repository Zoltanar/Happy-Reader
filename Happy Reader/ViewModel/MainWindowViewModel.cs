using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Happy_Reader.View;
using Happy_Reader.View.Tabs;
using Happy_Reader.View.Tiles;
using HRGoogleTranslate;
using IthVnrSharpLib;
using static Happy_Apps_Core.StaticHelpers;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private static readonly object HookLock = new();

		public event PropertyChangedEventHandler PropertyChanged;
		public StaticMethods.NotificationEventHandler NotificationEvent;
		private readonly RecentStringList _vndbQueriesList = new(50);
		private readonly RecentStringList _vndbResponsesList = new(50);
		private static MultiLogger Logger => StaticHelpers.Logger;

		public readonly OutputWindow OutputWindow;
		private string _statusText;
		private Thread _monitor;
		private DateTime _lastUpdateTime = DateTime.MinValue;
		private string _lastUpdateText;
		private User _user;
		private UserGame _userGame;
		private bool _translatePaused;
		private bool _loadingComplete;
		private bool _closing;
		private bool _finalizing;
		private WinAPI.HookProcedureHandle _globalHook;

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
		[NotNull] public Translator Translator { get; }
		[NotNull] public VNTabViewModel DatabaseViewModel { get; }
		[NotNull] public CharactersTabViewModel CharactersViewModel { get; }
		[NotNull] public ProducersTabViewModel ProducersViewModel { get; }
		[NotNull] public IthViewModel IthViewModel { get; }
		[NotNull] public SettingsViewModel SettingsViewModel { get; }
		[NotNull] public InformationViewModel InformationViewModel { get; }
		[NotNull] public ApiLogViewModel ApiLogViewModel { get; }
		public OutputWindowViewModel OutputWindowViewModel => (OutputWindowViewModel)OutputWindow.DataContext;

		public bool TranslatePaused
		{
			get => _translatePaused;
			set
			{
				_translatePaused = value;
				if (IthViewModel.HookManager != null) IthViewModel.HookManager.Paused = value;
				OutputWindowViewModel.OnPropertyChanged(nameof(OutputWindowViewModel.TranslatePaused));
				IthViewModel.OnPropertyChanged(null);
			}
		}

		public PausableUpdateList<DisplayLog> LogsList { get; } = new();
		public string DisplayUser => $"User: {User?.ToString() ?? "(none)"}";
		public string DisplayGame => $"Game: {UserGame?.ToString() ?? "(none)"}";

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
			get => _userGame;
			private set
			{
				_userGame = value;
				TestViewModel.Game = _userGame?.VN;
				TestViewModel.OnPropertyChanged(nameof(TestViewModel.Game));
				OnPropertyChanged();
				OnPropertyChanged(nameof(UserGame.RunningStatus));
			}
		}

		public ClipboardManager ClipboardManager;

		public MainWindowViewModel()
		{
			Application.Current.Exit += ExitProcedures;
			StaticMethods.Data = new HappyReaderDatabase(StaticMethods.ReaderDatabaseFile, true);
			SettingsViewModel = Happy_Apps_Core.SettingsJsonFile.Load<SettingsViewModel>(StaticMethods.AllSettingsJson);
			StaticMethods.Settings = SettingsViewModel;
			CSettings = SettingsViewModel.CoreSettings;
			SettingsViewModel.TranslatorSettings.CaptureClipboardChanged = CaptureClipboardSettingChanged;
			StaticMethods.AllFilters = Happy_Apps_Core.SettingsJsonFile.Load<FiltersData>(StaticMethods.AllFiltersJson, StaticMethods.SerialiserSettings);
			InformationViewModel = new InformationViewModel();
			ApiLogViewModel = new ApiLogViewModel
			{
				VndbQueries = _vndbQueriesList.Items,
				VndbResponses = _vndbResponsesList.Items
			};
			Translator = new Translator(StaticMethods.Data);
			Translation.Translator = Translator;
			UserGamesViewModel = new UserGamesViewModel(this);
			EntriesViewModel = new EntriesTabViewModel(() => UserGame);
			TestViewModel = new TranslationTester(this);
			DatabaseViewModel = new VNTabViewModel(this);
			CharactersViewModel = new CharactersTabViewModel(this);
			ProducersViewModel = new ProducersTabViewModel(this);
			IthViewModel = new IthViewModel(this, InitialiseOutputWindowForGame);
			OutputWindow = new OutputWindow(InitialiseOutputWindowForGame);
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
			var exitWatch = Stopwatch.StartNew();
			Logger.ToDebug($"[{nameof(MainWindowViewModel)}] Starting exit procedures...");
			_finalizing = true;
			if (UserGame?.IsHooked ?? false) HookedProcessOnExited(sender, args);
			var ithFinalize = new Thread(() => IthViewModel.Finalize(null, null)) { IsBackground = true, Name = "IthVnrFinalizeThread"};
			ithFinalize.Start();
			bool terminated = ithFinalize.Join(5000); //false if timed out
			if (!terminated) { }
			try
			{
				_closing = true;
				OutputWindow?.Close();
				UserGame?.SaveTimePlayed(false);
				GoogleTranslate.ExitProcedures(StaticMethods.Data.SaveChanges);
				_monitor?.Join();
			}
			catch (Exception ex)
			{
				Logger.ToFile(ex);
			}
			Logger.ToDebug($"[{nameof(MainWindowViewModel)}] Completed exit procedures, took {exitWatch.Elapsed}");
		}

		public async Task Initialize(Stopwatch watch, RoutedEventHandler defaultUserGameGrouping, bool initialiseEntries, bool noApiTranslation, bool logVerbose)
		{
			StaticHelpers.Logger.LogVerbose = logVerbose;
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
				StatusText = "Loading Cached Translations...";
				await Task.Run(() =>
				{
					var cacheLoadWatch = Stopwatch.StartNew();
					Translator.SetCache(noApiTranslation, logVerbose, SettingsViewModel.TranslatorSettings);
					StaticHelpers.Logger.ToDebug($"Loaded cached translations in {cacheLoadWatch.ElapsedMilliseconds} ms");
				});
				StatusText = "Populating Proxies...";
				PopulateProxies();
				StatusText = "Loading Entries...";
				EntriesViewModel.SetEntries();
			}
			await UserGamesViewModel.Initialize();
			InformationViewModel.Initialise(LocalDatabase, StaticMethods.Data);
			LoadLogs();
			SetLastPlayed();
			defaultUserGameGrouping(null, null);
			TestViewModel.Initialize();
			OnPropertyChanged(nameof(TestViewModel));
			if (SettingsViewModel.GuiSettings.HookIthVnr)
			{
				StatusText = "Initializing ITHVNR...";
				string errorMessage;
				try
				{
					IthViewModel.Initialize(RunTranslation, out errorMessage);
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile(ex);
					errorMessage = ex.Message;
				}
				if (!string.IsNullOrWhiteSpace(errorMessage)) IthViewModel.DisplayThreads.Add(new TextBlock(new Run(errorMessage)));
			}
			_monitor = GetAndStartMonitorThread();
			_loadingComplete = true;
			StatusText = "Loading complete.";
			NotificationEvent(this, $"Took {watch.Elapsed.ToSeconds()}.", "Loading Complete");
		}

		private bool GlobalMouseClick(MouseEventExtArgs args)
		{
			//On global mouse click, when a game is hooked and left button is pressed, disable combining output.
			//By default, when the output window receives multiple messages in a short time, it combines them (this helps when ITHVNR captures text from a single line in multiple parts).
			//If the user progresses through the messages faster than the mentioned short time, they would be erroneously combined.
			//this prevents that from occurring, assuming that every time the user left clicks the mouse, a new line is shown.
			if (args.Button != MouseButton.Left || !args.IsMouseButtonDown || !args.Clicked || (!UserGame?.IsHooked ?? true)) return true;
			OutputWindowViewModel.DisableCombine = true;
			return true;
		}

		private Thread GetAndStartMonitorThread()
		{
			var monitor = new Thread(MonitorStart) { IsBackground = true };
			monitor.SetApartmentState(ApartmentState.STA);
			monitor.Start();
			return monitor;
		}

		public void LoadLogs()
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
			User = LocalDatabase.Users[CSettings.UserID];
			LocalDatabase.CurrentUser = User;
		}

		private void PopulateProxies()
		{
			var newProxies = new List<Entry>();
			try
			{
				if (!File.Exists(StaticMethods.ProxiesJson)) return;
				var array = JArray.Parse(File.ReadAllText(StaticMethods.ProxiesJson));
				// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
				foreach (var item in array.OfType<JObject>())
				{
					// ReSharper disable PossibleNullReferenceException
					var r = item["role"].ToString();
					var i = item["input"].ToString();
					var o = item["output"].ToString();
					// ReSharper restore PossibleNullReferenceException
					var proxy = StaticMethods.Data.Entries.FirstOrDefault(x =>
						x.Type == EntryType.Proxy && x.RoleString.Equals(r) && x.Input.Equals(i));
					if (proxy != null) continue;
					newProxies.Add(new Entry { Type = EntryType.Proxy, RoleString = r, Input = i, Output = o });
				}
			}
			catch (Exception ex)
			{
				//it's ok to fail here, proxies can be added via the UI anyway.
				Logger.ToFile(ex);
				NotificationEvent(this, ex.Message, "Populate Proxies failed");
			}
			StaticMethods.Data.AddEntries(newProxies);
		}

		private void MonitorStart()
		{
			StaticHelpers.Logger.ToDebug($"MonitorStart starting with ID: {Thread.CurrentThread.ManagedThreadId}");
			while (true)
			{
				if (_closing) return;
				if (UserGame?.Process != null)
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
					NotificationEvent.Invoke(this, ex.Message, "Error in MonitorStart/Loop");
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
				var userGameProcesses = StaticMethods.Data.UserGames.Select(x => x.ProcessName).ToArray();
				var gameProcess = processes.FirstOrDefault(p => userGameProcesses.Contains(p.ProcessName));
				try
				{
					if (gameProcess == null || gameProcess.HasExited) return false;
					var processFileName = StaticMethods.GetProcessFileName(gameProcess);
					var possibleUserGames = StaticMethods.Data.UserGames.Where(x => x.ProcessName == gameProcess.ProcessName);
					foreach (var userGame in possibleUserGames)
					{
						if (_closing) return true;
						if (UserGame?.Process != null) return true;
						if (gameProcess.HasExited) continue;
						if (!userGame.FilePath.Equals(processFileName, StringComparison.InvariantCultureIgnoreCase)) continue;
						StaticMethods.DispatchIfRequired(() => HookUserGame(userGame, gameProcess, false), TimeSpan.FromSeconds(10));
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
					if (process != UserGame?.Process) process.Dispose();
				}
			}
			return false;
		}

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public void ClipboardChanged(object sender, EventArgs e)
		{
			if (TranslatePaused || StaticMethods.CtrlKeyIsHeld()) return;
			if (UserGame?.Process == null) return;
			var cpOwner = StaticMethods.GetClipboardOwner();
			var b1 = cpOwner == null;
			var b2 = cpOwner?.Id == UserGame.Process.Id;
			var b3 = cpOwner?.ProcessName.ToLower().Equals("ithvnr") ?? false;
			var b4 = cpOwner?.ProcessName.ToLower().Equals("ithvnrsharp") ?? false;
			if (!(b1 || b2 || b3 || b4)) return; //if process isn't hooked process or named ithvnr
			var text = RunWithRetries(
				Clipboard.GetText,
				() => Thread.Sleep(10),
				5,
				ex => ex is COMException comEx && (uint)comEx.ErrorCode == WinAPIConstants.CLIPBRD_E_CANT_OPEN);
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
				if (TranslatePaused) return false;
				if (StaticMethods.CtrlKeyIsHeld()) return false;
				Logger.Verbose($"{nameof(RunTranslation)} - {e}");
				if (UserGame.Process == null) return false;
				TestViewModel.OriginalText = e.Text;
				var translation = Translator.Translate(User, UserGame?.VN, e.Text, false, UserGame?.RemoveRepetition ?? false);
				if (string.IsNullOrWhiteSpace(translation?.Output)) return false;
				StaticMethods.DispatchIfRequired(() => OutputWindow.AddTranslation(translation), new TimeSpan(0, 0, 5));
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

		public void HookUserGame(UserGame userGame, Process process, bool useLocaleEmulator)
		{
			lock (HookLock)
			{
				if (UserGame == userGame && UserGame.Process != null) return;
				if (!File.Exists(userGame.FilePath))
				{
					StatusText = $"{userGame.DisplayName} - File not found.";
					return;
				}
				UserGame = userGame;
				process ??= useLocaleEmulator ? UserGame.StartProcessThroughLocaleEmulator() :
					!string.IsNullOrWhiteSpace(UserGame.LaunchPath) ? UserGame.StartProcessThroughProxy() :
					UserGame.StartProcess(UserGame.FilePath, string.Empty, false);
				//process can be closed at any point
				try
				{
					if (process.HasExited)
					{
						NotificationEvent(this,
							$"Process for {UserGame} has exited prematurely, check that executable is the running process.",
							"Process has exited.");
						UserGame = null;
						return;
					}
					UserGame.ProcessName ??= process.ProcessName;
					UserGame.SetActiveProcess(process, HookedProcessOnExited);
					TestViewModel.Game = UserGame.VN;
					UserGame.OnPropertyChanged(null);
					OnPropertyChanged(nameof(UserGame));
					UserGame.OutputWindow = OutputWindow;
					if (UserGame.HookProcess == UserGame.HookMode.None) return;
					if (SettingsViewModel.GuiSettings.HookGlobalMouse) _globalHook = WinAPI.HookMouseEvents(GlobalMouseClick);
					while (!_loadingComplete) Thread.Sleep(25);
					SetIthUserGameParameters();
				}
				//todo catch more specific exception
				catch (Exception ex)
				{
					if (UserGame != null)
					{
						UserGame.Process = null;
						UserGame.OutputWindow = null;
						UserGame = null;
					}
					Logger.ToFile(ex);
					throw;
				}
			}
		}

		private void InitialiseOutputWindowForGame()
		{
			if (OutputWindow.InitialisedWindowLocation) return;
			UserGame.Process.Refresh();
			var success = NativeMethods.GetWindowRect(UserGame.Process.MainWindowHandle, out var windowLocation);
			if (!success) OutputWindow.SetLocation(StaticMethods.OutputWindowStartPosition);
			else
			{
				var outputWindowLocation = UserGame.OutputRectangle.MovePosition(windowLocation);
				OutputWindow.SetLocation(outputWindowLocation);
			}
			if (!IthViewModel.Finalized && UserGame.HookProcess == UserGame.HookMode.VnrHook && !string.IsNullOrWhiteSpace(UserGame.HookCode))
			{
				IthViewModel.Commands?.ProcessCommand(UserGame.HookCode, UserGame.Process.Id);
			}
			OutputWindow.InitialisedWindowLocation = true;
		}

		private void SetIthUserGameParameters()
		{
			IthViewModel.MergeByHookCode = UserGame.MergeByHookCode;
			IthViewModel.PrefEncoding = UserGame.PrefEncoding;
			IthViewModel.GameTextThreads = StaticMethods.Data.GameThreads.Where(t => t.Item.GameId == UserGame.Id).Select(t => t.Item).ToArray();
			if (UserGame.HookProcess == UserGame.HookMode.VnrAgent)
			{
				if (!IthViewModel.EmbedHost.Initialized) IthViewModel.EmbedHost.Initialize();
				IthViewModel.Commands?.ProcessCommand($"/PA{UserGame.Process.Id}", UserGame.Process.Id);
			}
			else IthViewModel.Commands?.ProcessCommand($"/P{UserGame.Process.Id}", UserGame.Process.Id);
		}

		private void HookedProcessOnExited(object sender, EventArgs eventArgs)
		{
			if (Application.Current?.Dispatcher == null) return;
			Application.Current.Dispatcher.Invoke(() =>
			{
				OutputWindow.InitialisedWindowLocation = false;
				OutputWindow.Hide();
				StaticMethods.Data.SaveChanges();
			});
			_globalHook?.Dispose();
			UserGame.Process = null;
			UserGame.OutputWindow = null;
			UserGame = null;
			//restart monitor
			if (_finalizing || _monitor != null && _monitor.IsAlive) return;
			_monitor = GetAndStartMonitorThread();
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
