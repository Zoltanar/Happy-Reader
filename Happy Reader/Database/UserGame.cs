using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Happy_Apps_Core.Database;
using IthVnrSharpLib;
using JetBrains.Annotations;
using StaticHelpers = Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.Database
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class UserGame : INotifyPropertyChanged
    {
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
		public bool MergeByHookCode { get; set; }
	    public string ProcessName { get; set; }
	    // ReSharper disable once InconsistentNaming
		public DateTime TimeOpenDT { get; set; }
		public EncodingEnum PrefEncodingEnum { get; set; }
	    public bool IsRunning => _process != null;
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
                    if (VNID != null) _vn = StaticHelpers.LocalDatabase.LocalVisualNovels.SingleOrDefault(x => x.VNID == VNID);
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
				if(value == null) _process.Dispose();
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
        [NotMapped]
        public string DisplayName =>
            UserDefinedName ?? StaticMethods.TruncateStringFunction30(VN?.Title ?? Path.GetFileNameWithoutExtension(FilePath));
        [NotMapped]
        public BitmapImage Image
        {
            get
            {
                Bitmap image = null;
                if (!File.Exists(FilePath))
                {
                    // ReSharper disable once PossibleNullReferenceException
                    Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/file-not-found.png")).Stream;
                    image = new Bitmap(iconStream);
                }
                else if (VN == null) image = Icon.ExtractAssociatedIcon(FilePath)?.ToBitmap();
                else if (File.Exists(VN.StoredCover)) image = new Bitmap(VN.StoredCover);
                if (image == null)
                {
					// ReSharper disable once PossibleNullReferenceException
					Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/no-image.png")).Stream;
                    image = new Bitmap(iconStream);
                }
                using (MemoryStream memory = new MemoryStream())
                {
                    image.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage.BeginInit();
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();
                    return bitmapimage;
                }
            }
        }
		[NotMapped]
	    public Encoding PrefEncoding
	    {
		    get => Encodings[(int)PrefEncodingEnum];
			set
			{
				var index = Array.IndexOf(Encodings, value);
				PrefEncodingEnum = (EncodingEnum) index;
			}
	    }

	    [NotMapped]
	    public string MonthGroupingString
	    {
		    get
		    {
			    if (!File.Exists(FilePath)) return "File not found"; //DateTime.MinValue;
			    if (VN == null) return "Other"; //DateTime.MinValue.AddDays(1);
			    if (IsLastPlayed(5)) return "Last Played";
			    var dt = VN.ReleaseDate;
			    var newDt = new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
			    return string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:MMMM} {0:yyyy}",newDt);
		    }
	    }
	    [NotMapped]
	    public DateTime MonthGrouping
	    {
		    get
		    {
			    if (!File.Exists(FilePath)) return DateTime.MinValue;
			    if (VN == null) return DateTime.MinValue.AddDays(1);
			    if (IsLastPlayed(5)) return DateTime.MaxValue;
			    var dt = VN.ReleaseDate;
			    var newDt = new DateTime(dt.Year, dt.Month, DateTime.DaysInMonth(dt.Year, dt.Month));
			    return newDt;
		    }
	    }

		private bool IsLastPlayed(int max)
	    {
		    var lastPlayed = StaticMethods.Data.Logs.Local.Where(x => x.Kind == LogKind.TimePlayed).OrderByDescending(x => x.Timestamp).Select(x => x.AssociatedId).Distinct().Take(max);
			return lastPlayed.Contains(Id);
	    }

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

		private void ProcessExited(object sender, EventArgs e) => SaveTimePlayed(true);

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void SaveUserDefinedName([NotNull]string text)
        {
            UserDefinedName = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            StaticMethods.Data.SaveChanges();
            OnPropertyChanged(nameof(DisplayName));
        }

        public void SaveVNID(int? vnid)
        {
            VNID = vnid;
            StaticMethods.Data.SaveChanges();
            VN = vnid == null ? null : StaticHelpers.LocalDatabase.LocalVisualNovels.SingleOrDefault(x => x.VNID == vnid);
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(Image));
            OnPropertyChanged(nameof(VN));
        }

        public void SaveHookCode([NotNull]string text)
        {
            HookCode = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
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

		public void SaveLaunchPath([NotNull]string text)
		{
			LaunchPath = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
			StaticMethods.Data.SaveChanges();
			OnPropertyChanged(nameof(LaunchPath));
		}

	    public override string ToString() => DisplayName;

	    public static Encoding[] Encodings => IthVnrViewModel.Encodings;
		public enum EncodingEnum
	    {
		    // ReSharper disable once InconsistentNaming
			ShiftJis, UTF8, Unicode
	    }
    }

}
