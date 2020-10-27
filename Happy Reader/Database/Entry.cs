using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
		public string Output { get; set; } = string.Empty;
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
		public ListedVN Game => GameId == null ? null : StaticHelpers.LocalDatabase.VisualNovels[GameId.Value];
		[NotMapped]
		public User User => StaticHelpers.LocalDatabase.Users[UserId];
		[NotMapped]
		public RoleProxy AssignedProxy { get; set; }
		/// <summary>
		/// Location in string
		/// </summary>
		[NotMapped]
		public int Location { get; set; }

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

		public override string ToString() => $"[{Id}] {Input} > {Output}";

		private sealed class EntryEqualityComparer : IEqualityComparer<Entry>
		{
			public bool Equals(Entry x, Entry y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				return x.UserId == y.UserId && x.Input == y.Input && x.Output == y.Output && x.GameId == y.GameId && x.SeriesSpecific == y.SeriesSpecific && x.Private == y.Private && x.Priority.Equals(y.Priority) && x.Type == y.Type && x.RoleString == y.RoleString;
			}

			public int GetHashCode(Entry obj)
			{
				unchecked
				{
					var hashCode = obj.UserId;
					hashCode = (hashCode * 397) ^ (obj.Input != null ? obj.Input.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (obj.Output != null ? obj.Output.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ obj.GameId.GetHashCode();
					hashCode = (hashCode * 397) ^ obj.SeriesSpecific.GetHashCode();
					hashCode = (hashCode * 397) ^ obj.Private.GetHashCode();
					hashCode = (hashCode * 397) ^ obj.Priority.GetHashCode();
					hashCode = (hashCode * 397) ^ (int) obj.Type;
					hashCode = (hashCode * 397) ^ (obj.RoleString != null ? obj.RoleString.GetHashCode() : 0);
					return hashCode;
				}
			}
		}

		public static IEqualityComparer<Entry> ValueComparer { get; } = new EntryEqualityComparer();
	}
}
