using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Happy_Apps_Core.Database;

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
	    }

	    [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
		[Column("CharacterID")]
		public int ID { get; set; }
		public string Name { get; set; }
		public string Image { get; set; }
		public DateTime? DateUpdated { get; set; }

	    /// <summary>
	    /// Only used in json convert from vndb
	    /// </summary>
        [NotMapped]
	    public VNItem[] VNs { get; set; }

        /// <summary>
        /// Only used in json convert from vndb
        /// </summary>
        [NotMapped]
        public TraitItem[] Traits { get; set; }

	    public virtual ICollection<DbTrait> DbTraits { get; set; }

	    public virtual ICollection<CharacterVN> CharacterVns { get; set; }
        
	    [NotMapped]
		public string Description { get; set; }
		[NotMapped]
		public string Aliases { get; set; }
        
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

	}
}