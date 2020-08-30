using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace Happy_Apps_Core
{
	public class CharacterItem : IDataItem<int>, IDumpItem, ICloneable
	{
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
		[NotNull] public IEnumerable<string> DisplayTraits
		{
			get
			{
				if (ID == 0) return new List<string>();
				var traits = DbTraits.ToList();
				if (traits.Count == 0) return new List<string>();
				var stringList = new List<string> { $"{traits.Count} Traits" };
				stringList.AddRange(traits.Select(trait => DumpFiles.GetTrait(trait.TraitId)?.ToString()));
				return stringList;
			}
		}
		public CharacterVN CharacterVN { get; set; }
		public ListedVN VisualNovel => CharacterVN == null ? null : StaticHelpers.LocalDatabase.VisualNovels[CharacterVN.VNId];
		public string VisualNovelName => VisualNovel?.Title;
		public string VisualNovelReleaseDate => VisualNovel?.ReleaseDateString;
		public DateTime? VisualNovelSortingDate => VisualNovel?.ReleaseDate;
		public ListedProducer Producer => VisualNovel?.Producer;
		public bool HasFullReleaseDate => VisualNovel?.HasFullDate ?? false;
		public IEnumerable<CharacterVN> VisualNovels => StaticHelpers.LocalDatabase.CharacterVNs.Where(cvn => cvn.CharacterId == ID);
		public string ImageSource
		{
			get
			{
				if (_imageSource != null) return _imageSource;
				if (ImageId == null) _imageSource = Path.GetFullPath(StaticHelpers.NoImageFile);
				else
				{
					var filePath = StaticHelpers.GetImageLocation(ImageId);
					_imageSource = File.Exists(filePath) ? filePath : Path.GetFullPath(StaticHelpers.NoImageFile);
				}
				return _imageSource;
			}
		}

		public bool ContainsTraits(IEnumerable<DumpFiles.WrittenTrait> traitFilters)
		{
			//remove all numbers in traits from traitIDs, if nothing is left then it matched all
			int[] traits = DbTraits.Select(x => x.TraitId).ToArray();
			return traitFilters.All(writtenTrait => traits.Any(characterTrait => writtenTrait.AllIDs.Contains(characterTrait)));
		}

		#region IDataItem Implementation
		public string KeyField => "ID";
		public int Key => ID;

		public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
		{
			string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO CharacterItems" +
									 "(ID,Name,Original,Gender,Aliases,Description,Image,TraitScore) VALUES " +
									 "(@ID,@Name,@Original,@Gender,@Aliases,@Description,@Image,@TraitScore)";
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
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
		}

		public static CharacterItem FromReader(IDataRecord reader)
		{
			var character = new CharacterItem();
			character.LoadFromReader(reader);
			return character;
		}
		#endregion

		public static Dictionary<string, int> Headers = new Dictionary<string, int>();

		public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

		public void SetDumpHeaders(string[] parts)
		{
			int colIndex = 0;
			Headers = parts.ToDictionary(c => c, c => colIndex++);
		}

		public void LoadFromStringParts(string[] parts)
		{
			ID = Convert.ToInt32(GetPart(parts, "id"));
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
	}
}