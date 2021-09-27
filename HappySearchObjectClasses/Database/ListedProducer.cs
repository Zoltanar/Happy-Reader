using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.DataAccess;
using JetBrains.Annotations;

namespace Happy_Apps_Core.Database
{
	/// <summary>
	/// Object for Favorite Producers in Object List View.
	/// </summary>
	public sealed class ListedProducer : IComparable<ListedProducer>, IComparable, INotifyPropertyChanged, IDataItem<int>, IDumpItem
	{
		/// <summary>
		/// Constructor for ListedProducer, not favorite producers.
		/// </summary>
		/// <param name="name">Producer Name</param>
		/// <param name="id">Producer ID</param>
		/// <param name="language">Language of producer</param>
		public ListedProducer(string name, int id, string language)
		{
			Name = name;
			ID = id;
			Language = language;
		}

		/// <summary>
		/// Constructor for ListedProducer for favorite producers.
		/// </summary>
		/// <param name="name">Producer Name</param>
		/// <param name="id">Producer ID</param>
		/// <param name="language">Language of producer</param>
		/// <param name="userAverageVote">User's average vote on Producer titles. (Only titles with votes)</param>
		/// <param name="userDropRate">User's average drop rate on Producer titles. (Dropped / (Finished+Dropped)</param>
		public ListedProducer(string name, int id, string language, double userAverageVote, int userDropRate)
		{
			Name = name;
			ID = id;
			Language = language;
			UserAverageVote = Math.Round(userAverageVote, 2);
			UserDropRate = userDropRate;
		}

		/// <summary>
		/// Number of Producer's titles
		/// </summary>
		[NotMapped]
		public int NumberOfTitles => Titles.Count();

		/// <summary>
		/// User's average vote on Producer titles. (Only titles with votes)
		/// </summary>
		[NotMapped]
		public double? UserAverageVote { get; set; }

		/// <summary>
		/// User's average drop rate on Producer titles. (Dropped / (Finished+Dropped)
		/// </summary>
		[NotMapped]
		public double? UserDropRate { get; set; }

		[NotMapped]
		public bool IsFavorited { get; set; }


		/// <summary>
		/// Bayesian average score of votes by all users.
		/// </summary>
		[NotMapped]
		public double? GeneralRating { get; set; }

		public ListedProducer()
		{
		}

		public IEnumerable<ListedVN> Titles => StaticHelpers.LocalDatabase.VisualNovels.Where(v => v.ProducerID == ID);

		#region Columns

		/// <summary>
		/// Producer ID
		/// </summary>
		[Key]
		[Column("ProducerID")]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public int ID { get; set; }

		/// <summary>
		/// Producer Name
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Language of Producer
		/// </summary>
		public string Language { get; set; }
		
		#endregion
		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString() => $"ID={ID} Name={Name}";

		public int CompareTo(object other)
		{
			if (ReferenceEquals(this, other)) return 0;
			return other switch
			{
				null => 1,
				ListedProducer otherProducer => ID.CompareTo(otherProducer.ID),
				_ => 1
			};
		}

		public int CompareTo(ListedProducer other)
		{
			if (ReferenceEquals(this, other)) return 0;
			return other is null ? 1 : ID.CompareTo(other.ID);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private bool _userDataSet;

		public void SetFavoriteProducerData(VisualNovelDatabase database)
		{
			if (_userDataSet) return;
			_userDataSet = true;
			IsFavorited = database.CurrentUser.FavoriteProducers.Contains(this);
			if (!IsFavorited) { }
			var titleIds = Titles.Select(x => x.VNID).ToList();
			var userTitles = database.UserVisualNovels.Where(x => titleIds.Contains(x.VNID) && x.UserId == database.CurrentUser.Id).ToList();
			var userTitleVotes = userTitles.Where(x => x.Vote != null).Select(x => x.Vote).ToList();
			UserAverageVote = userTitleVotes.Any() ? userTitleVotes.Average() / 10 : null;
			var titlesDropped = userTitles.Count(x => x.Labels.Contains(UserVN.LabelKind.Dropped));
			var titlesFinished = userTitles.Count(x => x.Labels.Contains(UserVN.LabelKind.Finished));
			var titlesDroppedOrFinished = titlesDropped + titlesFinished;
			UserDropRate = titlesDroppedOrFinished > 0 ? (double?)(titlesDropped / (double)(titlesFinished + titlesDropped)) : null;
			GeneralRating = Titles.Any() ? (double?)Titles.Average(x => x.Rating) : null;
		}

		#region IDataItem Implementation
		string IDataItem<int>.KeyField => "ProducerId";

		int IDataItem<int>.Key => ID;

		DbCommand IDataItem<int>.UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $@"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO ListedProducers(ProducerId,Name,Language) VALUES (@ProducerId,@Name,@Language)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@ProducerId", ID);
			command.AddParameter("@Name", Name);
			command.AddParameter("@Language", Language);
			return command;
		}

		void IDataItem<int>.LoadFromReader(System.Data.IDataRecord reader)
		{
			if (reader == null) throw new ArgumentNullException(nameof(reader));
			try
			{
				ID = Convert.ToInt32(reader["ProducerId"]);
				var nameObject = reader["Name"];
				if (!nameObject.Equals(DBNull.Value)) Name = Convert.ToString(nameObject);
				var languageObject = reader["Language"];
				if (!languageObject.Equals(DBNull.Value)) Language = Convert.ToString(languageObject);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}

		void IDumpItem.LoadFromStringParts(string[] parts)
		{
			ID = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			Name = GetPart(parts, "name");
			Language = GetPart(parts, "lang");
		}

		public static Dictionary<string, int> Headers = new();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		#endregion
	}
}