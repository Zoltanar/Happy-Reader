using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Happy_Apps_Core;
using JetBrains.Annotations;

namespace Happy_Reader.Database
{

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class UserGame : INotifyPropertyChanged
    {
        public UserGame(string file, ListedVN vn)
        {
            FilePath = file;
            FileName = Path.GetFileName(file);
            FolderName = Path.GetDirectoryName(file);
            Language = vn?.LanguagesObject?.Originals?.FirstOrDefault() ?? "ja";
            VNID = vn?.VNID;
            VN = vn;
        }

        public UserGame() { }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public string UserDefinedName { get; set; }

        [Required]
        public string Language { get; set; }

        public string FileName { get; set; }

        public string FolderName { get; set; }

        [NotMapped]
        public bool HookToProcess
        {
            get => HookProcess ?? false;
            set
            {
                HookProcess = value;
                StaticMethods.Data.SaveChanges();
            }
        }

        public bool? HookProcess { get; set; }

        public string WindowName { get; set; }

        public bool IgnoresRepeat { get; set; }

        public int? VNID { get; set; }

        public string FilePath { get; set; }

        public TimeSpan TimeOpen { get; set; }

        [NotMapped]
        public ListedVN VN { get; set; }

        private Stopwatch _runningTime;

        private Process _process;

        public bool IsRunning => _process != null;

        [NotMapped]
        public Process Process
        {
            get => _process;
            set
            {
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
        public string DisplayName => UserDefinedName ?? StaticHelpers.TruncateString(VN?.Title ?? Path.GetFileNameWithoutExtension(FileName), 30);

        [NotMapped]
        public BitmapImage Image
        {
            get
            {
                Bitmap image = null;
                if (VN == null && FilePath != null)
                {
                    if (File.Exists(FilePath)) image = Icon.ExtractAssociatedIcon(FilePath)?.ToBitmap();
                    else
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/file-not-found.png")).Stream;
                        image = new Bitmap(iconStream);
                    }
                }
                else if (VN != null)
                {
                    if (File.Exists(VN.StoredCover)) image = new Bitmap(VN.StoredCover);
                }
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

        public string ProcessName { get; set; }

        private void ProcessExited(object sender, EventArgs e)
        {
            _runningTime.Stop();
            TimeOpen += _runningTime.Elapsed;
            Process = null;
            var log = Log.NewTimePlayedLog(Id, _runningTime.Elapsed);
            StaticMethods.Data.Logs.Add(log);
            StaticMethods.Data.SaveChanges();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void SaveUserDefinedName([NotNull]string text)
        {
            UserDefinedName = text.Trim();
            StaticMethods.Data.SaveChanges();
            OnPropertyChanged(nameof(DisplayName));
        }

        public void SaveVNID(int? vnid)
        {
            VNID = vnid;
            StaticMethods.Data.SaveChanges();
            VN = vnid == null ? null : StaticHelpers.LocalDatabase.VisualNovels.SingleOrDefault(x => x.VNID == vnid);
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(Image));
            OnPropertyChanged(nameof(VN));
        }

        public void ChangeFilePath(string newFilePath)
        {
            FilePath = newFilePath;
            StaticMethods.Data.SaveChanges();
            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(Image));
        }
    }
}
