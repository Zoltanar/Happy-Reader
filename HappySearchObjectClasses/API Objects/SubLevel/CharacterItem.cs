using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;

namespace Happy_Apps_Core
{
	public class CharacterItem : IDataItem<int>, IDumpItem, ICloneable
	{
		private bool _imageSourceSet;
		private string _imageSource;
		private bool? _alertFlag;

		public int ID { get; set; }
		public string Name { get; set; }
		public string Original { get; set; }
		public string Gender { get; set; }
		public string Aliases { get; set; }
		public string Description { get; set; }
		public string ImageId { get; set; }
		public double? TraitScore { get; set; }
		public bool NewSinceUpdate { get; set; }

		public IEnumerable<DbTrait> DbTraits => StaticHelpers.LocalDatabase.Traits[ID];
		public IEnumerable<VnSeiyuu> Seiyuus => StaticHelpers.LocalDatabase.VnSeiyuus.Where(s => s.CharacterID == ID);
		public VnSeiyuu Seiyuu => Seiyuus.FirstOrDefault();
		public string GenderSymbol
		{
			get
			{
				return Gender switch
				{
					"f" => "♀",
					"m" => "♂",
					"b" => "⚤",
					_ => ""
				};
			}
		}
		public CharacterVN CharacterVN { get; set; }
		public ListedVN VisualNovel => CharacterVN == null ? null : StaticHelpers.LocalDatabase.VisualNovels[CharacterVN.VNId];
		public ListedProducer Producer => VisualNovel?.Producer;
		public IEnumerable<CharacterVN> VisualNovels => StaticHelpers.LocalDatabase.CharacterVNs.Where(cvn => cvn.CharacterId == ID);
		public string ImageSource => StaticHelpers.GetImageSource(ImageId, ref _imageSourceSet, ref _imageSource);

		#region IDataItem Implementation
		public string KeyField => "ID";
		public int Key => ID;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO CharacterItems" +
									 "(ID,Name,Original,Gender,Aliases,Description,Image,TraitScore, NewSinceUpdate) VALUES " +
									 "(@ID,@Name,@Original,@Gender,@Aliases,@Description,@Image,@TraitScore, @NewSinceUpdate)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@ID", ID);
			command.AddParameter("@Name", Name);
			command.AddParameter("@Original", Original);
			command.AddParameter("@Gender", Gender);
			command.AddParameter("@Aliases", Aliases);
			command.AddParameter("@Description", Description);
			command.AddParameter("@Image", ImageId);
			command.AddParameter("@TraitScore", TraitScore);
			command.AddParameter("@NewSinceUpdate", NewSinceUpdate);
			return command;
		}

		public void LoadFromReader(IDataRecord reader)
		{
			try
			{
				ID = Convert.ToInt32(reader["ID"]);
				Name = Convert.ToString(reader["Name"]);
				Original = Convert.ToString(reader["Original"]);
				Gender = Convert.ToString(reader["Gender"]);
				Aliases = Convert.ToString(reader["Aliases"]);
				Description = Convert.ToString(reader["Description"]);
				var imageIdObject = reader["Image"];
				if (!imageIdObject.Equals(DBNull.Value)) ImageId = Convert.ToString(imageIdObject);
				TraitScore = StaticHelpers.GetNullableDouble(reader["TraitScore"]);
				NewSinceUpdate = Convert.ToInt32(reader["NewSinceUpdate"]) == 1;
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}
		#endregion

		public static Dictionary<string, int> Headers = new();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, _ => colIndex++);
		}

		public void LoadFromStringParts(string[] parts)
		{
			ID = Convert.ToInt32(GetPart(parts, "id").Substring(1));
			Name = GetPart(parts, "name");
			Original = GetPart(parts, "original");
			Aliases = GetPart(parts, "alias");
			var imageId = GetPart(parts, "image");
			ImageId = imageId == "\\N" ? null : imageId;
			Description = GetPart(parts, "desc");
			Gender = GetPart(parts, "gender");
		}

		public CharacterItem Clone() => (CharacterItem)((ICloneable)this).Clone();

		object ICloneable.Clone()
		{
			var clone = (CharacterItem)this.MemberwiseClone();
			return clone;
		}

		public bool GetAlertFlag(IEnumerable<int> alertTraitIDs)
		{
			if (_alertFlag.HasValue) return _alertFlag.Value;
			if (TraitScore.HasValue) _alertFlag = TraitScore > 0;
			else
			{
				var alert = false;
				foreach (var traitId in alertTraitIDs)
				{
					var trait = DumpFiles.GetTrait(traitId);
					if (trait == null) continue;
					var found = DbTraits.Any(t => trait.AllIDs.Contains(t.TraitId));
					if (!found) continue;
					alert = true;
					break;
				}
				_alertFlag = alert;
			}
			return _alertFlag.Value;
		}

		public IEnumerable<IGrouping<string, DumpFiles.WrittenTrait>> GetGroupedTraits()
		{
			var groups = DbTraits
				.Where(t =>
				{
					var trait = DumpFiles.GetTrait(t.TraitId);
					if (trait == null) return false;
					return DumpFiles.RootTraitIds.Contains(trait.TopmostParent);
				})
				.Select(trait => DumpFiles.GetTrait(trait.TraitId))
				.GroupBy(x => x?.TopmostParentName ?? "Not Found");
			return groups;
		}
	}
}