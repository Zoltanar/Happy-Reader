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
		private readonly Action<List<int>> _refreshListOnAddAction;
		private readonly Action<APIStatus> _changeStatusAction;
		[NotNull]
		public readonly Action<string, MessageSeverity> TextAction;
		[NotNull]
		public readonly Func<bool> AskForNonSslAction;


		public VndbConnection(
			[NotNull]Action<string, MessageSeverity> textAction,
			Action<string, bool> advancedModeAction,
			Action<List<int>> refreshListOnAddAction,
			Func<bool> askForNonSsl,
			Action<APIStatus> changeStatusAction = null)
		{
			TextAction = textAction;
			_advancedAction = advancedModeAction;
			_refreshListOnAddAction = refreshListOnAddAction;
			AskForNonSslAction = askForNonSsl;
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
			if (CSettings.UserID < 1) return false;
			if (ActiveQuery != null && !ActiveQuery.Completed)
			{
				TextAction($"Wait until {ActiveQuery.ActionName} is done.", MessageSeverity.Error);
				return false;
			}
			ActiveQuery = new ApiQuery(featureName, refreshList, additionalMessage, _refreshListOnAddAction);
			TextAction($"Running {featureName}...", MessageSeverity.Normal);
			return true;
		}

		internal void EndQuery()
		{
			ActiveQuery.RunActionOnAdd();
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
				var allVNIds = new HashSet<int>(LocalDatabase.VisualNovels.Select(x => x.VNID));
				var preSkipped = new HashSet<int>(vnIDs);
				preSkipped.IntersectWith(allVNIds);
				vnIDs.ExceptWith(allVNIds);
				ActiveQuery.AddTitlesSkipped(preSkipped);
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
				await HandleVNItems(vnRoot.Items, producersToBeUpserted, vnsToBeUpserted, updateAll);
				UpdateVisualNovelsInDatabase(vnsToBeUpserted, producersToBeUpserted);
				await GetCharacters(currentArray);
				done += APIMaxResults;
			} while (done < vnIDs.Count);
		}

		private async Task HandleVNItems(List<VNItem> itemList, Dictionary<int, ProducerItem> upsertProducers, List<(VNItem VN, ProducerItem Producer, VNLanguages Languages)> upsertTitles, bool updateAll)
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
				ActiveQuery.AddTitleAdded(vnItem.ID);
				upsertTitles.Add((vnItem, relProducer, languages));
			}
		}

		private void UpdateVisualNovelsInDatabase(List<(VNItem VN, ProducerItem Producer, VNLanguages Languages)> vnsToBeUpserted, Dictionary<int, ProducerItem> producersToBeUpserted)
		{
			LocalDatabase.Connection.Open();
			try
			{
				vnsToBeUpserted.ForEach(vn => LocalDatabase.UpsertSingleVN(vn, true, false));
				foreach (var producer in producersToBeUpserted.Values) LocalDatabase.UpsertProducer(producer, false);
				ActiveQuery.RunActionOnAdd();
			}
			catch (Exception ex)
			{
				Logger.ToFile(ex);
				throw;
			}
			finally
			{
				LocalDatabase.Connection.Close();
			}
		}

		/// <summary>
		/// Get character data about multiple visual novels.
		/// </summary>
		/// <param name="ids">List of visual novel ids to fetch characters for.</param>
		private async Task GetCharacters(HashSet<int> ids)
		{
			if (!ids.Any()) return;
			var currentArray = ids.Take(APIMaxResults).ToList();
			string queryFormat = "get character basic,details,traits,vns,voiced (vn = {{0}})";
			var queryResult = await TryQuery(FormatQuery(queryFormat, currentArray), $"{nameof(GetCharacters)} Query Error");
			if (!queryResult) return;
			ResultsRoot<CharacterItem> charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
			UpdateCharactersInDatabase(charRoot);
			bool moreResults = charRoot.More;
			int pageNo = 1;
			var success = await ProcessMoreResults();
			if (!success) return;
			int done = APIMaxResults;
			while (done < ids.Count)
			{
				currentArray = ids.Skip(done).Take(APIMaxResults).ToList();
				queryResult = await TryQuery(FormatQuery(queryFormat, currentArray), $"{nameof(GetCharacters)} Query Error");
				if (!queryResult) return;
				charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
				UpdateCharactersInDatabase(charRoot);
				moreResults = charRoot.More;
				pageNo = 1;
				success = await ProcessMoreResults();
				if (!success) return;
				done += APIMaxResults;
			}

			async Task<bool> ProcessMoreResults()
			{
				// ReSharper disable AccessToModifiedClosure
				while (moreResults)
				{
					pageNo++;
					var result = await HandleMoreCharacterResults(queryFormat, currentArray, charRoot, pageNo);
					moreResults = result.MoreResults;
					if (!result.Success) return false;
				}
				return true;
				// ReSharper restore AccessToModifiedClosure
			}
		}

		private void UpdateCharactersInDatabase(ResultsRoot<CharacterItem> charRoot)
		{
			LocalDatabase.Connection.Open();
			try
			{
				foreach (var character in charRoot.Items)
				{
					if (LocalDatabase.UpsertSingleCharacter(character, false)) ActiveQuery.AddCharactersAdded();
					else ActiveQuery.AddCharactersUpdated();
				}
			}
			finally
			{
				LocalDatabase.Connection.Close();
			}
		}

		private async Task<(bool Success, bool MoreResults)> HandleMoreCharacterResults(string queryFormat, List<int> currentArray, ResultsRoot<CharacterItem> charRoot, int pageNo)
		{
			var queryResult = await TryQuery(FormatQuery(queryFormat, currentArray, pageNo), $"{nameof(GetCharacters)} Query Error");
			if (!queryResult) return (false,false);
			charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
			await Task.Run(() =>
			{
				UpdateCharactersInDatabase(charRoot);
			});
			// ReSharper restore AccessToModifiedClosure
			return (true,charRoot.More);
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
			releaseItems.Sort((x, y) => DateTime.Compare(StringToDate(x.Released, out _), StringToDate(y.Released, out _)));
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
			if (!update && (producerID == -1 || LocalDatabase.Producers[producerID] != null)) return (true, null);
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
			var userTitles = LocalDatabase.UserVisualNovels.Where(x => x.UserId == CSettings.UserID).Select(x => x.VNID);
			var fetchedTitles = LocalDatabase.VisualNovels.Select(x => x.VNID);
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
			await Task.Delay(0);
			throw new NotImplementedException("Change to use ulist");
			/*Logger.ToFile("Starting GetUserList");
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
			}*/
		}

		private async Task GetWishList(List<VisualNovelDatabase.UrtListItem> urtList)
		{
			Logger.ToFile("Starting GetWishList");
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
			Logger.ToFile("Starting GetVoteList");
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

		/// <summary>
		/// Get titles developed/published by producer.
		/// </summary>
		/// <param name="producer">Producer whose titles should be found</param>
		/// <param name="updateAll">Should already known fetched titles be updated as well?</param>
		private async Task GetProducerTitles(ListedProducer producer, bool updateAll)
		{
			Logger.ToFile(($"Starting {nameof(GetProducerTitles)} for Producer {producer.Name}"));
			string prodReleaseQuery = $"get release vn (producer={producer.ID}) {{{MaxResultsString}}}";
			var result = await TryQuery(prodReleaseQuery, Resources.upt_query_error);
			if (!result) return;
			var releaseRoot = JsonConvert.DeserializeObject<ResultsRoot<ReleaseItem>>(LastResponse.JsonPayload);
			List<ReleaseItem> releaseItems = releaseRoot.Items;
			List<int> producerVNList = releaseItems.SelectMany(item => item.VN.Select(x => x.ID)).ToList();
			var moreResults = releaseRoot.More;
			var pageNo = 1;
			while (moreResults)
			{
				pageNo++;
				string prodReleaseMoreQuery =
						$"get release vn (producer={producer.ID}) {{{MaxResultsString}, \"page\":{pageNo}}}";
				var moreResult = await TryQuery(prodReleaseMoreQuery, Resources.upt_query_error);
				if (!moreResult) return;
				releaseRoot = JsonConvert.DeserializeObject<ResultsRoot<ReleaseItem>>(LastResponse.JsonPayload);
				releaseItems = releaseRoot.Items;
				producerVNList.AddRange(releaseItems.SelectMany(item => item.VN.Select(x => x.ID)));
				moreResults = releaseRoot.More;
			}
			await GetMultipleVN(new HashSet<int>(producerVNList), updateAll);
		}

		#endregion

		#region Public Functions

		/// <summary>
		/// Update tags, traits and stats of titles.
		/// </summary>
		/// <param name="vnIDs">List of IDs of titles to be updated.</param>
		public async Task UpdateTagsTraitsStats(HashSet<int> vnIDs)
		{
			if (!StartQuery(nameof(UpdateTagsTraitsStats), true, true, true)) return;
			try
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
					ActiveQuery.AddTitleAdded(vnItem.ID);
				}
				await GetCharacters(currentArray);
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
						ActiveQuery.AddTitleAdded(vnItem.ID);
					}
					await GetCharacters(currentArray);
					done += APIMaxResults;
				}

				ActiveQuery.CompletedMessage =
						$"Updated tags, traits and stats for {ActiveQuery.TitlesAdded.Count} titles.";
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
			}
			finally
			{
				EndQuery();
			}
		}

		/// <summary>
		/// Change  user vote.
		/// </summary>
		/// <param name="vn">VN which will be changed</param>
		/// <param name="vote">New vote value</param>
		/// <returns>Returns whether it as successful.</returns>
		public async Task<bool> ChangeVote(ListedVN vn, int? vote)
		{
			if (!StartQuery(nameof(ChangeVote), false, false, true)) return false;
			try
			{
				bool remove = !vote.HasValue;
				_changeStatusAction?.Invoke(APIStatus.Busy);
				var userVn = vn.UserVN ?? new UserVN { UserId = CSettings.UserID, VNID = vn.VNID };
				var queryString = $"set ulist {vn.VNID} {{\"vote\":{vote}}}";
				var result = await TryQuery(queryString, Resources.cvns_query_error);
				if (!result) return false;
				userVn.Vote = vote;
				if (remove) userVn.Labels.Remove(UserVN.LabelKind.Voted);
				else userVn.Labels.Add(UserVN.LabelKind.Voted);
				userVn.VoteAdded = DateTime.UtcNow;
				if (userVn.Labels.Any())
				{
					LocalDatabase.UserVisualNovels.Upsert(userVn, true);
				}
				else
				{
					LocalDatabase.UserVisualNovels.Remove(userVn, true);
				}
				_changeStatusAction?.Invoke(Status);
				return true;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return false;
			}
			finally
			{
				EndQuery();
			}
		}

		/// <summary>
		/// Change labels of VN.
		/// </summary>
		/// <param name="vn">VN which will be changed</param>
		/// <param name="labels">New labels to set</param>
		/// <returns>Returns whether it as successful.</returns>
		public async Task<bool> ChangeVNStatus(ListedVN vn, HashSet<UserVN.LabelKind> labels)
		{
			if (!StartQuery(nameof(ChangeVNStatus), false, false, true)) return false;
			try
			{
				string queryString;
				_changeStatusAction?.Invoke(APIStatus.Busy);
				var userVn = vn.UserVN ?? new UserVN { UserId = CSettings.UserID, VNID = vn.VNID };
				queryString = $"set ulist {vn.VNID} {{\"labels\":[{string.Join(",", labels.Cast<int>())}]}}";
				var result = await TryQuery(queryString, Resources.cvns_query_error);
				if (!result) return false;
				userVn.Labels = labels.ToHashSet();
				userVn.Added = DateTime.UtcNow;
				if (userVn.Labels.Any())
				{
					LocalDatabase.UserVisualNovels.Upsert(userVn, true);
				}
				else
				{
					LocalDatabase.UserVisualNovels.Remove(userVn, true);
				}
				_changeStatusAction?.Invoke(Status);
				return true;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return false;
			}
			finally
			{
				EndQuery();
			}/**/
		}

		/// <summary>
		/// Get username from VNDB user ID, returns empty string if error.
		/// </summary>
		public async Task<string> GetUsernameFromID(int userID)
		{
			if (!StartQuery(nameof(GetUsernameFromID), false, false, true)) return "";
			try
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
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return "";
			}
			finally
			{
				EndQuery();
			}
		}

		/// <summary>
		/// Get user ID from VNDB username, returns -1 if error.
		/// </summary>
		public async Task<int> GetIDFromUsername(string username)
		{
			if (!StartQuery(nameof(GetIDFromUsername), false, false, true)) return -1;
			try
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
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return -1;
			}
			finally
			{
				EndQuery();
			}
		}

		/// <summary>
		/// Searches VNDB for producers by name, independent.
		/// Call StartQuery prior to it and ChangeAPIStatus afterwards.
		/// </summary>
		public async Task<List<ProducerItem>> AddProducersBySearchedName(string producerName)
		{
			if (!StartQuery(nameof(AddProducersBySearchedName), false, false, true)) return null;
			try
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
					string prodSearchMoreQuery = $"get producer basic (search~\"{producerName}\") {{{MaxResultsString}, \"page\":{pageNo}}}";
					var moreResult = await TryQuery(prodSearchMoreQuery, Resources.ps_query_error);
					if (!moreResult) return null;
					var prodMoreRoot = JsonConvert.DeserializeObject<ResultsRoot<ProducerItem>>(LastResponse.JsonPayload);
					prodItems.AddRange(prodMoreRoot.Items);
					moreResults = prodMoreRoot.More;
				}
				for (int index = prodItems.Count - 1; index >= 0; index--)
				{
					if (LocalDatabase.Producers.Any(x => x.Name.Equals(prodItems[index].Name))) prodItems.RemoveAt(index);
				}
				foreach (var producer in prodItems) LocalDatabase.UpsertProducer(producer, false);
				return prodItems;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return null;
			}
			finally
			{
				EndQuery();
			}

		}

		public async Task<ICollection<int>> SearchByNameOrAlias(string searchString)
		{
			if (!StartQuery(nameof(SearchByNameOrAlias), true, true, true)) return null;
			try
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
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return null;
			}
			finally
			{
				EndQuery();
			}
		}

		public async Task UpdateForYear(int year)
		{
			if (!StartQuery(nameof(UpdateForYear), true, true, true)) return;
			try
			{
				var ids = new HashSet<int>(LocalDatabase.VisualNovels.Where(x => x.ReleaseDate.Year == year && x.DateUpdated < DateTime.UtcNow.AddDays(-2))
				.OrderByDescending(x => x.VNID).Select(x => x.VNID));
				await GetMultipleVN(ids, true);
				ActiveQuery.CompletedMessage = $"Updated titles released in {year}, ({ActiveQuery.TitlesAdded.Count} items).";
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
			}
			finally
			{
				EndQuery();
			}
		}

		public async Task UpdateCharactersForYear(int year)
		{
			if (!StartQuery(nameof(UpdateCharactersForYear), true, true, true)) return;
			try
			{
				var startTime = DateTime.UtcNow.ToLocalTime();
				var startTimeString = startTime.ToString("HH:mm");
				TextAction($"Updating characters for titles from {year}.  Started at {startTimeString}", MessageSeverity.Normal);
				var vnids = LocalDatabase.VisualNovels.Where(x => x.ReleaseDate.Year == year && x.DateUpdated < DateTime.UtcNow.AddDays(-2)).OrderByDescending(x => x.VNID).Select(x => x.VNID);
				var set = new HashSet<int>(vnids);
				await Task.Run(() => GetCharacters(set));

				ActiveQuery.CompletedMessage = $"Updated characters for titles from {year}, ({ActiveQuery.CharactersAdded}/{ActiveQuery.CharactersUpdated} added/updated).";
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
			}
			finally
			{
				EndQuery();
			}
		}

		public async Task FetchForYear(int fromYear = 0, int toYear = VndbAPIMaxYear)
		{
			if (!StartQuery(nameof(FetchForYear), true, true, true)) return;
			try
			{
				var startTime = DateTime.UtcNow.ToLocalTime();
				var startTimeString = startTime.ToString("HH:mm");
				var yearString = $"{fromYear}-{toYear}";
				if (fromYear == toYear) yearString = $"{fromYear}";
				else if (fromYear == 0 && toYear == 9999) yearString = "all years";
				else if (fromYear == 0) yearString = $"<{toYear}";
				else if (toYear == 9999) yearString = $">{toYear}";
				TextAction($"Getting all titles from {yearString}.  Started at {startTimeString}", MessageSeverity.Normal);
				var filterText = $"released > \"{fromYear - 1}\" and released <= \"{toYear}\"";
				var (success, time) = await GetValue(filterText, startTime);
				if (!success) return;
				var endMessage = $"Got titles from {yearString} in {time:hh\\:mm}. {ActiveQuery.TitlesAdded.Count}/{ActiveQuery.TotalTitles} added.";
				ActiveQuery.CompletedMessage = endMessage;
			}
			catch (Exception ex)
			{
				Logger.ToFile(ex);
			}
			finally
			{
				EndQuery();
			}
		}

		private async Task<(bool, TimeSpan)> GetValue(string filterText, DateTime startTime)
		{
			string vnInfoQuery =
				$"get vn basic ({filterText}) {{{MaxResultsString}}}";
			var result = await TryQuery(vnInfoQuery, Resources.gyt_query_error);
			if (!result)
			{
				return (false, default);
			}

			var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
			List<VNItem> vnItems = vnRoot.Items;
			await GetMultipleVN(new HashSet<int>(vnItems.Select(x => x.ID)), false);
			var pageNo = 1;
			var moreResults = vnRoot.More;
			while (moreResults)
			{
				pageNo++;
				string vnInfoMoreQuery = $"get vn basic ({filterText}) {{{MaxResultsString}, \"page\":{pageNo}}}";
				var moreResult = await TryQuery(vnInfoMoreQuery, Resources.gyt_query_error);
				if (!moreResult) return (false, default);
				var vnMoreRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				List<VNItem> vnMoreItems = vnMoreRoot.Items;
				await GetMultipleVN(new HashSet<int>(vnMoreItems.Select(x => x.ID)), false);
				moreResults = vnMoreRoot.More;
			}
			var span = DateTime.UtcNow.ToLocalTime() - startTime;
			return (true, span);
		}

