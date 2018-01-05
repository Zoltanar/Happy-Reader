using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Happy_Apps_Core
{
	/// <summary>
	/// From get character commands
	/// </summary>
	public class CharacterItem
	{
	    private VNItem[] _vNs;
	    private TraitItem[] _traits;

	    [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
		[Column("CharacterID")]
		public int ID { get; set; }
		public string Name { get; set; }
		public string Image { get; set; }
		[Column("Traits")]
		public string TraitsColumn { get; set; }
		[Column("VNs")]
		public string VNsColumn { get; set; }
		public DateTime? DateUpdated { get; set; }

	    [NotMapped, NotNull]
        public TraitItem[] Traits => _traits ?? (_traits = JsonConvert.DeserializeObject<TraitItem[]>(TraitsColumn) ?? new TraitItem[] { });

        [NotMapped,NotNull]
	    public VNItem[] VNs => _vNs ?? (_vNs = JsonConvert.DeserializeObject<VNItem[]>(VNsColumn) ?? new VNItem[] { });

	    [NotMapped]
		public string Description { get; set; }
		[NotMapped]
		public string Aliases { get; set; }

		public bool CharacterIsInVN(int vnid)
		{
			IEnumerable<int> idList = VNs.Select(x => x.ID);
			return idList.Contains(vnid);
		}
		public bool ContainsTraits(IEnumerable<DumpFiles.WrittenTrait> traitFilters)
		{
			//remove all numbers in traits from traitIDs, if nothing is left then it matched all
			int[] traits = Traits.Select(x => x.ID).ToArray();
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