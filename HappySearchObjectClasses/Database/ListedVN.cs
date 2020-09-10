using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.DataAccess;
using JetBrains.Annotations;
using Newtonsoft.Json;


namespace Happy_Apps_Core.Database
{
	public class ListedVN : INotifyPropertyChanged, IDataItem<int>, IDumpItem
	{

		#region Columns
		/// <summary>
		/// VN's ID
		/// </summary>
		public int VNID { get; set; }

		/// <summary>
		/// VN title
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// VN kanji title
		/// </summary>
		public string KanjiTitle { get; set; }

		/// <summary>
		/// VN's first non-trial release date, set by calling SetReleaseDate(string)
		/// </summary>
		public string ReleaseDateString { get; set; }

		public int? ProducerID
		{
			get => _producerID;
			set
			{
				if (_producerID == value) return;
				_producerID = value;
				_producerSet = false;
			}
		}

		private bool _producerSet;

		public ListedProducer Producer
		{
			get
			{
				if (!_producerSet)
				{
					if (ProducerID.HasValue && ProducerID.Value >= 0)
					{
						_producer = StaticHelpers.LocalDatabase.Producers[ProducerID.Value];
					}
					_producerSet = true;
				}
				return _producer;
			}
		}

		public string ImageId { get; set; }

		/// <summary>
		/// Is VN's cover NSFW?
		/// </summary>
		public bool ImageNSFW { get; set; }

		/// <summary>
		/// VN description
		/// </summary>
		public string Description { get; set; }

		public LengthFilterEnum? LengthTime { get; set; }

		/// <summary>
		/// Popularity of VN, percentage of most popular VN
		/// </summary>
		public double Popularity { get; set; }

		/// <summary>
		/// Bayesian rating of VN, 1-10
		/// </summary>
		public double Rating { get; set; }

		/// <summary>
		/// Number of votes cast on VN
		/// </summary>
		public int VoteCount { get; set; }

		/// <summary>
		/// JSON Array string containing List of Relation Items
		/// </summary>
		private string Relations { get; set; }

		/// <summary>
		/// JSON Array string containing List of Screenshot Items
		/// </summary>
		private string Screens { get; set; }

		/// <summary>
		/// JSON Array string containing List of Anime Items
		/// </summary>
		private string Anime { get; set; }

		/// <summary>
		/// Newline separated string of aliases
		/// </summary>
		public string Aliases { get; set; }

		public string Languages { get; set; }

		public UserVN UserVN => StaticHelpers.LocalDatabase.UserVisualNovels[(StaticHelpers.CSettings.UserID, VNID)];

		public DateTime ReleaseDate { get; set; }

		public IEnumerable<DbTag> Tags => StaticHelpers.LocalDatabase.Tags[VNID];

		public string ReleaseLink { get; set; }

		#endregion

		public void SetReleaseDate(string releaseDateString)
		{
			ReleaseDateString = releaseDateString;
			ReleaseDate = StaticHelpers.StringToDate(releaseDateString, out var hasFullDate);
			_hasFullDate = hasFullDate;
		}

		private bool? _hasFullDate;

		public bool HasFullDate
		{
			get
			{
				if (_hasFullDate == null)
				{
					SetReleaseDate(ReleaseDateString);
				}
				Debug.Assert(_hasFullDate != null, nameof(_hasFullDate) + " != null");
				return _hasFullDate.Value;
			}
		}

		[NotNull]
		public IEnumerable<string> DisplayRelations
		{
			get
			{
				if (!RelationsObject.Any()) return new List<string> { "No relations found." };
				var titleString = RelationsObject.Length == 1 ? "1 Relation" : $"{RelationsObject.Length} Relations";
				var stringList = new List<string> { titleString, "--------------" };
				stringList.AddRange(RelationsObject.Select(r => r.Print()));
				return stringList;
			}
		}

		private static readonly string[] NoAnimeFound = { "No Anime found." };