#if DEBUG
		private const int VNIDToDebug = 20367;
#endif

		public async Task<int> UpdateURT()
		{
			if (!StartQuery(nameof(UpdateURT), true, true, true)) return -1;
			try
			{
				Logger.ToFile($"Starting {nameof(UpdateURT)} for {CSettings.UserID}, previously had {LocalDatabase.URTVisualNovels.Count()} titles.");
				//clone list to make sure it doesnt keep command status.
				var pre = LocalDatabase.UserVisualNovels.Where(x => x.UserId == CSettings.UserID).OrderBy(x => x.VNID).ToArray();
				List<VisualNovelDatabase.UrtListItem> localURTList = pre.Select(x => new VisualNovelDatabase.UrtListItem(x)).ToList();
				await GetUserList(localURTList);
				await GetWishList(localURTList);
				await GetVoteList(localURTList);
				LocalDatabase.UpdateURTTitles(CSettings.UserID, localURTList);
				await GetRemainingTitles();
				//SetFavoriteProducersData();
				//UpdateUserStats();
				ActiveQuery.CompletedMessage = $"Updated URT ({ActiveQuery.TitlesAdded.Count} added).";
				CSettings.URTDate = DateTime.UtcNow;
				return ActiveQuery.TitlesAdded.Count;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return -1;
			}
			finally
			{
				EndQuery();
			}
		}

		public async Task<bool> GetAndSetRelationsForVN(ListedVN vn)
		{
			if (!StartQuery(nameof(GetAndSetRelationsForVN), false, false, true)) return false;
			try
			{
				await TryQuery($"get vn relations (id = {vn.VNID})", "Relations Query Error");
				var root = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				if (root.Num == 0) return false;
				VNItem.RelationsItem[] relations = root.Items[0].Relations;
				await Task.Run(() => LocalDatabase.AddRelationsToVN(vn, relations));
				return true;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return false;
			}
			finally
			{
				EndQuery();
			}
		}

		public async Task<bool> GetAndSetAnimeForVN(ListedVN vn)
		{
			if (!StartQuery(nameof(GetAndSetAnimeForVN), false, false, true)) return false;
			try
			{
				await TryQuery($"get vn anime (id = {vn.VNID})", "Anime Query Error");
				var root = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				if (root.Num == 0) return false;
				VNItem.AnimeItem[] anime = root.Items[0].Anime;
				await Task.Run(() => LocalDatabase.AddAnimeToVN(vn, anime));
				return true;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return false;
			}
			finally
			{
				EndQuery();
			}
		}

		public async Task<bool> GetAndSetScreensForVN(ListedVN vn)
		{
			if (!StartQuery(nameof(GetAndSetScreensForVN), false, false, true)) return false;
			try
			{
				await TryQuery($"get vn screens (id = {vn.VNID})", "Screens Query Error");
				var root = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
				if (root.Num == 0) return false;
				VNItem.ScreenItem[] screens = root.Items[0].Screens;
				await Task.Run(() => LocalDatabase.AddScreensToVN(vn, screens));
				return true;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return false;
			}
			finally
			{
				EndQuery();
			}
		}

		public async Task UpdateForProducers(IEnumerable<ListedProducer> producers)
		{
			if (!StartQuery(nameof(UpdateForProducers), true, true, true)) return;
			try
			{
				foreach (var producer in producers) await GetProducerTitles(producer, false);
				ActiveQuery.CompletedMessage = $"Finished {nameof(UpdateForProducers)}, got {ActiveQuery.TitlesAdded.Count} new titles.";
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
			}
			finally
			{
				EndQuery();
			}
		}

		#endregion

		public async Task<bool> UpdateVN(ListedVN vn)
		{
			if (!StartQuery(nameof(UpdateVN), false, true, false)) return false;
			try
			{
				await Conn.GetMultipleVN(new HashSet<int> { vn.VNID }, true);
				return true;
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return false;
			}
			finally
			{
				EndQuery();
			}
		}
	}
}
