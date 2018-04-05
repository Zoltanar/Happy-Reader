using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Reader.Database
{

    public class Entry : INotifyPropertyChanged
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int UserId { get; set; }
        
        [Required]
        public string Input { get; set; }
        public string Output { get; set; }
        public int? GameId { get; set; }
        public bool SeriesSpecific { get; set; }
        public bool Private { get; set; }
        public double Priority { get; set; }
		public bool Regex { get; set; }
	    public string Comment { get; set; }
		public EntryType Type { get; set; }
        public string RoleString { get; set; }
        public bool Disabled { get; set; }
        public DateTime Time { get; set; }
        public DateTime? UpdateTime { get; set; }
        public long UpdateUserId { get; set; }
	    public string UpdateComment { get; set; }


	    [NotMapped]
	    public ListedVN Game => GameId == null ? null : StaticHelpers.LocalDatabase.VisualNovels.SingleOrDefault(x => x.VNID == GameId);
		[NotMapped]
	    public User User => StaticHelpers.LocalDatabase.Users.SingleOrDefault(x => x.Id == UserId);
		[NotMapped]
        public RoleProxy AssignedProxy { get; set; }

	    public event PropertyChangedEventHandler PropertyChanged;

	    [NotifyPropertyChangedInvocator]
	    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
	    {
		    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }

		public Entry() { }

	    public Entry(string input, string output)
	    {
		    Input = input;
		    Output = output;
		    Type = EntryType.Name;
	    }
    }
}
