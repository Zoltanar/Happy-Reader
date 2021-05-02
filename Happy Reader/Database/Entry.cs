using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Reader.Database
{

	public class Entry : INotifyPropertyChanged, IDataItem<long>, IReadyToUpsert
	{
		public string KeyField => nameof(Id);
		public long Key => Id;
		public bool ReadyToUpsert { get; set; }
		public bool Loaded { get; private set; }

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			if (Id == 0)
			{
				throw new InvalidOperationException("Id should have been set.");
			}
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(Entry)}s" +
									 "(Id, UserId, Input, Output, GameId, SeriesSpecific, Private, Priority, Regex, Comment, Type, RoleString, Disabled, Time, UpdateTime, UpdateUserId, UpdateComment) " +
									 "VALUES " +
									 "(@Id, @UserId, @Input, @Output, @GameId, @SeriesSpecific, @Private, @Priority, @Regex, @Comment, @Type, @RoleString, @Disabled, @Time, @UpdateTime, @UpdateUserId, @UpdateComment)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@Id", Id);
			command.AddParameter("@UserId", UserId);
			command.AddParameter("@Input", Input);
			command.AddParameter("@Output", Output);
			command.AddParameter("@GameId", GameId);
			command.AddParameter("@SeriesSpecific", SeriesSpecific);
			command.AddParameter("@Private", Private);
			command.AddParameter("@Priority", Priority);
			command.AddParameter("@Regex", Regex);
			command.AddParameter("@Comment", Comment);
			command.AddParameter("@Type", Type);
			command.AddParameter("@RoleString", RoleString);
			command.AddParameter("@Disabled", Disabled);
			command.AddParameter("@Time", Time);
			command.AddParameter("@UpdateTime", UpdateTime);
			command.AddParameter("@UpdateUserId", UpdateUserId);
			command.AddParameter("@UpdateComment", UpdateComment);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			Id = Convert.ToInt32(reader["Id"]);
			UserId = Convert.ToInt32(reader["UserId"]);
			Input = Convert.ToString(reader["Input"]);
			Output = Convert.ToString(reader["Output"]);
			GameId = StaticHelpers.GetNullableInt(reader["GameId"]);
			SeriesSpecific = Convert.ToInt32(reader["SeriesSpecific"]) == 1;
			Private = Convert.ToInt32(reader["Private"]) == 1;
			Priority = Convert.ToDouble(reader["Priority"]);
			Regex = Convert.ToInt32(reader["Regex"]) == 1;
			Comment = Convert.ToString(reader["Comment"]);
			Type = (EntryType)Convert.ToInt32(reader["Type"]);
			RoleString = Convert.ToString(reader["RoleString"]);
			Disabled = Convert.ToInt32(reader["Disabled"]) == 1;
			Time = Convert.ToDateTime(reader["Time"]);
			UpdateTime = StaticHelpers.GetNullableDate(reader["UpdateTime"]);
			UpdateUserId = Convert.ToInt32(reader["UpdateUserId"]);
			UpdateComment = Convert.ToString(reader["UpdateComment"]);
			Loaded = true;
		}
		
		public long Id { get; set; }
		public int UserId { get; set; }
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
		public ListedVN Game => GameId == null ? null : StaticHelpers.LocalDatabase.VisualNovels[GameId.Value];
		public User User => StaticHelpers.LocalDatabase.Users[UserId];
		public RoleProxy AssignedProxy { get; set; }
		/// <summary>
		/// Location in string
		/// </summary>
		public int Location { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
