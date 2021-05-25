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
									 "(Id, UserId, Input, Output, GameId, SeriesSpecific, Private, Priority, Regex, Comment, Type, RoleString, Disabled, Time, UpdateTime, UpdateUserId, UpdateComment, GameIdIsUserGame) " +
									 "VALUES " +
									 "(@Id, @UserId, @Input, @Output, @GameId, @SeriesSpecific, @Private, @Priority, @Regex, @Comment, @Type, @RoleString, @Disabled, @Time, @UpdateTime, @UpdateUserId, @UpdateComment, @GameIdIsUserGame)";
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
			command.AddParameter("@GameIdIsUserGame", GameIdIsUserGame);
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
			GameIdIsUserGame = Convert.ToInt32(reader["GameIdIsUserGame"]) == 1;
			Loaded = true;
		}

		public long Id { get; set; }
		public int UserId { get; set; }
		public string Input { get; set; }
		public string Output { get; set; } = string.Empty;
		private int? GameId { get; set; }
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
		private bool GameIdIsUserGame { get; set; }
		public EntryGame GameData { get; private set; }

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

		public void SetGameId(int? gameId, bool isUserGameId)
		{
			GameId = gameId; 
			GameIdIsUserGame = isUserGameId;
			GameData = new EntryGame(GameId, GameIdIsUserGame,false);
			ReadyToUpsert = true;
		}

		private sealed class EntryClashComparer : IEqualityComparer<Entry>
		{
			//todo remove rolestring?

			public bool Equals(Entry x, Entry y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				//both private to same user OR neither private
				//if one is private and another isn't, they don't clash
				var result =
					((x.UserId == y.UserId && x.Private && y.Private) || !x.Private && !y.Private)
							 //both series-specific to same game OR neither series specific
							 //if one is series specific and another isn't, they don't clash
							 && ((x.GameId == y.GameId && x.SeriesSpecific && y.SeriesSpecific) || !x.SeriesSpecific && !y.SeriesSpecific)
							 && x.Type == y.Type
							 && x.RoleString == y.RoleString
							 //only one entry can be used for the input, regardless of output or priority
							 && x.Input == y.Input;
				return result;
			}

			public int GetHashCode(Entry obj)
			{
				unchecked
				{
					//if not private, user id doesn't matter
					var hashCode = obj.Private ? obj.UserId.GetHashCode() : 0;
					//if not series specific, game id doesn't matter
					if (obj.SeriesSpecific) hashCode = (hashCode * 397) ^ obj.GameId.GetHashCode();
					hashCode = (hashCode * 397) ^ (int)obj.Type;
					hashCode = (hashCode * 397) ^ (obj.RoleString != null ? obj.RoleString.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (obj.Input != null ? obj.Input.GetHashCode() : 0);
					return hashCode;
				}
			}
		}

		public static IEqualityComparer<Entry> ClashComparer { get; } = new EntryClashComparer();

		public void InitGameId()
		{
			SetGameId(GameId, GameIdIsUserGame);
			ReadyToUpsert = false;
		}
	}
}
