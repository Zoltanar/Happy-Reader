using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
#pragma warning disable 162


namespace Happy_Apps_Core.Database
{
	public partial class VisualNovelDatabase
	{
		#region Set Methods

		public void AddNoteToVN(int vnid, string note, int userID)
		{
			var uvn = UserVisualNovels.Single(x => x.UserId == userID && x.VNID == vnid);
			uvn.ULNote = note;
		}

		public void AddRelationsToVN(ListedVN vn, VNItem.RelationsItem[] relations)
		{
			var relationsObject = relations.Any() ? relations : new VNItem.RelationsItem[] { };
			var relationsString = ListToJsonArray(relationsObject);
			vn.SetRelations(relationsString, relationsObject);
		}

		public void AddAnimeToVN(ListedVN vn, VNItem.AnimeItem[] anime)
		{
			var animeObject = anime.Any() ? anime : new VNItem.AnimeItem[] { };
			var animeString = ListToJsonArray(animeObject);
			vn.SetAnime(animeString, animeObject);
		}

		public void AddScreensToVN(ListedVN vn, VNItem.ScreenItem[] screens)
		{
			var screensObject = screens.Any() ? screens : new VNItem.ScreenItem[] { };
			var screensString = ListToJsonArray(screensObject);
			vn.SetScreens(screensString, screensObject);
		}

		public void InsertFavoriteProducers(List<ListedProducer> addProducerList, int userid)
		{
			Connection.Open();
			try
			{
				foreach (var listedProducer in addProducerList)
				{
					UserProducers.Add(new UserListedProducer { ListedProducer_Id = listedProducer.ID, User_Id = userid }, true);
				}
			}
			finally
			{
				Connection.Close();
			}
		}

		/// <summary>
		/// Insert or Replace Producer into producerlist.
		/// </summary>
		/// <param name="producer">The producer to be inserted</param>
		/// <param name="saveChanges">Commit changes to database if true, if false, then make sure to save yourself after</param>
		public void UpsertProducer(ProducerItem producer, bool saveChanges)
		{
			var dbProducer = Producers[producer.ID];
			var newProducer = dbProducer == null;
			if (newProducer)
			{
				dbProducer = new ListedProducer { ID = producer.ID };
			}
			dbProducer.Name = producer.Name;
			dbProducer.Language = producer.Language;
			Producers.Upsert(dbProducer, saveChanges);
		}

		/// <summary>
		/// Adds or Updates all titles in User-related title list.
		/// </summary>
		/// <param name="userid">ID of User</param>
		/// <param name="urtList">List of URT titles</param>
		public void UpdateURTTitles(int userid, IEnumerable<UrtListItem> urtList)
		{
			Connection.Open();
			try
			{
				foreach (var item in urtList)
				{
					var uvn = UserVisualNovels[(userid, item.ID)];
					switch (item.Action)
					{
						case Command.New:
							uvn = new UserVN { VNID = item.ID, UserId = userid };
							goto case Command.Update;
						case Command.Update:
							uvn.Labels = item.Labels.ToHashSet();
							uvn.ULNote = item.ULNote;
							uvn.Vote = item.Vote;
							uvn.VoteAdded = item.VoteAdded.UnixTimestampToDateTime();
							uvn.Added = item.GetLastModified();
							UserVisualNovels.Upsert(uvn, false);
							break;
						case Command.Delete:
							UserVisualNovels.Remove(uvn, false);
							break;
					}
				}
			}
			finally
			{
				Connection.Close();
			}
		}

		/// <summary>
		/// Returns true if added or false if updated/failed
		/// </summary>
		/// <returns>True if added or false if updated</returns>
		public bool UpsertSingleCharacter(CharacterItem character, bool saveChanges)
		{
			bool result;
			var dbCharacter = Characters[character.ID];
			if (dbCharacter == null)
			{
				dbCharacter = new CharacterItem { ID = character.ID };
				result = true;
			}
			else result = false;
			dbCharacter.Name = character.Name;
			dbCharacter.Original = character.Original;
			dbCharacter.Gender = character.Gender;
			dbCharacter.Aliases = character.Aliases;
			dbCharacter.Description = character.Description;
			dbCharacter.ImageId = character.ImageId;
			if (saveChanges) Connection.Open();
			try
			{
				var traitCh = character.Traits.Select(trait => DbTrait.From(trait, character.ID)).ToList();
				var traitsToRemove = dbCharacter.DbTraits.Except(traitCh, DbTrait.ValueComparer).ToArray();
				foreach (var trait in traitsToRemove) Traits.Remove(trait, false);
				foreach (var trait in traitCh) Traits.Upsert(trait, false);
				Characters.Upsert(dbCharacter, false);
				//replace character's visual novels with new data
				foreach (var characterVn in dbCharacter.VisualNovels) CharacterVNs.Remove(characterVn, false);
				foreach (var characterVn in character.VNs) CharacterVNs.Add(CharacterVN.From(characterVn, character.ID), false);
				//todo API: handle character seiyuu
				/*
				var staffCh = (character.Voiced?.Select(v => CharacterStaff.From(v, character.ID)) ?? Array.Empty<CharacterStaff>()).ToArray();
				var staffToRemove = dbCharacter.DbStaff.Except(staffCh, CharacterStaff.KeyComparer).ToArray();
				foreach (var staff in staffToRemove) CharacterStaffs.Remove(staff, false);
				foreach (var staff in staffCh) CharacterStaffs.Upsert(staff, false);
				*/
				return result;
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
				throw;
			}
			finally
			{
				if (saveChanges) Connection.Close();
			}
		}

