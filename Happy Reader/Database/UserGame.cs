using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Happy_Apps_Core.Database;
using IthVnrSharpLib;
using JetBrains.Annotations;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class UserGame : INotifyPropertyChanged
	{
		public static readonly SortedList<DateTime, long> LastGamesPlayed = new SortedList<DateTime, long>();
		public static Encoding[] Encodings => IthVnrViewModel.Encodings;

		private Stopwatch _runningTime;
		private Process _process;
		private ListedVN _vn;
		private bool _vnGot;

		public UserGame(string file, ListedVN vn)
		{
			FilePath = file;
			VNID = vn?.VNID;
			VN = vn;
		}

		public UserGame() { }

		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long Id { get; set; }
		public string UserDefinedName { get; set; }
		public string LaunchPath { get; set; }
		public bool HookProcess { get; set; }
		public int? VNID { get; set; }
		public string FilePath { get; set; }
		public string HookCode { get; set; }
		public string DefaultHookFull { get; set; }
		public bool MergeByHookCode { get; set; }
		public string ProcessName { get; set; }
		public string Tag { get; set; }

		public bool HasVN => VNID.HasValue;
		public bool FileExists => File.Exists(FilePath);

		/// <summary>
		/// Store output window location and dimensions as a string in the 'form x,y,width,height' 
		/// </summary>
		public string OutputWindow { get; set; }

		[NotMapped]
		public Rectangle OutputRectangle
		{
			get
			{
				if (string.IsNullOrEmpty(OutputWindow)) return new Rectangle(20, 20, 400, 200);
				List<int> parts = OutputWindow.Split(',').Select(int.Parse).ToList();
				return new Rectangle(parts[0], parts[1], parts[2], parts[3]);
			}
			set => OutputWindow = string.Join(",", value.X, value.Y, value.Width, value.Height);
		}

		// ReSharper disable once InconsistentNaming
		public DateTime TimeOpenDT { get; set; }
		public EncodingEnum PrefEncodingEnum { get; set; }
		[NotMapped]
		public TimeSpan TimeOpen
		{
			get => TimeSpan.FromTicks(TimeOpenDT.Ticks);
			set => TimeOpenDT = new DateTime(value.Ticks);
		}

		[NotMapped]
		public ListedVN VN
		{
			get
			{
				if (Id == 0) return _vn;
				if (!_vnGot)
				{
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

		[NotMapped]
		public Process Process
		{
			get => _process;
			set
			{
				if (value == null) _process?.Dispose();
				_process = value;
				OnPropertyChanged(nameof(IsRunning));
				OnPropertyChanged(nameof(TimeOpen));
				if (value == null) return;
				Log.NewStartedPlayingLog(Id, DateTime.Now);
				_runningTime = Stopwatch.StartNew();
				_process.Exited += ProcessExited;
				_process.EnableRaisingEvents = true;
			}
		}

		private WinAPI.WindowHook _windowHook;

		[NotMapped]
		public string DisplayName =>
			UserDefinedName ?? StaticMethods.TruncateStringFunction30(VN?.Title ?? Path.GetFileNameWithoutExtension(FilePath));

		[NotMapped]
		public BitmapImage Image
		{
			get
			{
				Bitmap image;
				if (!File.Exists(FilePath))
				{
					// ReSharper disable once PossibleNullReferenceException
					Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/file-not-found.png")).Stream;
					image = new Bitmap(iconStream);
				}
				else if (VN == null) image = Icon.ExtractAssociatedIcon(FilePath)?.ToBitmap();
				else if (File.Exists(VN.ImageSource)) image = new Bitmap(VN.ImageSource);
				else image = Icon.ExtractAssociatedIcon(FilePath)?.ToBitmap();
				if (image == null)
				{
					// ReSharper disable once PossibleNullReferenceException
					Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/no-image.png")).Stream;
					image = new Bitmap(iconStream);
				}
				using var memory = new MemoryStream();
				image.Save(memory, ImageFormat.Bmp);
				memory.Position = 0;
				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memory;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				return bitmapImage;
			}
		}
		[NotMapped]
		public Encoding PrefEncoding
		{
			get => Encodings[(int)PrefEncodingEnum];
			set
			{
				var index = Array.IndexOf(Encodings, value);
				PrefEncodingEnum = (EncodingEnum)index;
			}
		}
		[NotMapped]
		public string MonthGroupingString
		{
			[UsedImplicitly]
			get
			{
				if (!File.Exists(FilePath)) return "File not found"; //DateTime.MinValue;
				if (LastGamesPlayed.IndexOfValue(Id) > LastGamesPlayed.Count - 6) return "Last Played";
				if (VN == null) return "Other"; //DateTime.MinValue.AddDays(1);
				var dt = VN.ReleaseDate;
				var newDt = new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
				return string.Format(CultureInfo.InvariantCulture, "{0:MMMM} {0:yyyy}", newDt);
			}
		}
		[NotMapped]
		public string DisplayNameGroup => DisplayName.Substring(0, Math.Min(DisplayName.Length, 1));

		[NotMapped]
		public DateTime MonthGrouping
		{
			[UsedImplicitly]
			get
			{
				if (!File.Exists(FilePath)) return DateTime.MinValue;
				if (VN == null) return DateTime.MinValue.AddDays(1);
				if (LastGamesPlayed.IndexOfValue(Id) > LastGamesPlayed.Count - 6) return DateTime.MaxValue;
				var dt = VN.ReleaseDate;
				var newDt = new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
				return newDt;
			}
		}
		[NotMapped]
		public string TagSort => string.IsNullOrWhiteSpace(Tag) ? char.MaxValue.ToString() : Tag;

		[NotMapped]
		public DateTime LastPlayedDate
		{
			[UsedImplicitly]
			get
			{
				return LastGamesPlayed.ContainsValue(Id) ? LastGamesPlayed.First(x => x.Value == Id).Key : DateTime.MinValue;
			}
		}

		[NotMapped]
		public bool IsRunning => _process != null;

		[NotMapped]
		public Action<NativeMethods.RECT> MoveOutputWindow { get; set; }

		private NativeMethods.RECT? _locationOnMoveStart;

		public event PropertyChangedEventHandler PropertyChanged;

		public void SaveTimePlayed(bool notify)
		{
			_runningTime.Stop();
			TimeOpen += _runningTime.Elapsed;
			Process = null;
			var log = Log.NewTimePlayedLog(Id, _runningTime.Elapsed, notify);
			StaticMethods.Data.Logs.Add(log);
			StaticMethods.Data.SaveChanges();
		}

		public void MergeTimePlayed(TimeSpan mergedTimePlayed)
		{
			TimeOpen += mergedTimePlayed;
			var log = Log.NewMergedTimePlayedLog(Id, mergedTimePlayed, false);
			StaticMethods.Data.Logs.Add(log);
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(TimeOpen));
		}

		public void ResetTimePlayed()
		{
			TimeOpen = new TimeSpan();
			var log = Log.NewResetTimePlayedLog(Id, false);
			StaticMethods.Data.Logs.Add(log);
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(TimeOpen));
		}

		private void ProcessExited(object sender, EventArgs e)
		{
			SaveTimePlayed(true);
			_windowHook?.Dispose();
			_windowHook = null;
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public void SaveUserDefinedName([NotNull] string text)
		{
			UserDefinedName = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(DisplayName));
		}

		public void SaveTag([NotNull] string text)
		{
			Tag = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(Tag));
		}

		public bool SaveVNID(int? vnid)
		{
			VNID = vnid;
			StaticMethods.Data.SaveChanges();
			VN = vnid == null ? null : LocalDatabase.VisualNovels[vnid.Value];
			OnPropertyChanged(nameof(DisplayName));
			OnPropertyChanged(nameof(Image));
			OnPropertyChanged(nameof(VN));
			return VN != null;
		}

		public void SaveHookCode(string hookCode, string fullHook)
		{
			HookCode = string.IsNullOrWhiteSpace(hookCode) ? null : hookCode.Trim();
			DefaultHookFull = fullHook;
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(HookCode));
		}

		public void ChangeFilePath(string newFilePath)
		{
			FilePath = newFilePath;
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(FilePath));
			OnPropertyChanged(nameof(Image));
		}

		public void SaveLaunchPath([NotNull] string text)
		{
			LaunchPath = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(LaunchPath));
		}

		public override string ToString() => UserDefinedName ?? VN?.Title ?? Path.GetFileNameWithoutExtension(FilePath);

		public void SetActiveProcess(Process process, EventHandler hookedProcessOnExited)
		{
			Process = process;
			Process.EnableRaisingEvents = true;
			_windowHook = new WinAPI.WindowHook(process);
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
			var diff = newLocation - _locationOnMoveStart.Value;
			MoveOutputWindow?.Invoke(diff);
			_locationOnMoveStart = null;
		}

		private void WindowsIsRestored(IntPtr windowPointer)
		{
			Logger.ToDebug($"Restored {DisplayName}, starting running time at {_runningTime.Elapsed}");
			_runningTime.Start();
		}

		private void WindowIsMinimised(IntPtr windowPointer)
		{
			_runningTime.Stop();
			Logger.ToDebug($"Minimized {DisplayName}, stopped running time at {_runningTime.Elapsed}");
		}

		public Process StartProcessThroughLocaleEmulator()
		{
			var proxyPath = StaticMethods.GuiSettings.LocaleEmulatorPath;
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
			string exeParentFolder = Path.GetDirectoryName(filePath);
			if (exeParentFolder == null) throw new ArgumentNullException(nameof(exeParentFolder), "Parent folder of exe was not found.");
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
		
		public enum EncodingEnum
		{
			// ReSharper disable once InconsistentNaming
			ShiftJis, UTF8, Unicode
		}
	}

}
