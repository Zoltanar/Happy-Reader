using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
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
		private static readonly object HookLock = new object();

		public event PropertyChangedEventHandler PropertyChanged;
		public StaticMethods.NotificationEventHandler NotificationEvent;
		private readonly RecentStringList _vndbQueriesList = new RecentStringList(50);
		private readonly RecentStringList _vndbResponsesList = new RecentStringList(50);
		private MultiLogger Logger => Happy_Apps_Core.StaticHelpers.Logger;

		public readonly OutputWindow OutputWindow;
		private string _statusText;
		private bool _captureClipboard;
		private bool _showFileNotFound;
		public ClipboardManager ClipboardManager;
		private Thread _monitor;
		private DateTime _lastUpdateTime = DateTime.MinValue;
		private string _lastUpdateText;
		private User _user;
		private UserGame _userGame;
		private bool _translatePaused;
		private bool _loadingComplete;
		private bool _closing;
		private bool _finalizing;

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

		public bool ShowFileNotFound
		{
			get => _showFileNotFound;
			set
			{
				if (value == _showFileNotFound) return;
				_showFileNotFound = value;
				LoadUserGames().RunSynchronously();
				OnPropertyChanged();
			}
		}

		[NotNull] public EntriesTabViewModel EntriesViewModel { get; }
		[NotNull] public TranslationTester TestViewModel { get; }
		[NotNull] public Translator Translator { get; }
		[NotNull] public VNTabViewModel DatabaseViewModel { get; }
		[NotNull] public CharactersTabViewModel CharactersViewModel { get; }
		[NotNull] public ProducersTabViewModel ProducersViewModel { get; }
		[NotNull] public FiltersViewModel FiltersViewModel { get; }
		[NotNull] public IthViewModel IthViewModel { get; }
		[NotNull] public SettingsViewModel SettingsViewModel { get; }
		[NotNull] public ApiLogViewModel ApiLogViewModel { get; }
		//todo make a new UserControl and ViewModel for entries.

		public bool TranslatePaused
		{
			get => _translatePaused;
			set
			{
				_translatePaused = value;
				if (IthViewModel.HookManager != null) IthViewModel.HookManager.Paused = value;
				((OutputWindowViewModel)OutputWindow.DataContext).OnPropertyChanged(nameof(OutputWindowViewModel.TranslatePaused));
				IthViewModel.OnPropertyChanged(null);
			}
		}

		public PausableUpdateList<DisplayLog> LogsList { get; } = new PausableUpdateList<DisplayLog>();
		public ObservableCollection<UserGameTile> UserGameItems { get; } = new ObservableCollection<UserGameTile>();
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
				OnPropertyChanged(nameof(DisplayGame));
			}
		}

		public MainWindowViewModel()
		{
			Application.Current.Exit += ExitProcedures;
			SettingsViewModel = new SettingsViewModel(CSettings, StaticMethods.GuiSettings, StaticMethods.TranslatorSettings);
			ApiLogViewModel = new ApiLogViewModel
			{
				VndbQueries = _vndbQueriesList.Items,
				VndbResponses = _vndbResponsesList.Items
			};
			Translator = new Translator(StaticMethods.Data);
			Translation.Translator = Translator;
			EntriesViewModel = new EntriesTabViewModel(() => UserGame);
			TestViewModel = new TranslationTester(this);
			FiltersViewModel = new FiltersViewModel();
			DatabaseViewModel = new VNTabViewModel(this, FiltersViewModel.Filters, FiltersViewModel.PermanentFilter);
			CharactersViewModel = new CharactersTabViewModel();
			ProducersViewModel = new ProducersTabViewModel(this);
			IthViewModel = new IthViewModel(this);
			OutputWindow = new OutputWindow();
			Log.AddToList += AddLogToList;
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
			var ithFinalize = new Thread(() => IthViewModel.Finalize(null, null)) { IsBackground = true };
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

		public UserGameTile AddGameFile(string file)
		{
			var userGame = StaticMethods.Data.UserGames.FirstOrDefault(x => x.FilePath == file);
			if (userGame != null)
			{
				StatusText = $"This file has already been added. ({userGame.DisplayName})";
				return null;
			}
			var vn = StaticMethods.ResolveVNForFile(file);
			userGame = new UserGame(file, vn) { Id = StaticMethods.Data.UserGames.Max(x => x.Id) + 1 };
			StaticMethods.Data.UserGames.Add(userGame);
			StaticMethods.Data.SaveChanges();
			StatusText = vn == null ? "File was added without VN." : $"File was added as {userGame.DisplayName}.";
			return new UserGameTile(userGame);
		}

		public async Task Initialize(Stopwatch watch, RoutedEventHandler defaultUserGameGrouping, bool initialiseIthVnr, bool initialiseEntries, bool noApiTranslation, bool logVerbose)
		{
			StaticHelpers.Logger.LogVerbose = logVerbose;
			CaptureClipboard = SettingsViewModel.TranslatorSettings.CaptureClipboardOnStart;
			await Task.Run(() =>
			{
				StatusText = "Loading data from dump files...";
				DumpFiles.Load();
			});
			await DatabaseViewModel.Initialize();
			await ProducersViewModel.Initialize();
			await CharactersViewModel.Initialize(this);
			if (initialiseEntries)
			{
				StatusText = "Loading Cached Translations...";
				await Task.Run(() =>
				{
					var cacheLoadWatch = Stopwatch.StartNew();
					Translator.SetCache(noApiTranslation, logVerbose);
					StaticHelpers.Logger.ToDebug($"Loaded cached translations in {cacheLoadWatch.ElapsedMilliseconds} ms");
				});
				StatusText = "Populating Proxies...";
				PopulateProxies();
				StatusText = "Loading Entries...";
				EntriesViewModel.SetEntries();
			}
			StatusText = "Loading User Games...";
			await Task.Yield();
			await LoadUserGames();
			LoadLogs();
			SetLastPlayed();
			defaultUserGameGrouping(null, null);
			TestViewModel.Initialize();
			OnPropertyChanged(nameof(TestViewModel));
			if (initialiseIthVnr)
			{
				StatusText = "Initializing ITHVNR...";
				IthViewModel.Initialize(RunTranslation, GetPreferredHookCode);
			}
			_monitor = GetAndStartMonitorThread();
			_loadingComplete = true;
			StatusText = "Loading complete.";
			NotificationEvent(this, $"Took {watch.Elapsed.ToSeconds()}.", "Loading Complete");
		}

		private Thread GetAndStartMonitorThread()
		{
			var monitor = new Thread(MonitorStart) { IsBackground = true };
			monitor.SetApartmentState(ApartmentState.STA);
			monitor.Start();
			return monitor;
		}

		public async Task LoadUserGames()
		{
			UserGameItems.Clear();
			IEnumerable<UserGame> orderedGames = null;
			await Task.Run(() =>
		 {
			 StaticMethods.Data.UserGames.Load();
			 foreach (var game in StaticMethods.Data.UserGames.Local)
			 {
				 if (game.VNID != null)
				 {
					 game.VN = LocalDatabase.VisualNovels[game.VNID.Value];
					 //if game has vn and vn is not already marked as owned, this prevents overwriting CurrentlyOwned with PastOwned,
					 //if multiple user games have the same VN but the later one has been deleted.
					 if (game.VN != null && game.VN.IsOwned != OwnedStatus.CurrentlyOwned) game.VN.IsOwned = game.FileExists ? OwnedStatus.CurrentlyOwned : OwnedStatus.PastOwned;
				 }
			 }
			 orderedGames = StaticMethods.Data.UserGames.Local.OrderBy(x => x.VNID ?? 0).ToList();
			 if (!ShowFileNotFound) orderedGames = orderedGames.Where(og => og.FileExists);
		 });
			foreach (var game in orderedGames) { UserGameItems.Add(new UserGameTile(game)); }
			OnPropertyChanged(nameof(UserGameItems));
		}

		public void LoadLogs()
		{
			StaticMethods.Data.Logs.Load();
			var logs = new List<DisplayLog>();
			var now = DateTime.UtcNow;
			foreach (var group in StaticMethods.Data.Logs.Local.GroupBy(i => (Date: i.GetGroupDate(now), i.Kind, i.AssociatedId)))
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
			var lastPlayed = StaticMethods.Data.Logs.Local.Where(x => x.Kind == LogKind.TimePlayed).OrderByDescending(x => x.Timestamp).GroupBy(z => z.AssociatedId).Select(x => x.First()).ToList();
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
			var array = JArray.Parse(File.ReadAllText(StaticMethods.ProxiesJson));
			// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
			foreach (JObject item in array)
			{
				// ReSharper disable PossibleNullReferenceException
				var r = item["role"].ToString();
				var i = item["input"].ToString();
				var o = item["output"].ToString();
				// ReSharper restore PossibleNullReferenceException
				var proxy = StaticMethods.Data.Entries.SingleOrDefault(x => x.RoleString.Equals(r) && x.Input.Equals(i));
				if (proxy != null) continue;
				proxy = new Entry { UserId = 0, Type = EntryType.Proxy, RoleString = r, Input = i, Output = o };
				StaticMethods.Data.Entries.Add(proxy);
				StaticMethods.Data.SaveChanges();
			}
		}

		private void MonitorStart()
		{
			StaticHelpers.Logger.ToDebug($"MonitorStart starting with ID: {Thread.CurrentThread.ManagedThreadId}");
			StaticMethods.Data.UserGames.Load();
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
				var userGameProcesses = StaticMethods.Data.UserGames.Local.Select(x => x.ProcessName).ToArray();
				var gameProcess = processes.FirstOrDefault(p => userGameProcesses.Contains(p.ProcessName));
				try
				{
					if (gameProcess == null || gameProcess.HasExited) return false;
					var processFileName = StaticMethods.GetProcessFileName(gameProcess);
					var possibleUserGames = StaticMethods.Data.UserGames.Local.Where(x => x.ProcessName == gameProcess.ProcessName);
					foreach (var userGame in possibleUserGames)
					{
						if (_closing) return true;
						if (UserGame?.Process != null) return true;
						if (gameProcess.HasExited) continue;
						if (!userGame.FilePath.Equals(processFileName, StringComparison.InvariantCultureIgnoreCase)) continue;
						DispatchIfRequired(() => HookUserGame(userGame, gameProcess, false), TimeSpan.FromSeconds(10));
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
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public void RemoveUserGame(UserGameTile item)
		{
			UserGameItems.Remove(item);
			StaticMethods.Data.UserGames.Remove(item.UserGame);
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(TestViewModel));
		}

		public void RemoveUserGame(UserGame item)
		{
			var tile = UserGameItems.Single(x => x.UserGame == item);
			RemoveUserGame(tile);
		}

		public void ClipboardChanged(object sender, EventArgs e)
		{
			if (TranslatePaused || CtrlKeyIsHeld()) return;
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
				(ex) => ex is COMException comEx && (uint)comEx.ErrorCode == 0x800401D0); //0x800401D0 = CLIPBRD_E_CANT_OPEN
			var timeSinceLast = DateTime.UtcNow - _lastUpdateTime;
			if (timeSinceLast.TotalMilliseconds < 100 && _lastUpdateText == text) return;
			Logger.Verbose($"Capturing clipboard from {cpOwner?.ProcessName ?? "??"}\t {DateTime.UtcNow:HH\\:mm\\:ss\\:fff}\ttimeSinceLast:{timeSinceLast.Milliseconds}\t{text}");
			_lastUpdateTime = DateTime.UtcNow;
			_lastUpdateText = text;
			RunTranslation(this, new TextOutputEventArgs(null, text, cpOwner?.ProcessName, true));
		}

		public bool RunTranslation(object sender, TextOutputEventArgs e)
		{
			if (e.TextThread?.IsConsole ?? false) return false;
			if (string.IsNullOrWhiteSpace(e.Text) || e.Text == "\r\n") return false;
			try
			{
				if (TranslatePaused) return false;
				bool blockTranslate = DispatchIfRequired(CtrlKeyIsHeld);
				if (blockTranslate) return false;
				Logger.Verbose($"{nameof(RunTranslation)} - {e}");
				if (UserGame.Process == null) return false;
				if ((sender as TextThread)?.IsConsole ?? false) return false;
				TestViewModel.OriginalText = e.Text;
				var translation = Translator.Translate(User, UserGame?.VN, e.Text, false);
				if (string.IsNullOrWhiteSpace(translation?.Output))
				{
					//todo report error
					return false;
				}
				DispatchIfRequired(() => OutputWindow.AddTranslation(translation), new TimeSpan(0, 0, 5));
			}
			catch (Exception ex)
			{
				Logger.ToFile(ex);
				return false;
			}
			return true;
		}

		private static void DispatchIfRequired(Action action, TimeSpan timeout)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			if (Application.Current.Dispatcher.CheckAccess()) action();
			else Application.Current.Dispatcher.Invoke(action, timeout);
		}

		private static T DispatchIfRequired<T>(Func<T> action)
		{
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			return Application.Current.Dispatcher.CheckAccess() ? action() : Application.Current.Dispatcher.Invoke(action);
		}

		public void VndbAdvancedAction(string text, bool isQuery)
		{
			if (!StaticMethods.GuiSettings.AdvancedMode) return;
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
					if (UserGame.ProcessName == null)
					{
						var game = StaticMethods.Data.UserGames.Single(x => x.Id == UserGame.Id);
						game.ProcessName = process.ProcessName;
						StaticMethods.Data.SaveChanges();
					}
					UserGame.SetActiveProcess(process, HookedProcessOnExited);
					TestViewModel.Game = UserGame.VN;
					UserGame.MoveOutputWindow = (r) => OutputWindow.MoveByDifference(r);
					if (!UserGame.HookProcess) return;
					while (!_loadingComplete) Thread.Sleep(25);
					OutputWindow.SetLocation(UserGame.OutputRectangle);
					SetIthUserGameParameters();
				}
				//todo catch more specific exception
				catch (Exception ex)
				{
					if (UserGame != null) UserGame.Process = null;
					UserGame = null;
					Logger.ToFile(ex);
					throw;
				}
			}
		}

		private void SetIthUserGameParameters()
		{
			IthViewModel.MergeByHookCode = UserGame.MergeByHookCode;
			IthViewModel.PrefEncoding = UserGame.PrefEncoding;
			IthViewModel.Commands?.ProcessCommand($"/P{UserGame.Process.Id}", 0);
			if (!string.IsNullOrWhiteSpace(UserGame.HookCode))
				IthViewModel.Commands?.ProcessCommand(UserGame.HookCode, UserGame.Process.Id);
		}

		private void HookedProcessOnExited(object sender, EventArgs eventArgs)
		{
			UserGame.OutputRectangle = OutputWindow.GetRectangle();
			Debug.Assert(Application.Current.Dispatcher != null, "Application.Current.Dispatcher != null");
			Application.Current.Dispatcher.Invoke(() => OutputWindow.Hide());
			UserGame = null;
			//restart monitor
			if (_monitor != null && _monitor.IsAlive) return;
			_monitor = GetAndStartMonitorThread();
		}

		// ReSharper disable UnusedTupleComponentInReturnValue
		public (string HookCode, string HookFull, Encoding PrefEncoding) GetPreferredHookCode(uint processId)
		{
			return processId != UserGame?.Process?.Id ? (null, null, null) : (UserGame.HookCode, UserGame.DefaultHookFull, UserGame.PrefEncoding);
		}
		// ReSharper restore UnusedTupleComponentInReturnValue

		public void RefreshActiveObjectImages()
		{
			foreach (var tile in UserGameItems)
			{
				tile.UserGame.OnPropertyChanged(nameof(Database.UserGame.Image));
			}
			foreach (var tile in DatabaseViewModel.ListedVNs)
			{
				tile.UpdateImageBinding();
			}
		}

		private static bool CtrlKeyIsHeld() => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
	}
}