		public void UpsertSingleVN((VNItem item, ProducerItem producer, VNLanguages languages) data, bool saveChanges)
		{
			var (item, producer, languages) = data;
			var vn = VisualNovels[item.ID] ?? new ListedVN { VNID = item.ID };
			vn.Title = item.Title;
			vn.KanjiTitle = item.Original;
			vn.ProducerID = producer?.ID;
			vn.SetReleaseDate(item.Released);
			RefreshTags(vn, item, saveChanges);
			vn.Description = item.Description;
			vn.ImageId = item.ImageId;
			vn.ImageNSFW = item.Image_Nsfw;
			vn.LengthTime = (LengthFilterEnum?)item.Length;
			vn.Popularity = item.Popularity;
			vn.Rating = item.Rating;
			vn.VoteCount = item.VoteCount;
			vn.Aliases = item.Aliases;
			vn.Languages = languages?.ToString();
			VisualNovels.Upsert(vn, saveChanges);
		}

		private void RefreshTags(ListedVN vn, VNItem item, bool saveChanges)
		{
			if (saveChanges) Connection.Open();
			var tagsToUpdate = item.Tags.Select(t => DbTag.From(t, vn.VNID)).ToList();
			var tagsToRemove = vn.Tags.Except(tagsToUpdate, DbTag.KeyComparer);
			foreach (var tag in tagsToRemove) Tags.Remove(tag, false);
			foreach (var tag in tagsToUpdate) Tags.Upsert(tag, false);
			if (saveChanges) Connection.Close();
		}

		public void RemoveFavoriteProducer(int producerID, int userid)
		{
			UserProducers.Remove(UserProducers[(producerID, userid)], true);
		}

		public void RemoveVisualNovel(int vnid, bool saveChanges)
		{
			var vn = VisualNovels[vnid];
			if (vn == null) return;
			VisualNovels.Remove(vn, saveChanges);
		}

		public void RemoveCharacter(int cid, bool saveChanges)
		{
			var character = Characters[cid];
			if (character == null) return;
			Characters.Remove(character, saveChanges);
		}

		#endregion

		#region Other

		public static Func<ListedVN, bool> SearchForVN(string searchString)
		{
			var lowerSearchString = searchString.ToLower();
			return vn => vn.Title.ToLower().Contains(lowerSearchString) ||
						 vn.KanjiTitle != null && vn.KanjiTitle.ToLower().Contains(lowerSearchString) ||
						 vn.Aliases != null && vn.Aliases.ToLower().Contains(lowerSearchString);
		}

		public static Func<CharacterItem, bool> SearchForCharacter(string searchString)
		{
			var lowerSearchString = searchString.ToLower();
			return ch => ch.Name.ToLower().Contains(lowerSearchString) ||
									 ch.Original != null && ch.Original.ToLower().Contains(lowerSearchString) ||
									 ch.Aliases != null && ch.Aliases.ToLower().Contains(lowerSearchString);
		}

		/// <summary>
		///     Type of VN status to be changed.
		/// </summary>
		public enum ChangeType
		{
			Labels,
			Vote
		}

		/// <summary>
		/// Object for updating user-related list.
		/// </summary>
		public class UrtListItem
		{
#pragma warning disable 1591
			public int ID { get; }
			public List<UserVN.LabelKind> Labels { get; set; }
			public int? ULAdded { get; private set; }
			public string ULNote { get; private set; }
			public int? WLAdded { get; private set; }
			public int? Vote { get; private set; }
			public int? VoteAdded { get; private set; }
			public Command Action { get; private set; }
#pragma warning restore 1591

