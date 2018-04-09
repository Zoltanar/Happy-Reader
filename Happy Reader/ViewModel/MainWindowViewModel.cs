using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using IthVnrSharpLib;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.ViewModel
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private static readonly object HookLock = new object();

		public event PropertyChangedEventHandler PropertyChanged;
		public StaticMethods.NotificationEventHandler NotificationEvent;
		private readonly RecentStringList _vndbQueriesList = new RecentStringList(50);
		private readonly RecentStringList _vndbResponsesList = new RecentStringList(50);

		private readonly OutputWindow _outputWindow;
		private string _statusText;
		private bool _onlyGameEntries;
		private bool _captureClipboard;
		private Process _hookedProcess;
		public ClipboardManager ClipboardManager;
		private Thread _monitor;
		private DateTime _lastUpdateTime = DateTime.MinValue;
		private string _lastUpdateText;
		private User _user;
		private UserGame _userGame;
		private bool _translateOn = true;
		private bool _loadingComplete;
		private bool _closing;
		private bool _finalizing;

		public string StatusText
		{
			get => _statusText;
			set
			{
				_statusText = value;
				Debug.WriteLine($"{DateTime.Now:O} StatusText: {_statusText}");
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
		public Translator Translator { get; }
		public VNTabViewModel DatabaseViewModel { get; }
		public FiltersViewModel FiltersViewModel { get; }
		public IthViewModel IthViewModel { get; }

		public bool TranslateOn
		{
			get => _translateOn;
			set
			{
				_translateOn = value;
				if (GSettings.PauseIthVnrAndTranslate)
				{
					IthViewModel.HookManager.Paused = !value;
					IthViewModel.OnPropertyChanged(nameof(IthViewModel.HookManager));
				}
			}
		}

		public ObservableCollection<DisplayEntry> EntriesList { get; } = new ObservableCollection<DisplayEntry>();
		public ObservableCollection<UserGameTile> UserGameItems { get; } = new ObservableCollection<UserGameTile>();
		public string DisplayUser => "User: " + (User?.ToString() ?? "(none)");
		public string DisplayGame => "Game: " + (UserGame?.ToString() ?? "(none)");
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
				OnPropertyChanged(nameof(DisplayGame));
			}
		}

		public MainWindowViewModel(MainWindow mainWindow)
		{
			Application.Current.Exit += ExitProcedures;
			VndbQueries = _vndbQueriesList.Items;
			VndbResponses = _vndbResponsesList.Items;
			Translator = new Translator(StaticMethods.Data);
			Translation.Translator = Translator;
			TestViewModel = new TranslationTester(this);
			FiltersViewModel = new FiltersViewModel();
			DatabaseViewModel = new VNTabViewModel(this, FiltersViewModel.Filters, FiltersViewModel.PermanentFilter);
			IthViewModel = new IthViewModel(this);
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
			Application.Current.Dispatcher.Invoke(() =>
			{
				EntriesList.Clear();
				foreach (var item in items) EntriesList.Add(new DisplayEntry(item));
			});
		}

		public void ExitProcedures(object sender, ExitEventArgs args)
		{
			if (_finalizing) return;
			_finalizing = true;
			Thread ithFinalize = new Thread(() => IthViewModel.Finalize(null, null)) { IsBackground = true };
			ithFinalize.Start();
			bool terminated = ithFinalize.Join(5000); //false if timed out
			if (!terminated) { }
			var exitWatch = Stopwatch.StartNew();
			Debug.WriteLine("(MainWindowViewModel) Starting exit procedures...");
			try
			{
				_closing = true;
				_outputWindow?.Close();
				UserGame?.SaveTimePlayed(false);
				var saveCacheThread = new Thread(StaticMethods.SaveTranslationCache);
				saveCacheThread.Start();
				saveCacheThread.Join();
				_monitor?.Join();
			}
			catch (Exception ex)
			{
				LogToFile(ex);
			}
			Debug.WriteLine($"(MainWindowViewModel) Completed exit procedures, took {exitWatch.Elapsed}");
		}

		public UserGameTile AddGameFile(string file)
		{
			var userGame = StaticMethods.Data.UserGames.FirstOrDefault(x => x.FilePath == file);
			if (userGame != null)
			{
				StatusText = $"This file has already been added. ({userGame.DisplayName})";
				return null;
			}
			//todo cleanup
			var filename = Path.GetFileNameWithoutExtension(file);
			ListedVN[] fileResults = LocalDatabase.VisualNovels.Where(VisualNovelDatabase.ListVNByNameOrAliasFunc(filename)).ToArray();
			ListedVN vn = null;
			if (fileResults.Length == 1) vn = fileResults.First();
			else
			{
				var parent = Directory.GetParent(file);
				var folder = parent.Name.Equals("data", StringComparison.OrdinalIgnoreCase) ? Directory.GetParent(parent.FullName).Name : parent.Name;
				ListedVN[] folderResults = LocalDatabase.VisualNovels.Where(VisualNovelDatabase.ListVNByNameOrAliasFunc(folder)).ToArray();
				if (folderResults.Length == 1) vn = folderResults.First();
			}
			//ListedVN[] allResults = fileResults.Concat(folderResults).ToArray(); //todo list results and ask user
			userGame = new UserGame(file, vn) { Id = StaticMethods.Data.UserGames.Max(x => x.Id) + 1 };
			StaticMethods.Data.UserGames.Add(userGame);
			StaticMethods.Data.SaveChanges();
			StatusText = vn == null ? "File was added without VN." : $"File was added as {userGame.DisplayName}.";
			return new UserGameTile(userGame);
		}

		public async Task Initialize(Stopwatch watch)
		{
			CaptureClipboard = GSettings.CaptureClipboardOnStart;
			await DatabaseViewModel.Initialize();
			await Task.Run(() =>
			{
#if !NOCACHELOAD
				StatusText = "Loading Cached Translations...";
				var cacheLoadWatch = Stopwatch.StartNew();
				StaticMethods.Data.CachedTranslations.Load();
				Translator.SetCache();
				Debug.WriteLine($"Loaded cached translations in {cacheLoadWatch.ElapsedMilliseconds} ms");
#endif
				StatusText = "Populating Proxies...";
				PopulateProxies();
				StatusText = "Loading Dumpfiles...";
				DumpFiles.Load();
				StatusText = "Loading Entries...";
				SetEntries();
			});
			StatusText = "Loading User Games...";
			LoadUserGames();
			TestViewModel.Initialize();
			OnPropertyChanged(nameof(TestViewModel));
			_monitor = new Thread(MonitorStart) { IsBackground = true };
			_monitor.Start();
			StatusText = "Initializing ITHVNR...";
			IthViewModel.Initialize(RunTranslation, GetPreferredHookCode);
			_loadingComplete = true;
			StatusText = "Loading complete.";
			NotificationEvent(this, $"Took {watch.Elapsed:ss\\:fff} seconds.", "Loading Complete");
		}

		public void LoadUserGames()
		{
			StaticMethods.Data.UserGames.Load();
			foreach (var game in StaticMethods.Data.UserGames.Local)
			{
				game.VN = game.VNID != null
					? LocalDatabase.VisualNovels.SingleOrDefault(x => x.VNID == game.VNID)
					: null;
			}
			foreach (var game in StaticMethods.Data.UserGames.Local.OrderBy(x => x.VNID ?? 0)) { UserGameItems.Add(new UserGameTile(game)); }
			OnPropertyChanged(nameof(UserGameItems));
		}

		public async Task SetUser(int userid, bool newId)
		{
			CSettings.UserID = userid;
			if (newId)
			{
				await LocalDatabase.VisualNovels.ForEachAsync(vn => vn.UserVNId = LocalDatabase.UserVisualNovels
					.SingleOrDefault(x => x.UserId == CSettings.UserID && x.VNID == vn.VNID)?.Id);
				await LocalDatabase.SaveChangesAsync();
			}
			User = LocalDatabase.Users.Single(x => x.Id == CSettings.UserID);
			LocalDatabase.CurrentUser = User;
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
			Debug.WriteLine($"MonitorStart starting with ID: {Thread.CurrentThread.ManagedThreadId}");
			StaticMethods.Data.UserGames.Load();
			//NotificationEvent.Invoke(this, $"Processes to monitor: {StaticMethods.Data.UserGameProcesses.Length}");
			while (true)
			{
				if (_closing) return;
				if (UserGame?.Process != null)
				{
					Debug.WriteLine($"MonitorStart ending with ID: {Thread.CurrentThread.ManagedThreadId}");
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
		/// Returns true if loop should be broken.
		/// </summary>
		/// <returns></returns>
		private bool MonitorLoop()
		{
			var processes = Process.GetProcesses();
			try
			{
				var userGameProcesses = StaticMethods.Data.UserGames.Local.Select(x => x.ProcessName).ToArray();
				var gameProcess = processes.FirstOrDefault(p => userGameProcesses.Contains(p.ProcessName));
				if (gameProcess == null) return false;
				var possibleUserGames = StaticMethods.Data.UserGames.Local.Where(x => x.ProcessName == gameProcess.ProcessName);
				foreach (var userGame in possibleUserGames)
				{
					if (_closing) return true;
					if (UserGame?.Process != null)
					{
						Debug.WriteLine($"MonitorLoop ending with ID: {Thread.CurrentThread.ManagedThreadId}");
						return true;
					}
					try
					{
						if (gameProcess.Is64BitProcess()) continue;
						if (gameProcess.HasExited) continue;
						if (!userGame.FilePath.Equals(gameProcess.MainModule.FileName)) continue;
						HookUserGame(userGame, gameProcess);
						Debug.WriteLine($"MonitorLoop ending with ID: {Thread.CurrentThread.ManagedThreadId}");
						return true; //end monitor
					}
					catch (InvalidOperationException)
					{ } //can happen if process is closed after getting reference
				}
			}
			finally
			{
				foreach (var process in processes) process.Dispose();
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

		public void ClipboardChanged(object sender, EventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || !TranslateOn) return;
			if (_hookedProcess == null) return;
			var cpOwner = StaticMethods.GetClipboardOwner();
			var b1 = cpOwner == null;
			var b2 = cpOwner?.Id == _hookedProcess.Id;
			var b3 = cpOwner?.ProcessName.ToLower().Equals("ithvnr") ?? false;
			var b4 = cpOwner?.ProcessName.ToLower().Equals("ithvnrsharp") ?? false;
			if (!(b1 || b2 || b3 || b4)) return; //if process isn't hooked process or named ithvnr
			var text = Clipboard.GetText();
			var timeSinceLast = DateTime.UtcNow - _lastUpdateTime;
			if (timeSinceLast.TotalMilliseconds < 100 && _lastUpdateText == text) return;
			LogVerbose($"Capturing clipboard from {cpOwner?.ProcessName ?? "??"}\t {DateTime.UtcNow:HH\\:mm\\:ss\\:fff}\ttimeSinceLast:{timeSinceLast.Milliseconds}\t{text}");
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
				if (!TranslateOn) return false;
				Func<bool> blockTranslateFunc = () => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
				bool blockTranslate = DispatchIfRequired(blockTranslateFunc);
				if (blockTranslate) return false;
				LogVerbose($"{nameof(RunTranslation)} - {e}");
				if (_hookedProcess == null) return false;
				if ((sender as TextThread)?.IsConsole ?? false) return false;
				var rect = StaticMethods.GetWindowDimensions(_hookedProcess);
				if (rect.ZeroSized) return false; //todo show it somehow or show error.
				var translation = Translator.Translate(User, UserGame?.VN, e.Text);
				if (string.IsNullOrWhiteSpace(translation?.Output))
				{
					//todo report error
					return false;
				}
				Action postOutput = delegate
				{
					//IthViewModel.OnPropertyChanged(nameof(IthViewModel.SelectedTextThread));
					if (!_outputWindow.IsVisible) _outputWindow.Show();
					_outputWindow.SetLocation(rect.Left, rect.Bottom, rect.Width);
					_outputWindow.AddTranslation(translation);
				};
				DispatchIfRequired(postOutput, new TimeSpan(0, 0, 5));
			}
			catch (Exception ex)
			{
				LogToFile(ex);
				return false;
			}

			return true;
		}

		private static void DispatchIfRequired(Action action, TimeSpan timeout)
		{
			if (Application.Current.Dispatcher.CheckAccess()) action();
			else Application.Current.Dispatcher.Invoke(action, timeout);
		}

		private static T DispatchIfRequired<T>(Func<T> action)
		{
			T result;
			if (Application.Current.Dispatcher.CheckAccess()) result = action();
			else result = Application.Current.Dispatcher.Invoke(action);
			return result;
		}

		public void VndbAdvancedAction(string text, bool isQuery)
		{
			Application.Current.Dispatcher.Invoke(() => (isQuery ? _vndbQueriesList : _vndbResponsesList).AddWithId(text));
		}

		public void HookUserGame(UserGame userGame, Process process)
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
				if (process == null)
				{
					process = userGame.LaunchPath != null ? StaticMethods.StartProcessThroughProxy(userGame) : StaticMethods.StartProcess(userGame.FilePath);
				}
				if (userGame.ProcessName == null)
				{
					var game = StaticMethods.Data.UserGames.Single(x => x.Id == userGame.Id);
					game.ProcessName = process.ProcessName;
					StaticMethods.Data.SaveChanges();
				}
				userGame.Process = process;
				_hookedProcess = process;
				_hookedProcess.EnableRaisingEvents = true;
				_hookedProcess.Exited += HookedProcessOnExited;
				TestViewModel.Game = userGame.VN;
				if (!userGame.HookProcess) return;
				while (!_loadingComplete) Thread.Sleep(25);
				IthViewModel.MergeByHookCode = userGame.MergeByHookCode;
				IthViewModel.PrefEncoding = userGame.PrefEncoding;
				IthViewModel.Commands?.ProcessCommand($"/P{_hookedProcess.Id}", 0);
				if (!string.IsNullOrWhiteSpace(UserGame.HookCode)) IthViewModel.Commands?.ProcessCommand(UserGame.HookCode, _hookedProcess.Id);
			}
		}

		private void HookedProcessOnExited(object sender, EventArgs eventArgs)
		{
			Application.Current.Dispatcher.Invoke(() => _outputWindow.Hide());
			UserGame = null;
			//restart monitor
			if (_monitor != null && _monitor.IsAlive) return;
			_monitor = new Thread(MonitorStart) { IsBackground = true };
			_monitor.Start();
		}

		public void DebugButton()
		{
			_outputWindow.Show();
		}

		// ReSharper disable UnusedTupleComponentInReturnValue
		public (string HookCode, Encoding PrefEncoding) GetPreferredHookCode(uint processId)
		{
			return processId != UserGame?.Process?.Id ? (null, null) : (UserGame.HookCode, UserGame.PrefEncoding);
		}
		// ReSharper restore UnusedTupleComponentInReturnValue

		public void DeleteEntry(DisplayEntry displayEntry)
		{
			EntriesList.Remove(displayEntry);
			StaticMethods.Data.Entries.Remove(displayEntry.Entry);
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(EntriesList));
		}

	}
}
