using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using JetBrains.Annotations;

namespace Happy_Reader.Database
{

    public class UserGame : INotifyPropertyChanged
    {
        public UserGame(string file, ListedVN vn)
        {
            FilePath = file;
            FileName = Path.GetFileName(file);
            FolderName = Path.GetDirectoryName(file);
            Language = vn.Languages.Originals.FirstOrDefault();
            VNID = vn.VNID;
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
                _runningTime = Stopwatch.StartNew();
                _process.Exited += ProcessExited;
                _process.EnableRaisingEvents = true;
            }
        }

        [NotMapped]
        public object DisplayName => UserDefinedName ?? VN?.Title ?? FileName;

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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