			/// <summary>
			/// Create URT item from previously fetched data. (For Method Group)
			/// </summary>
			public static UrtListItem FromVN(ListedVN vn)
			{
				return new UrtListItem(vn);
			}
			/// <summary>
			/// Create URT item from previously fetched data. (For Method Group)
			/// </summary>
			public static UrtListItem FromVN(UserVN vn)
			{
				return new UrtListItem(vn);
			}

			/// <summary>
			/// Create URT item from previously fetched data.
			/// </summary>
			public UrtListItem(ListedVN vn)
			{
				ID = vn.VNID;
				Action = Command.Delete;
			}
			/// <summary>
			/// Create URT item from previously fetched data.
			/// </summary>
			public UrtListItem(UserVN vn)
			{
				ID = vn.VNID;
				Action = Command.Delete;
			}

			public UrtListItem(int id)
			{
				ID = id;
				Action = Command.New;
			}

			/// <summary>
			/// Create new URT item from user list data.
			/// </summary>
			public UrtListItem(UserListItem item) : this(item.VN)
			{
				throw new NotImplementedException("Change to use ulist");
				/*ULStatus = (UserlistStatus)item.Status;
				ULAdded = item.Added;
				ULNote = item.Notes;*/
			}

			/// <summary>
			/// Create new URT item from wish list data.
			/// </summary>
			public UrtListItem(WishListItem item) : this(item.VN)
			{
				throw new NotImplementedException("Change to use ulist");/*
				WLStatus = (WishlistStatus)item.Priority;
				WLAdded = item.Added;*/
			}

			/// <summary>
			/// Create new URT item from vote list data.
			/// </summary>
			public UrtListItem(VoteListItem item) : this(item.VN)
			{
				Vote = item.Vote;
				VoteAdded = item.Added;
			}

			/// <summary>
			/// Update URT item with user list data.
			/// </summary>
			public void Update(UserListItem item)
			{
				throw new NotImplementedException("Change to use ulist");
				/*
				ULStatus = (UserlistStatus)item.Status;
				ULAdded = item.Added;
				ULNote = item.Notes;
				if (Action != Command.New) Action = Command.Update;
				else { }*/
			}


			/// <summary>
			/// Update URT item with wish list data.
			/// </summary>
			public void Update(WishListItem item)
			{
				throw new NotImplementedException("Change to use ulist");/*
				WLStatus = (WishlistStatus)item.Priority;
				WLAdded = item.Added;
				if (Action != Command.New) Action = Command.Update;*/
			}

			/// <summary>
			/// Update URT item with vote list data.
			/// </summary>
			public void Update(VoteListItem item)
			{
				Vote = item.Vote;
				VoteAdded = item.Added;
				if (Action != Command.New) Action = Command.Update;
			}

			/// <summary>Returns a string that represents the current object.</summary>
			/// <returns>A string that represents the current object.</returns>
			/// <filterpriority>2</filterpriority>
			public override string ToString() => $"{Action} - {ID}";

			public DateTime? GetLastModified()
			{
				return new[] { WLAdded, ULAdded }.Where(v => v.HasValue).OrderByDescending(v => v).FirstOrDefault().UnixTimestampToDateTime();
			}
		}

		/// <summary>
		///     Command to change VN status.
		/// </summary>
		public enum Command
		{
			/// <summary>
			/// Add to URT list
			/// </summary>
			New,
			/// <summary>
			/// Update item in URT list
			/// </summary>
			Update,
			/// <summary>
			/// Delete item from URT list
			/// </summary>
			Delete
		}

		/// <summary>
		/// Convert list of objects to JSON array string.
		/// </summary>
		/// <param name="objects">List of objects</param>
		/// <returns>JSON array string</returns>
		private static string ListToJsonArray(ICollection<object> objects)
		{
			return JsonConvert.SerializeObject(objects);
		}

		#endregion

		public void DeleteForDump()
		{
			Connection.Open();
			try
			{
				var trans = Connection.BeginTransaction();
				DeleteTable("CharacterItems", trans);
				DeleteTable("CharacterVNs", trans);
				DeleteTable("DbTags", trans);
				DeleteTable("DbTraits", trans);
				DeleteTable("ListedProducers", trans);
				DeleteTable("ListedVNs", trans);
				DeleteTable("UserVNs", trans);
				DeleteTable("StaffItems", trans);
				DeleteTable("StaffAliass", trans);
				DeleteTable("VnStaffs", trans);
				DeleteTable("VnSeiyuus", trans);
				trans.Commit();
			}
			finally
			{
				Connection.Close();
			}
		}

		private void DeleteTable(string tableName, SQLiteTransaction trans)
		{
			var command = Connection.CreateCommand();
			command.CommandText = $@"DELETE FROM {tableName}";
			command.Transaction = trans;
			command.ExecuteNonQuery();
			command.Dispose();
		}

	}

}