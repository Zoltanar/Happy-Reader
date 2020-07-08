using System;
using System.Collections.Generic;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
// ReSharper disable InconsistentNaming

namespace Happy_Apps_Core
{
	public partial class CharacterItem : IDumpItem
	{
		[UsedImplicitly]
		public  class TraitItem : List<int>
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

		[UsedImplicitly]
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

		[UsedImplicitly]
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