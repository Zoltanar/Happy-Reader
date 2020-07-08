using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Windows.Media;
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
	/// <summary>
	/// From get character commands
	/// </summary>
	public partial class CharacterItem : IDataItem<int>, IDumpItem, ICloneable
	{
		public int ID { get; set; }

		#region Basic Flag
		public string Name { get; set; }
		public string Original { get; set; }
		public string Gender { get; set; }
		public string BloodT { get; set; }
		/// <summary>
		/// Converted to BirthDate when fetched from vndb
		/// </summary>
		public int?[] Birthday { get; [UsedImplicitly] set; }
		public DateTime? BirthDate { get; set; }
		#endregion

		#region Details Flag
		public string Aliases { get; set; }
		public string Description { get; set; }
		public string ImageId { get; set; }
		#endregion

		public DateTime? DateUpdated { get; set; }

		/// <summary>
		/// Only used in json convert from vndb
		/// </summary>
		[NotMapped]
		public TraitItem[] Traits { get; [UsedImplicitly] set; }

		/// <summary>
		/// Only used in json convert from vndb
		/// </summary>
		[NotMapped]
		public VNItem[] VNs { get; [UsedImplicitly] set; }

		[NotMapped]
		public StaffItem[] Voiced { get; [UsedImplicitly] set; }

		public IEnumerable<DbTrait> DbTraits => StaticHelpers.LocalDatabase.Traits.Where(t => t.CharacterItem_Id == ID);

		public IEnumerable<CharacterStaff> DbStaff =>
			StaticHelpers.LocalDatabase.CharacterStaffs.Where(cs => cs.CharacterItem_Id == ID);

		[NotMapped]
		public Brush BackBrush
		{
			get
			{
				return CharacterVN?.Role switch
				{
					"main" => Brushes.Gold,
					"primary" => Brushes.Orchid,
					"side" => Brushes.GreenYellow,
					"appears" => Brushes.LightBlue,
					null => Brushes.Gray,
					_ => Brushes.White
				};
			}
		}

		public string GenderSymbol
		{
			get
			{
				switch (Gender)
				{
					case "f": return "♀";
					case "m": return "♂";
					case "b": return "⚤";
					default: return "";

				}
			}
		}

		[NotMapped, NotNull]
		public IEnumerable<string> DisplayTraits
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

		[NotMapped]
		public CharacterVN CharacterVN { get; set; }
		public ListedVN VisualNovel => CharacterVN == null ? null : StaticHelpers.LocalDatabase.VisualNovels[CharacterVN.VNId];
		public string VisualNovelName => VisualNovel?.Title;
		public string VisualNovelReleaseDate => VisualNovel?.ReleaseDateString;
		public DateTime? VisualNovelSortingDate => VisualNovel?.ReleaseDate;
		public ListedProducer Producer => VisualNovel?.Producer;
		public bool HasFullReleaseDate => VisualNovel?.HasFullDate ?? false;

		public IEnumerable<CharacterVN> VisualNovels => StaticHelpers.LocalDatabase.CharacterVNs.Where(cvn => cvn.CharacterId == ID);

		private object _imageSource;

		public object ImageSource
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

		public bool HasProducer => Producer != null;

		public bool HasVisualNovel => CharacterVN != null;

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
									 "(ID,Name,Original,Gender,BloodT,BirthDate,DateUpdated,Aliases,Description,Image) VALUES " +
									 "(@ID,@Name,@Original,@Gender,@BloodT,@BirthDate,@DateUpdated,@Aliases,@Description,@Image)";
			var command = connection.CreateCommand();
			command.CommandText = sql;
			command.AddParameter("@ID", ID);
			command.AddParameter("@Name", Name);
			command.AddParameter("@Original", Original);
			command.AddParameter("@Gender", Gender);
			command.AddParameter("@BloodT", BloodT);
			command.AddParameter("@BirthDate", BirthDate);
			command.AddParameter("@DateUpdated", DateUpdated);
			command.AddParameter("@Aliases", Aliases);
			command.AddParameter("@Description", Description);
			command.AddParameter("@Image", ImageId);
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
				BloodT = Convert.ToString(reader["BloodT"]);
				BirthDate = StaticHelpers.GetNullableDate(reader["BirthDate"]);
				DateUpdated = StaticHelpers.GetNullableDate(reader["DateUpdated"]);
				Aliases = Convert.ToString(reader["Aliases"]);
				Description = Convert.ToString(reader["Description"]);
				var imageIdObject = reader["Image"];
				if (!imageIdObject.Equals(DBNull.Value)) ImageId = Convert.ToString(imageIdObject);
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
			clone.Birthday = Birthday;
			clone.Traits = Traits;
			clone.Voiced = Voiced;
			return clone;
		}
	}
}