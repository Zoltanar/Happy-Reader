using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows.Media;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

// ReSharper disable VirtualMemberCallInConstructor

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get character commands
    /// </summary>
    public class CharacterItem
    {
        public CharacterItem()
        {
            DbTraits = new HashSet<DbTrait>();
            CharacterVns = new HashSet<CharacterVN>();
            DbStaff = new HashSet<CharacterStaff>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }

        #region Basic Flag
        public string Name { get; set; }
        public string Original { get; set; }
        public string Gender { get; set; }
        public string BloodT { get; set; }
        /// <summary>
        /// Only used in json convert from vndb
        /// </summary>
        [NotMapped]
        public int?[] Birthday { get; set; }
        public DateTime? BirthDate { get; set; }
        #endregion

        #region Details Flag
        public string Aliases { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        #endregion

        public DateTime? DateUpdated { get; set; }

        /// <summary>
        /// Only used in json convert from vndb
        /// </summary>
        [NotMapped]
        public TraitItem[] Traits { get; set; }

        /// <summary>
        /// Only used in json convert from vndb
        /// </summary>
        [NotMapped]
        public VNItem[] VNs { get; set; }

        [NotMapped]
        public StaffItem[] Voiced { get; set; }

        public virtual ICollection<DbTrait> DbTraits { get; set; }
        public virtual ICollection<CharacterVN> CharacterVns { get; set; }
        public virtual ICollection<CharacterStaff> DbStaff { get; set; }

        [NotMapped]
        public Brush BackBrush
        {
            get
            {
                switch (AttachedVN.Role)
                {
                    case "main":
                        return Brushes.Gold;
                    case "primary":
                        return Brushes.Orchid;
                    case "side":
                        return Brushes.GreenYellow;
                    case "appears":
                        return Brushes.LightBlue;
                        default: return Brushes.White;
                }
            }
        }

        public string GenderSymbol
        {
            get
            {
                switch (Gender)
                {
                    case "f": return "♀";
                    case "m": return "♂";
                    case "b": return "⚤";
                    default: return "";

                }
            }
        }

        [NotMapped, NotNull]
        public IEnumerable<string> DisplayTraits
        {
            get
            {
                if (ID == 0 || DbTraits.Count == 0) return new List<string>();
                var stringList = new List<string> { $"{DbTraits.Count} Traits" };
                stringList.AddRange(DbTraits.Select(trait => DumpFiles.PlainTraits.Find(x => x.ID == trait.TraitId)?.ToString()));
                return stringList;
            }
        }

        [NotMapped]
        public CharacterVN AttachedVN { get; set; }

        public bool ContainsTraits(IEnumerable<DumpFiles.WrittenTrait> traitFilters)
        {
            //remove all numbers in traits from traitIDs, if nothing is left then it matched all
            int[] traits = DbTraits.Select(x => x.TraitId).ToArray();
            return traitFilters.All(writtenTrait => traits.Any(characterTrait => writtenTrait.AllIDs.Contains(characterTrait)));
        }

        public class TraitItem : List<int>
        {
            public int ID
            {
                get => this[0];
                set => this[0] = value;
            }
            public int Spoiler
            {
                get => this[1];
                set => this[1] = value;
            }
        }

        public class VNItem : List<object>
        {
            public int ID
            {
                get => Convert.ToInt32(this[0]);
                set => this[0] = value;
            }
            public int RID
            {
                get => Convert.ToInt32(this[1]);
                set => this[1] = value;
            }
            public int Spoiler
            {
                get => Convert.ToInt32(this[2]);
                set => this[2] = value;
            }
            public string Role
            {
                get => Convert.ToString(this[3]);
                set => this[3] = value;
            }
        }

        public class StaffItem
        {
            public int ID { get; set; }
            /// <summary>
            /// the staff alias ID being used
            /// </summary>
	        public int AID { get; set; }
            public int VID { get; set; }
            public string Note { get; set; }
        }

    }
}