		[NotNull]
		public IEnumerable<string> DisplayAnime
		{
			get
			{
				switch (AnimeObject.Length)
				{
					case 0:
						//static to save memory and performance
						return NoAnimeFound;
					case 1:
						return new[] { AnimeObject.First().Print() };
					default:
						var titleString = $"{AnimeObject.Length} Anime";
						var stringList = new List<string> { titleString, "--------------" };
						stringList.AddRange(AnimeObject.Select(x => x.Print()));
						return stringList;
				}
			}
		}


		/// <summary>
		/// Newline separated string of aliases
		/// </summary>
		public string DisplayAliases => Aliases?.Replace("\n", ", ") ?? "";

		[NotNull]
		public RelationsItem[] RelationsObject
		{
			get
			{
				if (_relationsObject != null) return _relationsObject;
				if (string.IsNullOrWhiteSpace(Relations)) _relationsObject = Array.Empty<RelationsItem>();
				else _relationsObject = JsonConvert.DeserializeObject<RelationsItem[]>(Relations) ?? Array.Empty<RelationsItem>();
				foreach (var r in _relationsObject)
				{
					if (string.IsNullOrWhiteSpace(r.Title))
						r.Title = StaticHelpers.LocalDatabase.VisualNovels[r.ID]?.Title;
				}
				return _relationsObject;
			}
		}

		[NotNull]
		public AnimeItem[] AnimeObject
		{
			get
			{
				if (_animeObject != null) return _animeObject;
				if (string.IsNullOrWhiteSpace(Anime)) _animeObject = Array.Empty<AnimeItem>();
				else _animeObject = JsonConvert.DeserializeObject<AnimeItem[]>(Anime) ?? Array.Empty<AnimeItem>();
				return _animeObject;
			}
		}

		[NotNull]
		public ScreenItem[] ScreensObject
		{
			get
			{
				if (_screensObject != null) return _screensObject;
				if (string.IsNullOrWhiteSpace(Screens)) _screensObject = Array.Empty<ScreenItem>();
				else _screensObject = JsonConvert.DeserializeObject<ScreenItem[]>(Screens) ?? Array.Empty<ScreenItem>();
				return _screensObject;
			}
		}

		private RelationsItem[] _relationsObject;
		private AnimeItem[] _animeObject;
		private ScreenItem[] _screensObject;
		private VNLanguages _languagesObject;
		private ListedProducer _producer;
		private int? _producerID;
		
		public SuggestionScoreObject Suggestion { get; set; }

		/// <summary>
		/// Language of producer
		/// </summary>
		[NotNull]
		public VNLanguages LanguagesObject => _languagesObject ??= Languages == null ? new VNLanguages() : JsonConvert.DeserializeObject<VNLanguages>(Languages) ?? new VNLanguages();

		/// <summary>
		/// Return unreleased status of vn.
		/// </summary>
		public ReleaseStatusEnum ReleaseStatus
		{
			get
			{
				if (ReleaseDate == DateTime.MaxValue) return ReleaseStatusEnum.WithoutReleaseDate;
				return ReleaseDate > DateTime.Today ? ReleaseStatusEnum.WithReleaseDate : ReleaseStatusEnum.Released;
			}
		}

		/// <summary>
		/// Gets voted status of vn.
		/// </summary>
		public bool Voted => UserVN != null && UserVN.Vote >= 1;

		public bool HasAnime => AnimeObject.Any();

		private OwnedStatus? _ownedStatus;

