using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using Happy_Reader.View;
using JetBrains.Annotations;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public sealed class UserGame : INotifyPropertyChanged, IDataItem<long>, IReadyToUpsert
	{
		public enum HookMode
		{
			None = 0,
			VnrHook = 1,
			VnrAgent = 2
		}

		public enum ProcessStatus
		{
			Off = 0,
			Paused = 1,
			On = 2
		}

		public static readonly SortedList<DateTime, long> LastGamesPlayed = new();
		private HookMode _hookProcess;
		private BitmapImage _image;
		private NativeMethods.RECT? _locationOnMoveStart;
		private bool _mergeByHookCode;
		private EncodingEnum _prefEncodingEnum;
		private Process _process;
		private bool _removeRepetition;

		private Stopwatch _runningTime;
		private ListedVN _vn;
		private bool _vnGot;
		private WinAPI.WindowHook _windowHook;

		public UserGame(string file, ListedVN vn)
		{
			FilePath = file;
			VNID = vn?.VNID;
			VN = vn;
			if (VN != null) VN.IsOwned = FileExists ? OwnedStatus.CurrentlyOwned : OwnedStatus.PastOwned;
		}

		public UserGame()
		{
		}

		public static Encoding[] Encodings => IthVnrSharpLib.IthVnrViewModel.Encodings;
		public static HookMode[] HookModes { get; } = (HookMode[]) Enum.GetValues(typeof(HookMode));
		public long Id { get; set; }
		public string UserDefinedName { get; private set; }
		public string LaunchPath { get; private set; }
		public HookMode HookProcess
		{
			get => _hookProcess;
			set
			{
				if (_hookProcess == value) return;
				_hookProcess = value;
				if (Loaded) ReadyToUpsert = true;
			}
		}
		public int? VNID { get; private set; }
		public string FilePath { get; private set; }
		public string HookCode { get; private set; }
		public bool MergeByHookCode
		{
			get => _mergeByHookCode;
			set
			{
				if (_mergeByHookCode == value) return;
				_mergeByHookCode = value;
				if (Loaded) ReadyToUpsert = true;
			}
		}
		public string ProcessName { get; set; }
		public string Tag { get; private set; }
		public bool RemoveRepetition
		{
			get => _removeRepetition;
			set
			{
				if (_removeRepetition == value) return;
				_removeRepetition = value;
				if (Loaded) ReadyToUpsert = true;
			}
		}
		/// <summary>
		/// Store output window location and dimensions as a string in the 'form x,y,width,height' 
		/// </summary>
		private string OutputWindow { get; set; }
		// ReSharper disable once InconsistentNaming
		private DateTime TimeOpenDT { get; set; }
		private EncodingEnum PrefEncodingEnum
		{
			get => _prefEncodingEnum;
			set
			{
				if (_prefEncodingEnum == value) return;
				_prefEncodingEnum = value;
				if (Loaded) ReadyToUpsert = true;
			}
		}
		public bool HasVN => VNID.HasValue;
		public bool FileExists => File.Exists(FilePath);
		public bool IsHooked => Process != null && HookProcess != HookMode.None;
		public Rectangle OutputRectangle
		{
			get
			{
				if (string.IsNullOrEmpty(OutputWindow)) return StaticMethods.OutputWindowStartPosition;
				List<int> parts = OutputWindow.Split(',').Select(int.Parse).ToList();
				return new Rectangle(parts[0], parts[1], parts[2], parts[3]);
			}
			set => OutputWindow = string.Join(",", value.X, value.Y, value.Width, value.Height);
		}
		public TimeSpan TimeOpen
		{
			get => TimeSpan.FromTicks(TimeOpenDT.Ticks + (_runningTime?.ElapsedTicks ?? 0));
			set => TimeOpenDT = new DateTime(value.Ticks);
		}
		public ListedVN VN
		{
			get
			{
				if (Id == 0) return _vn;
				if (!_vnGot)
				{
					if (LocalDatabase == null) return null;
					if (VNID != null) _vn = LocalDatabase.VisualNovels[VNID.Value];
					_vnGot = true;
				}

				return _vn;
			}
			set
			{
				_vn = value;
				_vnGot = true;
			}
		}
		public Process Process
		{
			get => _process;
			set
			{
				if (value == null) _process?.Dispose();
				_process = value;
				OnPropertyChanged(nameof(TimeOpen));
				OnPropertyChanged(nameof(RunningStatus));
				if (value == null) return;
				Log.NewStartedPlayingLog(Id, DateTime.Now);
				_runningTime = Stopwatch.StartNew();
				_process.Exited += ProcessExited;
				_process.EnableRaisingEvents = true;
			}
		}
		public string DisplayName => !string.IsNullOrWhiteSpace(UserDefinedName)
			? UserDefinedName
			: StaticMethods.TruncateStringFunction30(VN?.Title ?? Path.GetFileNameWithoutExtension(FilePath));
		public BitmapImage Image
		{
			get
			{
				Bitmap image;
				if (!File.Exists(FilePath))
				{
					// ReSharper disable once PossibleNullReferenceException
					return Theme.FileNotFoundImage;
				}

				if ((VN?.ImageNSFW ?? false) && !StaticMethods.ShowNSFWImages()) return Theme.NsfwImage;
				if (_image != null) return _image;
				// ReSharper disable once PossibleNullReferenceException
				if (VN?.ImageSource == null)
				{
					if (!IconImageExists(out var iconPath)) return Theme.ImageNotFoundImage;
					image = new Bitmap(iconPath);
				}
				else image = new Bitmap(VN.ImageSource);

				using var memory = new MemoryStream();
				image.Save(memory, ImageFormat.Bmp);
				memory.Position = 0;
				_image = new BitmapImage();
				_image.BeginInit();
				_image.StreamSource = memory;
				_image.CacheOption = BitmapCacheOption.OnLoad;
				_image.EndInit();
				return _image;
			}
		}
		public Encoding PrefEncoding
		{
			get => Encodings[(int) PrefEncodingEnum];
			set
			{
				var index = Array.IndexOf(Encodings, value);
				PrefEncodingEnum = (EncodingEnum) index;
			}
		}
		public string DisplayNameGroup => DisplayName.Substring(0, Math.Min(DisplayName.Length, 1));
		public string TagSort => string.IsNullOrWhiteSpace(Tag) ? char.MaxValue.ToString() : Tag;
		public DateTime LastPlayedDate
		{
			[UsedImplicitly]
			get
			{
				return LastGamesPlayed.ContainsValue(Id) ? LastGamesPlayed.First(x => x.Value == Id).Key : DateTime.MinValue;
			}
		}
		public ProcessStatus RunningStatus
		{
			get
			{
				if (_process == null) return ProcessStatus.Off; //off = no process running
				if (_runningTime != null && !_runningTime.IsRunning)
					return ProcessStatus.Paused; //paused = process is running but timer is paused
				return ProcessStatus.On; //on = process is running and timer is not paused
			}
		}
		public NativeMethods.RECT WindowLocation { get; private set; }
		public static Action<NativeMethods.RECT> MoveOutputWindow { get; set; }
		public string KeyField => nameof(Id);
		public long Key => Id;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(UserGame)}s" +
			             "(Id, UserDefinedName, LaunchPath, HookProcess, VNID, FilePath, HookCode, MergeByHookCode, ProcessName, Tag, RemoveRepetition, OutputWindow, TimeOpenDT, PrefEncodingEnum) " +
			             "VALUES " +
			             "(@Id, @UserDefinedName, @LaunchPath, @HookProcess, @VNID, @FilePath, @HookCode, @MergeByHookCode, @ProcessName, @Tag, @RemoveRepetition, @OutputWindow, @TimeOpenDT, @PrefEncodingEnum)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Id", Id);
			command.AddParameter("@UserDefinedName", UserDefinedName);
			command.AddParameter("@LaunchPath", LaunchPath);
			command.AddParameter("@HookProcess", HookProcess);
			command.AddParameter("@VNID", VNID);
			command.AddParameter("@FilePath", FilePath);
			command.AddParameter("@HookCode", HookCode);
			command.AddParameter("@MergeByHookCode", MergeByHookCode);
			command.AddParameter("@ProcessName", ProcessName);
			command.AddParameter("@Tag", Tag);
			command.AddParameter("@RemoveRepetition", RemoveRepetition);
			command.AddParameter("@OutputWindow", OutputWindow);
			command.AddParameter("@TimeOpenDT", TimeOpenDT);
			command.AddParameter("@PrefEncodingEnum", PrefEncodingEnum);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			Id = Convert.ToInt32(reader["Id"]);
			UserDefinedName = Convert.ToString(reader["UserDefinedName"]);
			LaunchPath = Convert.ToString(reader["LaunchPath"]);
			HookProcess = (HookMode) Convert.ToInt32(reader["HookProcess"]);
			VNID = GetNullableInt(reader["VNID"]);
			FilePath = Convert.ToString(reader["FilePath"]);
			HookCode = Convert.ToString(reader["HookCode"]);
			MergeByHookCode = Convert.ToInt32(reader["MergeByHookCode"]) == 1;
			ProcessName = Convert.ToString(reader["ProcessName"]);
			Tag = Convert.ToString(reader["Tag"]);
			RemoveRepetition = Convert.ToInt32(reader["RemoveRepetition"]) == 1;
			OutputWindow = Convert.ToString(reader["OutputWindow"]);
			TimeOpenDT = Convert.ToDateTime(reader["TimeOpenDT"]);
			PrefEncodingEnum = (EncodingEnum) Convert.ToInt32(reader["PrefEncodingEnum"]);
			Loaded = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public bool Loaded { get; private set; }
		public bool ReadyToUpsert { get; set; }

		private bool IconImageExists(out string iconPath)
		{
			iconPath = Path.Combine(StaticMethods.UserGameIconsFolder, Id + ".bmp");
			return File.Exists(iconPath);
		}

		public void SaveIconImage()
		{
			IconImageExists(out var iconPath);
			var icon = Icon.ExtractAssociatedIcon(FilePath);
			if (icon == null) return;
			var image = icon.ToBitmap();
			using var fileStream = File.OpenWrite(iconPath);
			image.Save(fileStream, ImageFormat.Bmp);
			OnPropertyChanged(nameof(Image));
		}

		public void SaveTimePlayed(bool notify)
		{
			_runningTime.Stop();
			var timeToAdd = _runningTime.Elapsed;
			_runningTime = null;
			TimeOpen += timeToAdd;
			Process = null;
			var log = Log.NewTimePlayedLog(Id, timeToAdd, notify);
			StaticMethods.Data.Logs.Add(log, true, true);
		}

		public void MergeTimePlayed(TimeSpan mergedTimePlayed)
		{
			TimeOpen += mergedTimePlayed;
			var log = Log.NewMergedTimePlayedLog(Id, mergedTimePlayed, false);
			StaticMethods.Data.Logs.Add(log, true, true);
			OnPropertyChanged(nameof(TimeOpen));
		}

		public void ResetTimePlayed()
		{
			TimeOpen = new TimeSpan();
			var log = Log.NewResetTimePlayedLog(Id, false);
			StaticMethods.Data.Logs.Add(log, true, true);
			OnPropertyChanged(nameof(TimeOpen));
		}

		private void ProcessExited(object sender, EventArgs e)
		{
			SaveTimePlayed(true);
			_windowHook?.Dispose();
			_windowHook = null;
		}

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public void SaveUserDefinedName([NotNull] string text)
		{
			UserDefinedName = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.UserGames.Upsert(this, true);
			OnPropertyChanged(nameof(DisplayName));
		}

		public void SaveTag([NotNull] string text)
		{
			Tag = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.UserGames.Upsert(this, true);
			OnPropertyChanged(nameof(Tag));
		}

		public bool SaveVNID(int? vnid)
		{
			VNID = vnid;
			StaticMethods.Data.UserGames.Upsert(this, true);
			VN = vnid == null ? null : LocalDatabase.VisualNovels[vnid.Value];
			if (VN != null) VN.IsOwned = FileExists ? OwnedStatus.CurrentlyOwned : OwnedStatus.PastOwned;
			OnPropertyChanged(nameof(DisplayName));
			OnPropertyChanged(nameof(Image));
			OnPropertyChanged(nameof(VN));
			return VN != null;
		}

		public void SaveHookCode(string hookCode)
		{
			HookCode = string.IsNullOrWhiteSpace(hookCode) ? null : hookCode.Trim();
			StaticMethods.Data.UserGames.Upsert(this, true);
			OnPropertyChanged(nameof(HookCode));
		}

		public void ChangeFilePath(string newFilePath)
		{
			FilePath = newFilePath;
			SaveIconImage();
			StaticMethods.Data.UserGames.Upsert(this, true);
			OnPropertyChanged(nameof(FilePath));
			OnPropertyChanged(nameof(FileExists));
			OnPropertyChanged(nameof(Image));
		}

		public void SaveLaunchPath([NotNull] string text)
		{
			LaunchPath = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(LaunchPath));
		}

		public override string ToString() => !string.IsNullOrWhiteSpace(UserDefinedName)
			? UserDefinedName
			: VN?.Title ?? Path.GetFileNameWithoutExtension(FilePath);

		public void SetActiveProcess(Process process, EventHandler hookedProcessOnExited)
		{
			Process = process;
			Process.EnableRaisingEvents = true;
			_windowHook = new WinAPI.WindowHook(process);
			var success = NativeMethods.GetWindowRect(_process.MainWindowHandle, out var location);
			if (success) WindowLocation = location;
			_windowHook.OnWindowMinimizeStart += WindowIsMinimised;
			_windowHook.OnWindowMinimizeEnd += WindowsIsRestored;
			_windowHook.OnWindowMoveSizeStart += WindowMoveStarts;
			_windowHook.OnWindowMoveSizeEnd += WindowMoveEnds;
			Process.Exited += hookedProcessOnExited;
			if (WinAPI.IsIconic(process.MainWindowHandle)) WindowIsMinimised(process.MainWindowHandle);
		}

		private void WindowMoveStarts(IntPtr windowPointer)
		{
			if (MoveOutputWindow == null) return;
			var success = NativeMethods.GetWindowRect(windowPointer, out var location);
			if (success) _locationOnMoveStart = location;
		}

		private void WindowMoveEnds(IntPtr windowPointer)
		{
			if (MoveOutputWindow == null) return;
			var success = NativeMethods.GetWindowRect(windowPointer, out var newLocation);
			if (!success || !_locationOnMoveStart.HasValue) return;
			WindowLocation = newLocation;
			var diff = newLocation - _locationOnMoveStart.Value;
			MoveOutputWindow?.Invoke(diff);
			_locationOnMoveStart = null;
		}

		private void WindowsIsRestored(IntPtr windowPointer)
		{
			var success = NativeMethods.GetWindowRect(windowPointer, out var location);
			if (success) WindowLocation = location;
			Logger.ToDebug($"Restored {DisplayName}, starting running time at {_runningTime.Elapsed}");
			_runningTime.Start();
			OnPropertyChanged(nameof(RunningStatus));
			var outputWindow = StaticMethods.MainWindow.ViewModel.OutputWindow;
			if (outputWindow.InitialisedWindowLocation) StaticMethods.MainWindow.ViewModel.OutputWindow.Show();
		}

		private void WindowIsMinimised(IntPtr windowPointer)
		{
			_runningTime.Stop();
			Logger.ToDebug($"Minimized {DisplayName}, stopped running time at {_runningTime.Elapsed}");
			OnPropertyChanged(nameof(RunningStatus));
			OnPropertyChanged(nameof(TimeOpen));
			StaticMethods.MainWindow.ViewModel.OutputWindow.Hide();
		}

		public Process StartProcessThroughLocaleEmulator()
		{
			var proxyPath = StaticMethods.Settings.GuiSettings.LocaleEmulatorPath;
			var args = $"\"{FilePath}\"";
			return StartProcess(proxyPath, args, true);
		}

		public Process StartProcessThroughProxy()
		{
			var firstQuote = LaunchPath.IndexOf('"');
			string proxyPath;
			string args;
			if (firstQuote > -1)
			{
				var secondQuote = LaunchPath.IndexOf('"', firstQuote + 1);
				proxyPath = LaunchPath.Substring(firstQuote + 1, secondQuote - 1);
				args = LaunchPath.Substring(secondQuote + 1).Trim();
			}
			else
			{
				var firstSpace = LaunchPath.IndexOf(' ');
				proxyPath = LaunchPath.Substring(0, firstSpace);
				args = LaunchPath.Substring(firstSpace + 1).Trim();
			}

			return StartProcess(proxyPath, args, true);
		}

		public Process StartProcess(string filePath, string args, bool usingProxy)
		{
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(FilePath));
			var existing = processes.FirstOrDefault();
			if (existing != null) return existing;
			var exeParentFolder = Path.GetDirectoryName(filePath);
			if (exeParentFolder == null)
				throw new ArgumentNullException(nameof(exeParentFolder), "Parent folder of exe was not found.");
			var pi = new ProcessStartInfo
			{
				FileName = filePath,
				UseShellExecute = false,
				WorkingDirectory = exeParentFolder,
				Arguments = args ?? string.Empty
			};
			var processStarted = Process.Start(pi);
			if (usingProxy)
			{
				Thread.Sleep(1500);
				processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(FilePath));
				return processes.FirstOrDefault();
			}

			Debug.Assert(processStarted != null, nameof(processStarted) + " != null");
			if (!processStarted.HasExited) processStarted.WaitForInputIdle(3000);
			return processStarted;
		}

		private enum EncodingEnum
		{
			// ReSharper disable InconsistentNaming
			// ReSharper disable UnusedMember.Local
			ShiftJis,
			UTF8,
			Unicode
			// ReSharper restore UnusedMember.Local
			// ReSharper restore InconsistentNaming
		}
	}
}