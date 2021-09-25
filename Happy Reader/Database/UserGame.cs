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
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using Happy_Reader.View;
using JetBrains.Annotations;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
	public class UserGame : INotifyPropertyChanged, IDataItem<long>, IReadyToUpsert
	{
		public enum ProcessStatus
		{
			Off = 0,
			Paused = 1,
			On = 2
		}

		public enum LaunchOverrideMode
		{
			[Description("No Override")]
			None = 0,
			Normal = 1,
			[Description("Use Locale Emulator")]
			UseLe = 2
		}

		public static readonly SortedList<DateTime, long> LastGamesPlayed = new();
		public static ComboBoxItem[] LaunchOverrideModes { get; } = StaticMethods.GetEnumValues(typeof(LaunchOverrideMode));

		private BitmapImage _image;
		private string _processName;
		private Process _process;
		private TimeSpan _timeOpen;
		internal Stopwatch RunningTime;
		private ListedVN _vn;
		private bool _vnGot;
		private LaunchOverrideMode _launchModeOverride = LaunchOverrideMode.None;

		public UserGame(string file, ListedVN vn)
		{
			GameHookSettings = new GameHookSettings(this);
			FilePath = file;
			VNID = vn?.VNID;
			VN = vn;
			if (VN != null) VN.IsOwned = FileExists ? OwnedStatus.CurrentlyOwned : OwnedStatus.PastOwned;
		}

		public UserGame()
		{
			GameHookSettings = new GameHookSettings(this);
		}

		public long Id { get; set; }
		public string UserDefinedName { get; private set; }
		public string LaunchPath { get; private set; }
		public LaunchOverrideMode LaunchModeOverride
		{
			get => _launchModeOverride;
			set
			{
				if (_launchModeOverride == value) return;
				_launchModeOverride = value;
				if (Loaded) ReadyToUpsert = true;
			}
		}
		public int? VNID { get; private set; }
		public string FilePath { get; private set; }
		public string ProcessName
		{
			get => _processName;
			set
			{
				if (_processName == value) return;
				_processName = value;
				if (Loaded) ReadyToUpsert = true;
			}
		}
		public string Tag { get; private set; }
		public string Note { get; private set; }
		public bool HasVN => VNID.HasValue && VN != null;
		public bool FileExists => File.Exists(FilePath);
		public TimeSpan TimeOpen
		{
			get => TimeSpan.FromTicks(_timeOpen.Ticks + (RunningTime?.ElapsedTicks ?? 0));
			set
			{
				if (_timeOpen == value) return;
				_timeOpen = value;
				if (Loaded) ReadyToUpsert = true;
			}
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
				RunningTime = Stopwatch.StartNew();
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
					var imageTemp = new Bitmap(iconPath);
					image = new Bitmap(imageTemp);
					imageTemp.Dispose();
				}
				else
				{
					var imageTemp = new Bitmap(VN.ImageSource);
					image = new Bitmap(imageTemp);
					imageTemp.Dispose();
				}

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
				if (RunningTime != null && !RunningTime.IsRunning)
					return ProcessStatus.Paused; //paused = process is running but timer is paused
				return ProcessStatus.On; //on = process is running and timer is not paused
			}
		}
		public string KeyField => nameof(Id);
		public long Key => Id;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(UserGame)}s" +
									 "(Id, UserDefinedName, LaunchPath, HookProcess, VNID, FilePath, HookCode, MergeByHookCode, MatchHookCode, ProcessName, Tag, RemoveRepetition, OutputWindow, TimeOpenDT, PrefEncodingEnum, LaunchModeOverride) " +
									 "VALUES " +
									 "(@Id, @UserDefinedName, @LaunchPath, @HookProcess, @VNID, @FilePath, @HookCode, @MergeByHookCode, @MatchHookCode, @ProcessName, @Tag, @RemoveRepetition, @OutputWindow, @TimeOpenDT, @PrefEncodingEnum, @LaunchModeOverride)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Id", Id);
			command.AddParameter("@UserDefinedName", UserDefinedName);
			command.AddParameter("@LaunchPath", LaunchPath);
			command.AddParameter("@HookProcess", GameHookSettings.HookProcess);
			command.AddParameter("@VNID", VNID);
			command.AddParameter("@FilePath", FilePath);
			command.AddParameter("@HookCode", GameHookSettings.HookCodes);
			command.AddParameter("@MergeByHookCode", GameHookSettings.MergeByHookCode);
			command.AddParameter("@MatchHookCode", GameHookSettings.MatchHookCode);
			command.AddParameter("@ProcessName", ProcessName);
			command.AddParameter("@Tag", Tag);
			command.AddParameter("@RemoveRepetition", GameHookSettings.RemoveRepetition);
			command.AddParameter("@TimeOpenDT", new DateTime(TimeOpen.Ticks));
			command.AddParameter("@PrefEncodingEnum", GameHookSettings.PrefEncodingEnum);
			command.AddParameter("@OutputWindow", GetOutputWindowString(GameHookSettings.OutputRectangle));
			command.AddParameter("@LaunchModeOverride", LaunchModeOverride);
			return command;
		}

		private static string GetOutputWindowString(NativeMethods.RECT outputRectangle)
			=> string.Join(",", outputRectangle.Left, outputRectangle.Top, outputRectangle.Width, outputRectangle.Height);

		public void LoadFromReader(IDataRecord reader)
		{
			Id = Convert.ToInt32(reader["Id"]);
			UserDefinedName = Convert.ToString(reader["UserDefinedName"]);
			LaunchPath = Convert.ToString(reader["LaunchPath"]);
			GameHookSettings.HookProcess = (HookMode)Convert.ToInt32(reader["HookProcess"]);
			VNID = GetNullableInt(reader["VNID"]);
			FilePath = Convert.ToString(reader["FilePath"]);
			GameHookSettings.HookCodes = Convert.ToString(reader["HookCode"]);
			GameHookSettings.MergeByHookCode = Convert.ToInt32(reader["MergeByHookCode"]) == 1;
			GameHookSettings.MatchHookCode = Convert.ToInt32(reader["MatchHookCode"]) == 1;
			ProcessName = Convert.ToString(reader["ProcessName"]);
			Tag = Convert.ToString(reader["Tag"]);
			Note = Convert.ToString(reader["Note"]);
			GameHookSettings.RemoveRepetition = Convert.ToInt32(reader["RemoveRepetition"]) == 1;
			TimeOpen = TimeSpan.FromTicks(Convert.ToDateTime(reader["TimeOpenDT"]).Ticks);
			GameHookSettings.PrefEncodingEnum = (EncodingEnum)Convert.ToInt32(reader["PrefEncodingEnum"]);
			var outputWindow = Convert.ToString(reader["OutputWindow"]);
			GameHookSettings.OutputRectangle = GameHookSettings.GetOutputRectangle(outputWindow);
			LaunchModeOverride = (LaunchOverrideMode)Convert.ToInt32(reader["LaunchModeOverride"]);
			Loaded = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public bool Loaded { get; private set; }
		public bool ReadyToUpsert { get; set; }

		[NotNull] public GameHookSettings GameHookSettings { get; }

		public bool IconImageExists(out string iconPath)
		{
			iconPath = Path.Combine(StaticMethods.UserGameIconsFolder, Id + ".bmp");
			return File.Exists(iconPath);
		}

		public void SaveIconImage()
		{
			try
			{
				IconImageExists(out var iconPath);
				var icon = Icon.ExtractAssociatedIcon(FilePath);
				if (icon == null) return;
				using var image = icon.ToBitmap();
				using var fileStream = File.OpenWrite(iconPath);
				image.Save(fileStream, ImageFormat.Bmp);
			}
			catch (Exception ex)
			{
				Logger.ToFile(ex);
			}
			OnPropertyChanged(nameof(Image));
		}

		public void SaveTimePlayed(bool notify)
		{
			RunningTime.Stop();
			var timeToAdd = RunningTime.Elapsed;
			RunningTime = null;
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
			GameHookSettings.DisposeWindow();
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

		public void SaveNote([NotNull] string text)
		{
			Note = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.UserGames.Upsert(this, true);
			OnPropertyChanged(nameof(Note));
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
			StaticMethods.Data.UserGames.Upsert(this, true);
			OnPropertyChanged(nameof(LaunchPath));
		}

		public override string ToString() => !string.IsNullOrWhiteSpace(UserDefinedName)
			? UserDefinedName
			: VN?.Title ?? Path.GetFileNameWithoutExtension(FilePath);

		public void SetActiveProcess(Process process, EventHandler hookedProcessOnExited)
		{
			Process = process;
			Process.EnableRaisingEvents = true;
			Process.Exited += hookedProcessOnExited;
			GameHookSettings.InitialiseWindow(process);
		}

		public Process StartProcessThroughLocaleEmulator()
		{
			var proxyPath = StaticMethods.Settings.GuiSettings.LocaleEmulatorPath;
			if (string.IsNullOrWhiteSpace(proxyPath) || !File.Exists(proxyPath)) throw new FileNotFoundException("Locale emulator path empty or not found (check settings).");
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
			Logger.ToFile($"{nameof(StartProcess)}, Using Proxy = {usingProxy}: '{filePath}' {args}'");
			var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(FilePath));
			var existing = processes.FirstOrDefault();
			if (existing != null)
			{
				Logger.ToFile($"{nameof(StartProcess)}: Already existed.");
				return existing;
			}
			var exeParentFolder = Path.GetDirectoryName(filePath);
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
				Thread.Sleep(3000);
				processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(FilePath));
				var process =  processes.FirstOrDefault();
				Logger.ToFile($"{nameof(StartProcess)}: {(process != null ? "Process found." : "Process not found.")}");
				return process;
			}
			Debug.Assert(processStarted != null, nameof(processStarted) + " != null");
			var exited = processStarted.HasExited;
			if (!exited) processStarted.WaitForInputIdle(3000);
			Logger.ToFile($"{nameof(StartProcess)}: {(exited ? "Exited immediately." : "Started.")}");
			return processStarted;
		}
	}
}