		public OwnedStatus IsOwned
		{
			get
			{
				_ownedStatus ??= VnIsOwned?.Invoke(VNID) ?? OwnedStatus.NeverOwned;
				return _ownedStatus.Value;
			}
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString() => $"[{VNID}] {Title}";

		/// <summary>
		/// Get VN's User-related status as a string.
		/// </summary>
		/// <returns>User-related status</returns>
		public string UserRelatedStatus
		{
			get
			{
				string[] parts = { "", "", "" };
				var label = UserVN?.Labels.FirstOrDefault(l => l != UserVN.LabelKind.Voted && l != UserVN.LabelKind.Wishlist);
				if (label != null)
				{
					parts[0] = "Label: ";
					parts[1] = label.GetDescription();
				}
				if (UserVN?.Vote > 0) parts[2] = $" (Vote: {UserVN.Vote:0.#})";
				return string.Join(" ", parts);
			}
		}
		
		private bool _imageSourceSet;
		private string _imageSource;

		/// <summary>
		/// Get location of cover image in system (not online)
		/// if there is no image or file is not found, returns null.
		/// </summary>
		public string ImageSource => StaticHelpers.GetImageSource(ImageId, ref _imageSourceSet, ref _imageSource);

		public string Series { get; set; }

		public string FlagSource => StaticHelpers.GetFlag(LanguagesObject.Originals);

		private bool? _specialFlag;

		public bool GetAlertFlag(List<int> tagIds, List<int> traitIds)
		{
			if (!DumpFiles.Loaded) return false;
			if (_specialFlag == null)
			{
				if (Suggestion != null) _specialFlag = Suggestion.Score > 0;
				else
				{
					foreach (var tagId in tagIds)
					{
						var tag = DumpFiles.GetTag(tagId);
						if (tag == null) continue;
						var found = tag.InCollection(Tags.Select(t => t.TagId));
						if (found)
						{
							_specialFlag = true;
							return _specialFlag.Value;
						}
					}
					foreach (var traitId in traitIds)
					{
						var trait = DumpFiles.GetTrait(traitId);
						if (trait == null) continue;
						var found = StaticHelpers.LocalDatabase.GetCharactersTraitsForVn(VNID, true).Any(t => trait.AllIDs.Contains(t.TraitId));
						if (found)
						{
							_specialFlag = true;
							return _specialFlag.Value;
						}
					}
					_specialFlag = false;
				}
			}
			return _specialFlag.Value;
		}
		
		public bool HasLanguage(string value)
		{
			return LanguagesObject.All.Contains(value);
		}

		public bool HasOriginalLanguage(string value)
		{
			return LanguagesObject.Originals.Contains(value);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public static Func<int, OwnedStatus> VnIsOwned = null;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			if (propertyName == null) Producer.OnPropertyChanged(null);
		}

		public void SetRelations(string relationsString, RelationsItem[] relationsObject)
		{
			Relations = relationsString;
			_relationsObject = relationsObject;
		}

		public void SetAnime(string animeString, AnimeItem[] animeObject)
		{
			Anime = animeString;
			_animeObject = animeObject;
		}

		public void SetScreens(string screensString, ScreenItem[] screensObject)
		{
			Screens = screensString;
			_screensObject = screensObject;
		}

		#region IDumpItem Implementation

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public void LoadFromStringParts(string[] parts)
		{
			try
			{
				VNID = Convert.ToInt32(GetPart(parts, "id"));
				Title = GetPart(parts, "title");
				KanjiTitle = GetPart(parts, "original");
				Aliases = GetPart(parts, "alias");
				if (!string.IsNullOrWhiteSpace(GetPart(parts, "length"))) LengthTime = (LengthFilterEnum)Convert.ToInt32(GetPart(parts, "length"));
				var imageId = GetPart(parts, "image");
				ImageId = imageId == "\\N" ? null : imageId;
				Description = Convert.ToString(GetPart(parts, "desc"));
				//Popularity = Convert.ToDouble(reader["Popularity"]);
				//Languages = Convert.ToString(reader["Languages"]);
				//DateFullyUpdated = DateTime.UtcNow; ;
				//Series = Convert.ToString(reader["Series"]);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
		#endregion

		#region IDataItem Implementation

		string IDataItem<int>.KeyField => nameof(VNID);

		int IDataItem<int>.Key => VNID;

		public IEnumerable<VnStaff> Staff => StaticHelpers.LocalDatabase.VnStaffs.Where(s => s.VNID == VNID);

		DbCommand IDataItem<int>.UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO ListedVNs" +
				"(VNID,Title,KanjiTitle,ReleaseDateString,ProducerID,Image,ImageNSFW,Description,LengthTime,Popularity," +
				"Rating,VoteCount,Relations,Screens,Anime,Aliases,Languages,Series,ReleaseDate,ReleaseLink,TagScore,TraitScore) VALUES " +
				"(@VNID,@Title,@KanjiTitle,@ReleaseDateString,@ProducerId,@Image,@ImageNSFW,@Description,@LengthTime,@Popularity," +
				"@Rating,@VoteCount,@Relations,@Screens,@Anime,@Aliases,@Languages,@Series,@ReleaseDate,@ReleaseLink,@TagScore,@TraitScore)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@VNID", VNID);
			command.AddParameter("@Title", Title);
			command.AddParameter("@KanjiTitle", KanjiTitle);
			command.AddParameter("@ReleaseDateString", ReleaseDateString);
			command.AddParameter("@ProducerID", ProducerID);
			command.AddParameter("@Image", ImageId);
			command.AddParameter("@ImageNSFW", ImageNSFW);
			command.AddParameter("@Description", Description);
			command.AddParameter("@LengthTime", LengthTime);
			command.AddParameter("@Popularity", Popularity);
			command.AddParameter("@Rating", Rating);
			command.AddParameter("@VoteCount", VoteCount);
			command.AddParameter("@Relations", Relations);
			command.AddParameter("@Screens", Screens);
			command.AddParameter("@Anime", Anime);
			command.AddParameter("@Aliases", Aliases);
			command.AddParameter("@Languages", Languages);
			command.AddParameter("@Series", Series);
			command.AddParameter("@ReleaseDate", ReleaseDate);
			command.AddParameter("@ReleaseLink", ReleaseLink);
			command.AddParameter("@TagScore", Suggestion?.TagScore);
			command.AddParameter("@TraitScore", Suggestion?.TraitScore);
			return command;
		}

		void IDataItem<int>.LoadFromReader(IDataRecord reader)
		{
			try
			{
				VNID = Convert.ToInt32(reader["VNID"]);
				Title = Convert.ToString(reader["Title"]);
				KanjiTitle = Convert.ToString(reader["KanjiTitle"]);
				ReleaseDateString = Convert.ToString(reader["ReleaseDateString"]);
				var producerIdObject = reader["ProducerID"];
				if (!producerIdObject.Equals(DBNull.Value)) ProducerID = Convert.ToInt32(producerIdObject);
				var imageIdObject = reader["Image"];
				if (!imageIdObject.Equals(DBNull.Value)) ImageId = Convert.ToString(imageIdObject);
				ImageNSFW = Convert.ToInt32(reader["ImageNSFW"]) == 1;
				Description = Convert.ToString(reader["Description"]);
				var lengthTimeObject = reader["LengthTime"];
				if (!lengthTimeObject.Equals(DBNull.Value)) LengthTime = (LengthFilterEnum)Convert.ToInt32(lengthTimeObject);
				Popularity = Convert.ToDouble(reader["Popularity"]);
				Rating = Convert.ToDouble(reader["Rating"]);
				VoteCount = Convert.ToInt32(reader["VoteCount"]);
				Relations = Convert.ToString(reader["Relations"]);
				Screens = Convert.ToString(reader["Screens"]);
				Anime = Convert.ToString(reader["Anime"]);
				Aliases = Convert.ToString(reader["Aliases"]);
				Languages = Convert.ToString(reader["Languages"]);
				Series = Convert.ToString(reader["Series"]);
				ReleaseDate = Convert.ToDateTime(reader["ReleaseDate"]);
				ReleaseLink = Convert.ToString(reader["ReleaseLink"]);
				Suggestion = new SuggestionScoreObject(StaticHelpers.GetNullableDouble(reader["TagScore"]), StaticHelpers.GetNullableDouble(reader["TraitScore"]));
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
		#endregion
	}

	public enum OwnedStatus
	{
		NeverOwned = 0,
		PastOwned = 1,
		CurrentlyOwned = 2
	}
}