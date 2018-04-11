using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Happy_Apps_Core.Database;
using Happy_Apps_Core.Properties;
using JetBrains.Annotations;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
	partial class VndbConnection
	{
		public enum MessageSeverity { Normal, Warning, Error }
		private enum QueryResult { Fail, Success, Throttled }

		public ApiQuery ActiveQuery { get; private set; }


		/// <summary>
		/// string is text to be passed, bool is true if query, false if response
		/// </summary>
		private readonly Action<string, bool> _advancedAction;
		private readonly Action _refreshListAction;
		private readonly Action<APIStatus> _changeStatusAction;
		[NotNull]
		public readonly Action<string, MessageSeverity> TextAction;

		public VndbConnection([NotNull]Action<string, MessageSeverity> textAction, Action<string, bool> advancedModeAction, Action refreshListAction, Action<APIStatus> changeStatusAction = null)
		{
			TextAction = textAction;
			_advancedAction = advancedModeAction;
			_refreshListAction = refreshListAction;
			_changeStatusAction = changeStatusAction;
		}

		/// <summary>
		/// Check if API Connection is ready, change status accordingly and write error if it isnt ready.
		/// </summary>
		/// <param name="featureName">Name of feature calling the query</param>
		/// <param name="refreshList">Refresh OLV on throttled connection</param>
		/// <param name="additionalMessage">Print Added/Skipped message on throttled connection</param>
		/// <param name="ignoreDateLimit">Ignore 10 year limit (if applicable)</param>
		/// <returns>If connection was ready</returns>
		internal bool StartQuery(string featureName, bool refreshList, bool additionalMessage, bool ignoreDateLimit)
		{
			if (ActiveQuery != null && !ActiveQuery.Completed)
			{
				TextAction($"Wait until {ActiveQuery.ActionName} is done.", MessageSeverity.Error);
				return false;
			}
			ActiveQuery = new ApiQuery(featureName, refreshList, additionalMessage, ignoreDateLimit);
			TextAction($"Running {featureName}...", MessageSeverity.Normal);
			return true;
		}

		internal void EndQuery()
		{
			ActiveQuery.Completed = true;
			TextAction(ActiveQuery.CompletedMessage, ActiveQuery.CompletedMessageSeverity);
			_changeStatusAction?.Invoke(Status);
		}

		#region Static

		private static string FormatQuery(string format, IEnumerable<int> idArray, int pageNo = 1)
		{
			var arrayString = '[' + string.Join(",", idArray) + ']';
			return format.Replace("{0}", arrayString) + $" {{{MaxResultsString}, \"page\":{pageNo}}}";
		}

		private static string FormatQuery(string format, string stringVariable, int pageNo = 1)
		{
			return format.Replace("{0}", stringVariable) + $" {{{MaxResultsString}, \"page\":{pageNo}}}";
		}

		private static void RemoveDeletedVNs(ResultsRoot<VNItem> root, ICollection<int> currentArray)
		{
			if (root.Num >= currentArray.Count) return;
			//some vns were deleted, find which ones and remove them
			IEnumerable<int> deletedVNs = currentArray.Where(currentvn => root.Items.All(receivedvn => receivedvn.ID != currentvn)).ToArray();
			foreach (var deletedVN in deletedVNs)
			{
				LocalDatabase.RemoveVisualNovel(deletedVN, false);
				currentArray.Remove(deletedVN);
			}
			LocalDatabase.SaveChanges();
		}

		private static void RemoveDeletedCharacters(ResultsRoot<CharacterItem> root, ICollection<int> currentArray)
		{
			if (root.Num >= currentArray.Count) return;
			//some vns were deleted, find which ones and remove them
			IEnumerable<int> deletedChars = currentArray.Where(presentCharId => root.Items.All(receivedCharacter => receivedCharacter.ID != presentCharId)).ToArray();
			foreach (var deletedCharacter in deletedChars)
			{
				LocalDatabase.RemoveCharacter(deletedCharacter, false);
				currentArray.Remove(deletedCharacter);
			}
			LocalDatabase.SaveChanges();
		}

		#endregion

		#region Private

		/// <summary>
		/// Get data about multiple visual novels.
		/// Creates its own SQLite Transactions.
		/// </summary>
		/// <param name="vnIDs">List of visual novel IDs</param>
		/// <param name="updateAll">If false, will skip VNs already fetched</param>
		private async Task GetMultipleVN(HashSet<int> vnIDs, bool updateAll)
		{
			if (!updateAll)
			{
				int[] allVNIds = LocalDatabase.LocalVisualNovels.Select(x => x.VNID).ToArray();
				int preCount = vnIDs.Count;
				vnIDs.ExceptWith(allVNIds);
				ActiveQuery.AddTitlesSkipped((uint)(vnIDs.Count - preCount));
			}
			vnIDs.Remove(0);
			if (!vnIDs.Any()) return;
			const string queryFormat = "get vn basic,details,tags,stats (id = {0})";
			int done = 0;
			do
			{
				var currentArray = new HashSet<int>(vnIDs.Skip(done).Take(APIMaxResults));
				var queryResult = await TryQuery(FormatQuery(queryFormat, currentArray), Resources.gmvn_query_error);
				if (!queryResult) return;
				var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				RemoveDeletedVNs(vnRoot, currentArray);
				if (!currentArray.Any()) return;
				var vnsToBeUpserted = new List<(VNItem VN, ProducerItem Producer, VNLanguages Languages)>(); //this is not a dictionary because vns should be distinct
				var producersToBeUpserted = new Dictionary<int, ProducerItem>(); //this is a dictionary because we dont want to fetch/upsert the same producer twice
				await HandleVNItems(vnRoot.Items, producersToBeUpserted, vnsToBeUpserted);
				vnsToBeUpserted.ForEach(vn => LocalDatabase.UpsertSingleVN(vn, true, false));
				try
				{
					LocalDatabase.SaveChanges();
				}
				catch (Exception ex)
				{
					LogToFile(ex);
					throw;
				}
				foreach (var producer in producersToBeUpserted.Values) LocalDatabase.UpsertProducer(producer, true, false);
				try
				{
					LocalDatabase.SaveChanges();
				}
				catch (Exception ex)
				{
					LogToFile(ex);
					throw;
				}
				await GetCharacters(currentArray, false);
				done += APIMaxResults;
			} while (done < vnIDs.Count);

			async Task HandleVNItems(List<VNItem> itemList, Dictionary<int, ProducerItem> upsertProducers, List<(VNItem VN, ProducerItem Producer, VNLanguages Languages)> upsertTitles)
			{
				foreach (var vnItem in itemList)
				{
					vnItem.SaveCover();
					var releases = await GetReleases(vnItem.ID, Resources.svn_query_error);
					var mainRelease = releases.FirstOrDefault(item => item.Producers.Exists(x => x.Developer));
					var relProducer = mainRelease?.Producers.FirstOrDefault(p => p.Developer);
					var languages = new VNLanguages(vnItem.Orig_Lang, vnItem.Languages);
					if (relProducer != null && !upsertProducers.ContainsKey(relProducer.ID))
					{
						var gpResult = await GetProducer(relProducer.ID, Resources.gmvn_query_error, updateAll);
						if (!gpResult.Item1)
						{
							_changeStatusAction?.Invoke(Status);
							return;
						}
						if (gpResult.Item2 != null) upsertProducers[relProducer.ID] = gpResult.Item2;
					}
					ActiveQuery.AddTitlesAdded();
					upsertTitles.Add((vnItem, relProducer, languages));
				}
			}
		}

		/// <summary>
		/// Get character data about multiple visual novels.
		/// </summary>
		/// <param name="ids">List of Ids to fetch by</param>
		/// <param name="isCharacterList">true if list of ids is characters ids, false if its vnids</param>
		private async Task GetCharacters(HashSet<int> ids, bool isCharacterList)
		{
			if (!ids.Any()) return;
			var currentArray = ids.Take(APIMaxResults).ToList();
			string queryFormat = $"get character basic,details,traits,vns,voiced ({(isCharacterList ? "id" : "vn")} = {{0}})";
			var queryResult = await TryQuery(FormatQuery(queryFormat, currentArray), $"{nameof(GetCharacters)} Query Error");
			if (!queryResult) return;
			ResultsRoot<CharacterItem> charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
			RemoveDeletedCharacters(charRoot, currentArray);
			foreach (var character in charRoot.Items)
			{
				if (LocalDatabase.UpsertSingleCharacter(character, false)) ActiveQuery.AddCharactersAdded();
				else ActiveQuery.AddCharactersUpdated();
			}
			LocalDatabase.SaveChanges();
			bool moreResults = charRoot.More;
			int pageNo = 1;
			while (moreResults)
			{
				if (!await HandleMoreResults()) return;
			}
			int done = APIMaxResults;
			while (done < ids.Count)
			{
				currentArray = ids.Skip(done).Take(APIMaxResults).ToList();
				queryResult = await TryQuery(FormatQuery(queryFormat, currentArray), $"{nameof(GetCharacters)} Query Error");
				if (!queryResult) return;
				charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
				RemoveDeletedCharacters(charRoot, currentArray);
				foreach (var character in charRoot.Items)
				{
					if (LocalDatabase.UpsertSingleCharacter(character, false)) ActiveQuery.AddCharactersAdded();
					else ActiveQuery.AddCharactersUpdated();
				}
				try
				{
					LocalDatabase.SaveChanges();
				}
				catch (Exception ex)
				{
					LogToFile(ex);
					throw;
				}
				moreResults = charRoot.More;
				pageNo = 1;
				while (moreResults)
				{
					if (!await HandleMoreResults()) return;
				}
				done += APIMaxResults;
			}

			async Task<bool> HandleMoreResults()
			{
				// ReSharper disable AccessToModifiedClosure
				pageNo++;
				queryResult = await TryQuery(FormatQuery(queryFormat, currentArray, pageNo), $"{nameof(GetCharacters)} Query Error");
				if (!queryResult) return false;
				charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
				foreach (var character in charRoot.Items)
				{
					if (LocalDatabase.UpsertSingleCharacter(character, false)) ActiveQuery.AddCharactersAdded();
					else ActiveQuery.AddCharactersUpdated();
				}
				try
				{
					LocalDatabase.SaveChanges();
				}
				catch (Exception ex)
				{
					LogToFile(ex);
					throw;
				}
				moreResults = charRoot.More;
				// ReSharper restore AccessToModifiedClosure
				return true;
			}
		}

		/// <summary>
		/// Get Releases for VN.
		/// </summary>
		/// <param name="vnid">ID of VN</param>
		/// <param name="errorMessage">Message to be printed in case of error</param>
		private async Task<List<ReleaseItem>> GetReleases(int vnid, string errorMessage)
		{
			string developerQuery = $"get release basic,producers (vn =\"{vnid}\") {{{MaxResultsString}}}";
			if (!await TryQuery(developerQuery, errorMessage)) return null;
			var relInfo = JsonConvert.DeserializeObject<ResultsRoot<ReleaseItem>>(LastResponse.JsonPayload);
			List<ReleaseItem> releaseItems = relInfo.Items.Where(rel => !rel.Type.Equals("trial")).ToList();
			if (!releaseItems.Any()) releaseItems = relInfo.Items;
			releaseItems.Sort((x, y) => DateTime.Compare(StringToDate(x.Released), StringToDate(y.Released)));
			return releaseItems;
		}

		/// <summary>
		/// Get Producer from VNDB using Producer ID.
		/// Add producer to database afterwards.
		/// </summary>
		/// <param name="producerID">ID of Producer</param>
		/// <param name="errorMessage">Message to be printed in case of error</param>
		/// <param name="update">Should producer data be fetched even if it is already present in local db?</param>
		/// <returns>Tuple of bool (indicating successful api connection) and ListedProducer (null if none found or already added)</returns>
		private async Task<(bool, ProducerItem)> GetProducer(int producerID, string errorMessage, bool update)
		{
			if (!update && (producerID == -1 || LocalDatabase.LocalProducers.Any(p => p.ID == producerID))) return (true, null);
			string producerQuery = $"get producer basic (id={producerID})";
			if (!await TryQuery(producerQuery, errorMessage)) return (false, null);
			var root = JsonConvert.DeserializeObject<ResultsRoot<ProducerItem>>(LastResponse.JsonPayload);
			List<ProducerItem> producers = root.Items;
			if (!producers.Any()) return (true, null);
			var producer = producers.First();
			return (true, producer);
		}

		private async Task GetRemainingTitles()
		{
			var userTitles = LocalDatabase.LocalUserVisualNovels.Where(x => x.UserId == CSettings.UserID).Select(x => x.VNID);
			var fetchedTitles = LocalDatabase.LocalVisualNovels.Select(x => x.VNID);
			var unfetchedTitles = new HashSet<int>(userTitles.Except(fetchedTitles));
			if (!unfetchedTitles.Any()) return;
			await GetMultipleVN(unfetchedTitles, false);
		}

		/// <summary>
		///     Get user's userlist from VNDB, add titles that aren't in local db already.
		/// </summary>
		/// <param name="urtList">list of title IDs (avoids duplicate fetching)</param>
		/// <returns>list of title IDs (avoids duplicate fetching)</returns>
		private async Task GetUserList(List<VisualNovelDatabase.UrtListItem> urtList)
		{
			LogToFile("Starting GetUserList");
			string userListQuery = $"get vnlist basic (uid = {CSettings.UserID} ) {{\"results\":100}}";
			//1 - fetch from VNDB using API
			var result = await TryQuery(userListQuery, Resources.gul_query_error);
			if (!result) return;
			var ulRoot = JsonConvert.DeserializeObject<ResultsRoot<UserListItem>>(LastResponse.JsonPayload);
			if (ulRoot.Num == 0) return;
			List<UserListItem> ulList = ulRoot.Items; //make list of vns in list
			var pageNo = 1;
			var moreResults = ulRoot.More;
			while (moreResults)
			{
				pageNo++;
				string userListQuery2 = $"get vnlist basic (uid = {CSettings.UserID} ) {{\"results\":100, \"page\":{pageNo}}}";
				var moreResult = await TryQuery(userListQuery2, Resources.gul_query_error);
				if (!moreResult) return;
				var ulMoreRoot = JsonConvert.DeserializeObject<ResultsRoot<UserListItem>>(LastResponse.JsonPayload);
				ulList.AddRange(ulMoreRoot.Items);
				moreResults = ulMoreRoot.More;
			}
			foreach (var item in ulList)
			{
#if DEBUG
				if (item.VN == VNIDToDebug) { }
#endif
				var itemInlist = urtList.FirstOrDefault(vn => vn.ID == item.VN);
				//add if it doesn't exist
				if (itemInlist == null) urtList.Add(new VisualNovelDatabase.UrtListItem(item));
				//update if it already exists
				else itemInlist.Update(item);
			}
		}

		private async Task GetWishList(List<VisualNovelDatabase.UrtListItem> urtList)
		{
			LogToFile("Starting GetWishList");
			string wishListQuery = $"get wishlist basic (uid = {CSettings.UserID} ) {{\"results\":100}}";
			var result = await TryQuery(wishListQuery, Resources.gwl_query_error);
			if (!result) return;
			var wlRoot = JsonConvert.DeserializeObject<ResultsRoot<WishListItem>>(LastResponse.JsonPayload);
			if (wlRoot.Num == 0) return;
			List<WishListItem> wlList = wlRoot.Items; //make list of vn in list
			var pageNo = 1;
			var moreResults = wlRoot.More;
			while (moreResults)
			{
				pageNo++;
				string wishListQuery2 = $"get wishlist basic (uid = {CSettings.UserID} ) {{\"results\":100, \"page\":{pageNo}}}";
				var moreResult = await TryQuery(wishListQuery2, Resources.gwl_query_error);
				if (!moreResult) return;
				var wlMoreRoot = JsonConvert.DeserializeObject<ResultsRoot<WishListItem>>(LastResponse.JsonPayload);
				wlList.AddRange(wlMoreRoot.Items);
				moreResults = wlMoreRoot.More;
			}
			foreach (var item in wlList)
			{
#if DEBUG
				if (item.VN == VNIDToDebug) { }
#endif
				var itemInlist = urtList.FirstOrDefault(vn => vn.ID == item.VN);
				//add if it doesn't exist
				if (itemInlist == null) urtList.Add(new VisualNovelDatabase.UrtListItem(item));
				//update if it already exists
				else itemInlist.Update(item);
			}
		}

		private async Task GetVoteList(List<VisualNovelDatabase.UrtListItem> urtList)
		{
			LogToFile("Starting GetVoteList");
			string voteListQuery = $"get votelist basic (uid = {CSettings.UserID} ) {{\"results\":100}}";
			var result = await TryQuery(voteListQuery, Resources.gvl_query_error);
			if (!result) return;
			var vlRoot = JsonConvert.DeserializeObject<ResultsRoot<VoteListItem>>(LastResponse.JsonPayload);
			if (vlRoot.Num == 0) return;
			List<VoteListItem> vlList = vlRoot.Items; //make list of vn in list
			var pageNo = 1;
			var moreResults = vlRoot.More;
			while (moreResults)
			{
				pageNo++;
				string voteListQuery2 = $"get votelist basic (uid = {CSettings.UserID} ) {{\"results\":100, \"page\":{pageNo}}}";
				var moreResult = await TryQuery(voteListQuery2, Resources.gvl_query_error);
				if (!moreResult) return;
				var vlMoreRoot = JsonConvert.DeserializeObject<ResultsRoot<VoteListItem>>(LastResponse.JsonPayload);
				vlList.AddRange(vlMoreRoot.Items);
				moreResults = vlMoreRoot.More;
			}
			foreach (var item in vlList)
			{
#if DEBUG
				if (item.VN == VNIDToDebug) { }
#endif
				var itemInlist = urtList.FirstOrDefault(vn => vn.ID == item.VN);
				//add if it doesn't exist
				if (itemInlist == null) urtList.Add(new VisualNovelDatabase.UrtListItem(item));
				//update if it already exists
				else itemInlist.Update(item);
			}
		}

		#endregion

		#region Public Functions

		/// <summary>
		/// Update tags, traits and stats of titles.
		/// </summary>
		/// <param name="vnIDs">List of IDs of titles to be updated.</param>
		[ConnectionFunctionAspect, ConnectionInterceptAspect(true, true, true)]
		public async Task UpdateTagsTraitsStats(HashSet<int> vnIDs)
		{
			if (!vnIDs.Any()) return;
			var currentArray = new HashSet<int>(vnIDs.Take(APIMaxResults));
			const string queryFormat = "get vn tags,stats (id = {0})";
			var queryResult = await TryQuery(FormatQuery(queryFormat, currentArray), Resources.gmvn_query_error);
			if (!queryResult) return;
			var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
			RemoveDeletedVNs(vnRoot, currentArray);
			if (!currentArray.Any()) return;
			foreach (var vnItem in vnRoot.Items)
			{
				LocalDatabase.UpdateVNTagsStats(vnItem, false);
				ActiveQuery.AddTitlesAdded();
			}
			LocalDatabase.SaveChanges();
			await GetCharacters(currentArray, false);
			int done = APIMaxResults;
			while (done < vnIDs.Count)
			{
				currentArray = new HashSet<int>(vnIDs.Skip(done).Take(APIMaxResults));
				queryResult = await TryQuery(FormatQuery(queryFormat, currentArray), Resources.gmvn_query_error);
				if (!queryResult) return;
				vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				RemoveDeletedVNs(vnRoot, currentArray);
				if (!currentArray.Any()) return;
				foreach (var vnItem in vnRoot.Items)
				{
					LocalDatabase.UpdateVNTagsStats(vnItem, false);
					ActiveQuery.AddTitlesAdded();
				}
				LocalDatabase.SaveChanges();
				await GetCharacters(currentArray, false);
				done += APIMaxResults;
			}
			ActiveQuery.CompletedMessage = $"Updated tags, traits and stats for {ActiveQuery.TitlesAdded} titles.";
		}

		/// <summary>
		/// Change userlist status, wishlist priority or user vote.
		/// </summary>
		/// <param name="vn">VN which will be changed</param>
		/// <param name="type">What is being changed</param>
		/// <param name="statusInt">The new value</param>
		/// <param name="newVoteValue">New vote value</param>
		/// <returns>Returns whether it as successful.</returns>
		[ConnectionFunctionAspect, ConnectionInterceptAspect(false, false, true)]
		public async Task<bool> ChangeVNStatus(ListedVN vn, VisualNovelDatabase.ChangeType type, int statusInt, double newVoteValue = -1)
		{
			bool remove = statusInt == -1;
			int? statusDate = null;
			if (statusInt != -1) statusDate = (int)DateTimeToUnixTimestamp(DateTime.UtcNow);
			string queryString;
			_changeStatusAction?.Invoke(APIStatus.Busy);
			switch (type)
			{
				case VisualNovelDatabase.ChangeType.UL:
					queryString = statusInt == -1 ? $"set vnlist {vn.VNID}" : $"set vnlist {vn.VNID} {{\"status\":{statusInt}}}";
					var result = await TryQuery(queryString, Resources.cvns_query_error);
					if (!result) return false;
					vn.UserVN.ULStatus = remove ? null : (UserlistStatus?)statusInt;
					vn.UserVN.ULAdded = statusDate;
					break;
				case VisualNovelDatabase.ChangeType.WL:
					queryString = statusInt == -1
						? $"set wishlist {vn.VNID}"
						: $"set wishlist {vn.VNID} {{\"priority\":{statusInt}}}";
					result = await TryQuery(queryString, Resources.cvns_query_error);
					if (!result) return false;
					vn.UserVN.WLStatus = remove ? null : (WishlistStatus?)statusInt;
					vn.UserVN.WLAdded = statusDate;
					break;
				case VisualNovelDatabase.ChangeType.Vote:
					int vote = (int)Math.Floor(newVoteValue * 10);
					queryString = statusInt == -1
						? $"set votelist {vn.VNID}"
						: $"set votelist {vn.VNID} {{\"vote\":{vote}}}";
					result = await TryQuery(queryString, Resources.cvns_query_error);
					if (!result) return false;
					vn.UserVN.Vote = vote;
					vn.UserVN.VoteAdded = statusDate;
					break;
			}
			var hasULStatus = vn.UserVN.ULStatus > UserlistStatus.None;
			var hasWLStatus = vn.UserVN.WLStatus > WishlistStatus.None;
			var hasVote = vn.UserVN.Vote > 0;
			if (!hasULStatus && !hasWLStatus && !hasVote)
			{
				vn.UserVNId = null;
				LocalDatabase.LocalUserVisualNovels.Remove(vn.UserVN);
			}
			LocalDatabase.SaveChanges();
			_changeStatusAction?.Invoke(Status);
			return true;
		}

		/// <summary>
		/// Get username from VNDB user ID, returns empty string if error.
		/// </summary>
		public async Task<string> GetUsernameFromID(int userID)
		{

			var result = await TryQueryNoReply($"get user basic (id={userID})");
			if (!result)
			{
				_changeStatusAction?.Invoke(Status);
				return "";
			}
			var response = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(LastResponse.JsonPayload);
			return response.Items.Any() ? response.Items[0].Username : "";
		}

		/// <summary>
		/// Get user ID from VNDB username, returns -1 if error.
		/// </summary>
		public async Task<int> GetIDFromUsername(string username)
		{
			var result = await TryQueryNoReply($"get user basic (username=\"{username}\")");
			if (!result)
			{
				_changeStatusAction?.Invoke(Status);
				return -1;
			}
			var response = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(LastResponse.JsonPayload);
			return response.Items.Any() ? response.Items[0].ID : -1;
		}

		/// <summary>
		/// Searches VNDB for producers by name, independent.
		/// Call StartQuery prior to it and ChangeAPIStatus afterwards.
		/// </summary>
		public async Task<List<ProducerItem>> AddProducersBySearchedName(string producerName)
		{
			string prodSearchQuery = $"get producer basic (search~\"{producerName}\") {{{MaxResultsString}}}";
			var result = await TryQuery(prodSearchQuery, Resources.ps_query_error);
			if (!result) return null;
			var prodRoot = JsonConvert.DeserializeObject<ResultsRoot<ProducerItem>>(LastResponse.JsonPayload);
			List<ProducerItem> prodItems = prodRoot.Items;
			var moreResults = prodRoot.More;
			var pageNo = 1;
			while (moreResults)
			{
				pageNo++;
				string prodSearchMoreQuery =
					$"get producer basic (search~\"{producerName}\") {{{MaxResultsString}, \"page\":{pageNo}}}";
				var moreResult =
					await TryQuery(prodSearchMoreQuery, Resources.ps_query_error);
				if (!moreResult) return null;
				var prodMoreRoot = JsonConvert.DeserializeObject<ResultsRoot<ProducerItem>>(LastResponse.JsonPayload);
				prodItems.AddRange(prodMoreRoot.Items);
				moreResults = prodMoreRoot.More;
			}
			for (int index = prodItems.Count - 1; index >= 0; index--)
			{
				if (LocalDatabase.LocalProducers.Any(x => x.Name.Equals(prodItems[index].Name))) prodItems.RemoveAt(index);
			}
			foreach (var producer in prodItems) LocalDatabase.UpsertProducer(producer, true, false);
			LocalDatabase.SaveChanges();
			return prodItems;

		}

		[ConnectionFunctionAspect, ConnectionInterceptAspect(true, true, true)]
		public async Task<ICollection<int>> SearchByNameOrAlias(string searchString)
		{
			string queryFormat = "get vn basic (search ~ \"{0}\")";
			var queryResult = await TryQuery(FormatQuery(queryFormat, searchString), Resources.vn_query_error);
			if (!queryResult) return null;
			var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
			List<VNItem> vnItems = vnRoot.Items;
			var pageNo = 1;
			var moreResults = vnRoot.More;
			while (moreResults)
			{
				pageNo++;
				queryResult = await TryQuery(FormatQuery(queryFormat, searchString, pageNo), Resources.vn_query_error);
				if (!queryResult) return null;
				vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				vnItems.AddRange(vnRoot.Items);
				moreResults = vnRoot.More;
			}
			var ids = new HashSet<int>(vnItems.Select(x => x.ID));
			await GetMultipleVN(ids, false);
			ActiveQuery.CompletedMessage = $"Got {ids.Count} items from search string '{searchString}'";
			return ids;
		}

		[ConnectionFunctionAspect, ConnectionInterceptAspect(true, true, true)]
		public async Task UpdateForYear(int year)
		{
			var ids = new HashSet<int>(LocalDatabase.LocalVisualNovels.Where(x => x.ReleaseDate.Year == year && x.DateUpdated < DateTime.UtcNow.AddDays(-2))
				.OrderByDescending(x => x.VNID).Select(x => x.VNID));
			await GetMultipleVN(ids, true);
			ActiveQuery.CompletedMessage = $"Updated titles released in {year}, ({ActiveQuery.TitlesAdded} items).";
		}

		[ConnectionFunctionAspect, ConnectionInterceptAspect(true, true, true)]
		public async Task UpdateCharactersForYear(int year)
		{
			var startTime = DateTime.UtcNow.ToLocalTime();
			var startTimeString = startTime.ToString("HH:mm");
			TextAction($"Updating characters for titles from {year}.  Started at {startTimeString}", MessageSeverity.Normal);
			var vnids = LocalDatabase.LocalVisualNovels.Where(x => x.ReleaseDate.Year == year && x.DateUpdated < DateTime.UtcNow.AddDays(-2)).OrderByDescending(x => x.VNID).Select(x => x.VNID);
			var set = new HashSet<int>(vnids);
			await Task.Run(() => GetCharacters(set, false));
			ActiveQuery.CompletedMessage = $"Updated characters for titles from {year}, ({ActiveQuery.CharactersAdded}/{ActiveQuery.CharactersUpdated} added/updated).";
		}

		[ConnectionFunctionAspect, ConnectionInterceptAspect(true, true, true)]
		public async Task FetchForYear(int fromYear = 0, int toYear = VndbAPIMaxYear)
		{
			var startTime = DateTime.UtcNow.ToLocalTime();
			var startTimeString = startTime.ToString("HH:mm");
			var yearString = $"{fromYear}-{toYear}";
			if (fromYear == toYear) yearString = $"{fromYear}";
			else if (fromYear == 0 && toYear == 9999) yearString = "all years";
			else if (fromYear == 0) yearString = $"<{toYear}";
			else if (toYear == 9999) yearString = $">{toYear}";
			TextAction($"Getting all titles from {yearString}.  Started at {startTimeString}", MessageSeverity.Normal);
			string vnInfoQuery = $"get vn basic (released > \"{fromYear - 1}\" and released <= \"{toYear}\") {{{MaxResultsString}}}";
			var result = await TryQuery(vnInfoQuery, Resources.gyt_query_error);
			if (!result) return;
			var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
			List<VNItem> vnItems = vnRoot.Items;
			await GetMultipleVN(new HashSet<int>(vnItems.Select(x => x.ID)), false);
			var pageNo = 1;
			var moreResults = vnRoot.More;
			while (moreResults)
			{
				pageNo++;
				string vnInfoMoreQuery =
					$"get vn basic (released > \"{fromYear - 1}\" and released <= \"{toYear}\") {{{MaxResultsString}, \"page\":{pageNo}}}";
				var moreResult = await TryQuery(vnInfoMoreQuery, Resources.gyt_query_error);
				if (!moreResult) return;
				var vnMoreRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				List<VNItem> vnMoreItems = vnMoreRoot.Items;
				await GetMultipleVN(new HashSet<int>(vnMoreItems.Select(x => x.ID)), false);
				moreResults = vnMoreRoot.More;
			}
			var span = DateTime.UtcNow.ToLocalTime() - startTime;
			ActiveQuery.CompletedMessage = $"Got titles from {yearString} in {span:hh\\:mm}. {ActiveQuery.TitlesAdded}/{ActiveQuery.TotalTitles} added.";
		}

#if DEBUG
		private const int VNIDToDebug = 20367;
#endif
		[ConnectionFunctionAspect, ConnectionInterceptAspect(true, true, true)]
		public async Task<uint> UpdateURT()
		{
			LogToFile($"Starting GetUserRelatedTitles for {CSettings.UserID}, previously had {LocalDatabase.URTVisualNovels.Count()} titles.");
			//clone list to make sure it doesnt keep command status.
			var pre = LocalDatabase.LocalUserVisualNovels.Where(x => x.UserId == CSettings.UserID).OrderBy(x => x.VNID).ToArray();
			List<VisualNovelDatabase.UrtListItem> localURTList = pre.Select(x => new VisualNovelDatabase.UrtListItem(x)).ToList();
			await GetUserList(localURTList);
			await GetWishList(localURTList);
			await GetVoteList(localURTList);
			LocalDatabase.UpdateURTTitles(CSettings.UserID, localURTList);
			await GetRemainingTitles();
			//SetFavoriteProducersData();
			//UpdateUserStats();
			ActiveQuery.CompletedMessage = $"Updated URT ({ActiveQuery.TitlesAdded} added).";
			CSettings.URTDate = DateTime.UtcNow;
			return ActiveQuery.TitlesAdded;
		}

		[ConnectionFunctionAspect, ConnectionInterceptAspect(false, false, true)]
		public async Task<bool> GetAndSetRelationsForVN(ListedVN vn)
		{
			await TryQuery($"get vn relations (id = {vn.VNID})", "Relations Query Error");
			var root = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
			if (root.Num == 0) return false;
			VNItem.RelationsItem[] relations = root.Items[0].Relations;
			await Task.Run(() => LocalDatabase.AddRelationsToVN(vn, relations));
			return true;
		}

		[ConnectionFunctionAspect, ConnectionInterceptAspect(false, false, true)]
		public async Task<bool> GetAndSetAnimeForVN(ListedVN vn)
		{
			await TryQuery($"get vn anime (id = {vn.VNID})", "Anime Query Error");
			var root = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
			if (root.Num == 0) return false;
			VNItem.AnimeItem[] anime = root.Items[0].Anime;
			await Task.Run(() => LocalDatabase.AddAnimeToVN(vn, anime));
			return true;
		}

		[ConnectionFunctionAspect, ConnectionInterceptAspect(false, false, true)]
		public async Task<bool> GetAndSetScreensForVN(ListedVN vn)
		{
			await TryQuery($"get vn screens (id = {vn.VNID})", "Screens Query Error");
			var root = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
			if (root.Num == 0) return false;
			VNItem.ScreenItem[] screens = root.Items[0].Screens;
			await Task.Run(() => LocalDatabase.AddScreensToVN(vn, screens));
			return true;
		}

		#endregion
	}
}
