using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Reader.Database
{

    public class Entry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long UserId { get; set; }

        public User User { get; set; }

        [Required]
        public string Input { get; set; }
        
        public string Output { get; set; }

        public long? FileId { get; set; }

        public long? GameId { get; set; }

        public Game Game { get; set; }

        public bool SeriesSpecific { get; set; }

        public bool Private { get; set; }

        public double Priority { get; set; }
        
        public EntryType Type { get; set; }
        
        public string RoleString { get; set; }
        
        public bool Disabled { get; set; }

        public int? UserHash { get; set; }

        public string Host { get; set; }

        public string FromLanguage { get; set; }

        public string ToLanguage { get; set; }

        public DateTime? Time { get; set; }

        public DateTime? UpdateTime { get; set; }

        public long UpdateUserId { get; set; }

        public bool CaseInsensitive { get; set; }

        public bool PhraseBoundary { get; set; }

        public bool Regex { get; set; }

        public bool Hentai { get; set; }

        public string Context { get; set; }

        public string Ruby { get; set; }

        public string Comment { get; set; }

        public string UpdateComment { get; set; }
        
        [NotMapped]
        public RoleProxy AssignedProxy { get; set; }

    }
}
