using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Reader.Database
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class Game
    {
        public Game()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Hooks = new HashSet<GameHook>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        [Required]
        public string Title { get; set; }
        public string RomajiTitle { get; set; }
        public string Brand { get; set; }
        public string Tags { get; set; }
        public string Image { get; set; }
        public string Wiki { get; set; }
        public string Date { get; set; }
        public string Artists { get; set; }
        public string Musicians { get; set; }
        public bool Otome { get; set; }
        public bool Ecchi { get; set; }
        public string Series { get; set; }
        public string Writers { get; set; }
        public string Banner { get; set; }
        public bool Okazu { get; set; }
        // ReSharper disable once InconsistentNaming
        public string SDArtists { get; set; }

        // ReSharper disable once MemberCanBeProtected.Global
        public virtual ICollection<GameHook> Hooks { get; set; }
    }
}
