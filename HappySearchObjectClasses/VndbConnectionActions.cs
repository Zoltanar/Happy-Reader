using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Happy_Apps_Core.Properties;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
    partial class VndbConnection
    {
        private enum QueryResult { Fail, Success, Throttled }

        public ApiQuery ActiveQuery;
        private readonly Action<string> _advancedAction;
        private readonly Action _refreshListAction;
        private readonly Action<APIStatus> _changeStatusAction;
        /// <summary>
        /// Count of titles added in last query.
        /// </summary>
        public int TitlesAdded { get; set; }
        /// <summary>
        /// Count of titles skipped in last query.
        /// </summary>
        public int TitlesSkipped { get; set; }
        private int _throttleWaitTime;

        /// <summary>
        /// Check if API Connection is ready, change status accordingly and write error if it isnt ready.
        /// </summary>
        /// <param name="replyLabel">Label where error reply will be printed</param>
        /// <param name="featureName">Name of feature calling the query</param>
        /// <param name="refreshList">Refresh OLV on throttled connection</param>
        /// <param name="additionalMessage">Print Added/Skipped message on throttled connection</param>
        /// <param name="ignoreDateLimit">Ignore 10 year limit (if applicable)</param>
        /// <returns>If connection was ready</returns>
        public bool StartQuery(Label replyLabel, string featureName, bool refreshList, bool additionalMessage, bool ignoreDateLimit)
        {
            if (!ActiveQuery.Completed)
            {
                WriteError(replyLabel, $"Wait until {ActiveQuery.ActionName} is done.");
                return false;
            }
            ActiveQuery = new ApiQuery(featureName, replyLabel, refreshList, additionalMessage, ignoreDateLimit);
            TitlesAdded = 0;
            TitlesSkipped = 0;
            return true;
        }

        /// <summary>
        /// Send query through API Connection.
        /// </summary>
        /// <param name="query">Command to be sent</param>
        /// <param name="errorMessage">Message to be printed in case of error</param>
        /// <returns>Returns whether it was successful.</returns>
        private async Task<QueryResult> TryQueryInner(string query, string errorMessage)
        {
            if (Status != APIStatus.Ready)
            {
                WriteError(ActiveQuery.ReplyLabel, "API Connection isn't ready.");
                return QueryResult.Fail;
            }
            Status = APIStatus.Busy;
            _changeStatusAction?.Invoke(Status);
            await Task.Run(() =>
            {
                if (Settings.DecadeLimit && (!ActiveQuery?.IgnoreDateLimit ?? false) && query.StartsWith("get vn ") && !query.Contains("id = "))
                {
                    query = Regex.Replace(query, "\\)", $" and released > \"{DateTime.UtcNow.Year - 10}\")");
                }
                LogToFile(query);
                Query(query);
            });
            _advancedAction?.Invoke(query);
            if (LastResponse.Type == ResponseType.Unknown)
            {
                return QueryResult.Fail;
            }
            while (LastResponse.Type == ResponseType.Error)
            {
                if (!LastResponse.Error.ID.Equals("throttled"))
                {
                    WriteError(ActiveQuery.ReplyLabel, errorMessage);
                    return QueryResult.Fail;
                }
                string fullThrottleMessage = "";
                double minWait = 0;
                await Task.Run(() =>
                {
#if DEBUG
                    minWait = Math.Min(5 * 60, LastResponse.Error.Fullwait); //wait 5 minutes
#else
                    minWait = Math.Min(5 * 60, LastResponse.Error.Fullwait); //wait 5 minutes
#endif
                    string normalWarning = $"Throttled for {Math.Floor(minWait / 60)} mins.";
                    string additionalWarning = "";
                    if (TitlesAdded > 0) additionalWarning += $" Added {TitlesAdded}.";
                    if (TitlesSkipped > 0) additionalWarning += $" Skipped {TitlesSkipped}.";
                    fullThrottleMessage = ActiveQuery.AdditionalMessage ? normalWarning + additionalWarning : normalWarning;
                });
                WriteWarning(ActiveQuery.ReplyLabel, fullThrottleMessage);
                LogToFile($"Local: {DateTime.Now} - {fullThrottleMessage}");
                var waitMS = minWait * 1000;
                _throttleWaitTime = Convert.ToInt32(waitMS);
                return QueryResult.Throttled;
            }
            return QueryResult.Success;
        }

        /// <summary>
        /// Send query through API Connection.
        /// </summary>
        /// <param name="query">Command to be sent</param>
        /// <param name="errorMessage">Message to be printed in case of error</param>
        /// <param name="setStatusOnEnd">Set to true to change status when ending this query (Only for direct calls)</param>
        /// <returns>Returns whether it was successful.</returns>
        public async Task<bool> TryQuery(string query, string errorMessage, bool setStatusOnEnd = false)
        {
            var result = await TryQueryInner(query, errorMessage);
            while (result == QueryResult.Throttled)
            {
                if (ActiveQuery.RefreshList)
                {
                    _refreshListAction();
                }
                _changeStatusAction?.Invoke(Status);
                await Task.Delay(_throttleWaitTime);
                Status = APIStatus.Ready;
                result = await TryQueryInner(query, errorMessage);
            }
            if (setStatusOnEnd) _changeStatusAction?.Invoke(Status);
            return result == QueryResult.Success;
        }

        /// <summary>
        /// Send query through API Connection, don't poste error messages back.
        /// </summary>
        /// <param name="query">Command to be sent</param>
        /// <returns>Returns whether it was successful.</returns>
        public async Task<bool> TryQueryNoReply(string query)
        {
            if (Status != APIStatus.Ready)
            {
                return false;
            }
            await Task.Run(() =>
            {
                LogToFile(query);
                Query(query);
            });
            _advancedAction?.Invoke(query);
            return LastResponse.Type != ResponseType.Unknown && LastResponse.Type != ResponseType.Error;
        }

        /// <summary>
        /// Get data about multiple visual novels.
        /// Creates its own SQLite Transactions.
        /// </summary>
        /// <param name="vnIDs">List of visual novel IDs</param>
        /// <param name="updateAll">If false, will skip VNs already fetched</param>
        public async Task GetMultipleVN(int[] vnIDs, bool updateAll)
        {
            var vnsToGet = new List<int>();
            await Task.Run(() =>
            {
                if (updateAll) vnsToGet = vnIDs.ToList();
                else
                {
                    vnsToGet = vnIDs.Except(LocalDatabase.VNList.Select(x => x.VNID)).ToList();
                    TitlesSkipped = TitlesSkipped + vnIDs.Length - vnsToGet.Count;
                }
                vnsToGet.Remove(0);
            });
            if (!vnsToGet.Any()) return;
            int done = 0;
            do
            {
                int[] currentArray = vnsToGet.Skip(done).Take(APIMaxResults).ToArray();
                string currentArrayString = '[' + string.Join(",", currentArray) + ']';
                string multiVNQuery =
                    $"get vn basic,details,tags,stats (id = {currentArrayString}) {{{MaxResultsString}}}";
                var queryResult = await TryQuery(multiVNQuery, Resources.gmvn_query_error);
                if (!queryResult) return;
                var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
                RemoveDeletedVNs(vnRoot, currentArray);
                var vnsToBeUpserted = new List<(VNItem VN, ProducerItem Producer, VNLanguages Languages)>();
                var producersToBeUpserted = new List<ProducerItem>();
                await HandleVNItems(vnRoot.Items, producersToBeUpserted, vnsToBeUpserted);
                vnsToBeUpserted.ForEach(vn => LocalDatabase.UpsertSingleVN(vn, true, false));
                producersToBeUpserted.ForEach(producer => LocalDatabase.UpsertProducer(producer, true, false));
                LocalDatabase.SaveChanges();
                await GetCharactersForMultipleVN(currentArray);
                done += APIMaxResults;
            } while (done < vnsToGet.Count);

            async Task HandleVNItems(List<VNItem> itemList, List<ProducerItem> upsertProducers, List<(VNItem VN, ProducerItem Producer, VNLanguages Languages)> upsertTitles)
            {
                foreach (var vnItem in itemList)
                {
                    SaveImage(vnItem);
                    var releases = await GetReleases(vnItem.ID, Resources.svn_query_error);
                    var mainRelease = releases.FirstOrDefault(item => item.Producers.Exists(x => x.Developer));
                    var relProducer = mainRelease?.Producers.FirstOrDefault(p => p.Developer);
                    VNLanguages languages = new VNLanguages(vnItem.Orig_Lang, vnItem.Languages);
                    if (relProducer != null)
                    {
                        var gpResult = await GetProducer(relProducer.ID, Resources.gmvn_query_error, updateAll);
                        if (!gpResult.Item1)
                        {
                            _changeStatusAction?.Invoke(Status);
                            return;
                        }
                        if (gpResult.Item2 != null) upsertProducers.Add(gpResult.Item2);
                    }
                    TitlesAdded++;
                    upsertTitles.Add((vnItem, relProducer, languages));
                }
            }
        }

        private void RemoveDeletedVNs(ResultsRoot<VNItem> root, int[] currentArray)
        {
            if (root.Num >= currentArray.Length) return;
            //some vns were deleted, find which ones and remove them
            IEnumerable<int> deletedVNs = currentArray.Where(currentvn => root.Items.All(receivedvn => receivedvn.ID != currentvn));
            foreach (var deletedVN in deletedVNs) LocalDatabase.RemoveVisualNovel(deletedVN, false);
            LocalDatabase.SaveChanges();
        }

        /// <summary>
        /// Get character data about multiple visual novels.
        /// Creates its own SQLite Transactions.
        /// </summary>
        /// <param name="vnIDs">List of VNs</param>
        public async Task GetCharactersForMultipleVN(int[] vnIDs)
        {
            if (!vnIDs.Any()) return;
            int[] currentArray = vnIDs.Take(APIMaxResults).ToArray();
            string currentArrayString = '[' + string.Join(",", currentArray) + ']';
            string charsForVNQuery = $"get character traits,vns (vn = {currentArrayString}) {{{MaxResultsString}}}";
            var queryResult = await TryQuery(charsForVNQuery, "GetCharactersForMultipleVN Query Error");
            if (!queryResult) return;
            var charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
            foreach (var character in charRoot.Items) LocalDatabase.UpsertSingleCharacter(character, false);
            LocalDatabase.SaveChanges();
            bool moreResults = charRoot.More;
            int pageNo = 1;
            while (moreResults)
            {
                if (!await HandleMoreResults()) return;
            }
            int done = APIMaxResults;
            while (done < vnIDs.Length)
            {
                currentArray = vnIDs.Skip(done).Take(APIMaxResults).ToArray();
                currentArrayString = '[' + string.Join(",", currentArray) + ']';
                charsForVNQuery = $"get character traits,vns (vn = {currentArrayString}) {{{MaxResultsString}}}";
                queryResult = await TryQuery(charsForVNQuery, "GetCharactersForMultipleVN Query Error");
                if (!queryResult) return;
                charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
                foreach (var character in charRoot.Items) LocalDatabase.UpsertSingleCharacter(character, false);
                LocalDatabase.SaveChanges();
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
                charsForVNQuery = $"get character traits,vns (vn = {currentArrayString}) {{{MaxResultsString}, \"page\":{pageNo}}}";
                queryResult = await TryQuery(charsForVNQuery, "GetCharactersForMultipleVN Query Error");
                if (!queryResult) return false;
                charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(LastResponse.JsonPayload);
                foreach (var character in charRoot.Items) LocalDatabase.UpsertSingleCharacter(character, false);
                LocalDatabase.SaveChanges();
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
        internal async Task<List<ReleaseItem>> GetReleases(int vnid, string errorMessage)
        {
            string developerQuery = $"get release basic,producers (vn =\"{vnid}\") {{{MaxResultsString}}}";
            var releaseResult =
                await TryQuery(developerQuery, errorMessage);
            if (!releaseResult) return null;
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
        public async Task<(bool, ProducerItem)> GetProducer(int producerID, string errorMessage, bool update)
        {
            if (!update && (producerID == -1 || LocalDatabase.ProducerList.Any(p => p.ID == producerID))) return (true, null);
            string producerQuery = $"get producer basic (id={producerID})";
            var producerResult =
                await TryQuery(producerQuery, errorMessage);
            if (!producerResult) return (false, null);
            var root = JsonConvert.DeserializeObject<ResultsRoot<ProducerItem>>(LastResponse.JsonPayload);
            List<ProducerItem> producers = root.Items;
            if (!producers.Any()) return (true, null);
            var producer = producers.First();
            return (true, producer);
        }

        /// <summary>
        /// Update tags, traits and stats of titles.
        /// </summary>
        /// <param name="vnIDs">List of IDs of titles to be updated.</param>
        public async Task UpdateTagsTraitsStats(IEnumerable<int> vnIDs)
        {
            List<int> vnsToGet = vnIDs.ToList();
            if (!vnsToGet.Any()) return;
            int[] currentArray = vnsToGet.Take(APIMaxResults).ToArray();
            string currentArrayString = '[' + string.Join(",", currentArray) + ']';
            string multiVNQuery = $"get vn tags,stats (id = {currentArrayString}) {{{MaxResultsString}}}";
            var queryResult = await TryQuery(multiVNQuery, Resources.gmvn_query_error);
            if (!queryResult) return;
            var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
            if (vnRoot.Num < currentArray.Length)
            {
                //some vns were deleted, find which ones and remove them
                var root = vnRoot;
                IEnumerable<int> deletedVNs = currentArray.Where(currentvn => root.Items.All(receivedvn => receivedvn.ID != currentvn));
                foreach (var deletedVN in deletedVNs) LocalDatabase.RemoveVisualNovel(deletedVN, false);
                LocalDatabase.SaveChanges();
            }
            foreach (var vnItem in vnRoot.Items)
            {
                LocalDatabase.UpdateVNTagsStats(vnItem, false);
                TitlesAdded++;
            }
            LocalDatabase.SaveChanges();
            await GetCharactersForMultipleVN(currentArray);
            int done = APIMaxResults;
            while (done < vnsToGet.Count)
            {
                currentArray = vnsToGet.Skip(done).Take(APIMaxResults).ToArray();
                currentArrayString = '[' + string.Join(",", currentArray) + ']';
                multiVNQuery = $"get vn tags,stats (id = {currentArrayString}) {{{MaxResultsString}}}";
                queryResult = await TryQuery(multiVNQuery, Resources.gmvn_query_error);
                if (!queryResult) return;
                vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
                if (vnRoot.Num < currentArray.Length)
                {
                    //some vns were deleted, find which ones and remove them
                    var root = vnRoot;
                    IEnumerable<int> deletedVNs = currentArray.Where(currentvn => root.Items.All(receivedvn => receivedvn.ID != currentvn));
                    foreach (var deletedVN in deletedVNs) LocalDatabase.RemoveVisualNovel(deletedVN, false);
                    LocalDatabase.SaveChanges();
                }
                foreach (var vnItem in vnRoot.Items)
                {
                    LocalDatabase.UpdateVNTagsStats(vnItem, false);
                    TitlesAdded++;
                }
                LocalDatabase.SaveChanges();
                await GetCharactersForMultipleVN(currentArray);
                done += APIMaxResults;
            }
        }

        /// <summary>
        /// Change userlist status, wishlist priority or user vote.
        /// </summary>
        /// <param name="vn">VN which will be changed</param>
        /// <param name="type">What is being changed</param>
        /// <param name="statusInt">The new value</param>
        /// <param name="newVoteValue">New vote value</param>
        /// <returns>Returns whether it as successful.</returns>
        public async Task<bool> ChangeVNStatus(ListedVN vn, VNDatabase.ChangeType type, int statusInt, double newVoteValue = -1)
        {
            bool remove = statusInt == -1;
            int? statusDate = null;
            if (statusInt != -1) statusDate = (int)DateTimeToUnixTimestamp(DateTime.UtcNow);
            string queryString;
            _changeStatusAction(APIStatus.Busy);
            switch (type)
            {
                case VNDatabase.ChangeType.UL:
                    queryString = statusInt == -1 ? $"set vnlist {vn.VNID}" : $"set vnlist {vn.VNID} {{\"status\":{statusInt}}}";
                    var result = await TryQuery(queryString, Resources.cvns_query_error);
                    if (!result) return false;
                    vn.UserVN.ULStatus = remove ? null : (UserlistStatus?)statusInt;
                    vn.UserVN.ULAdded = statusDate;
                    break;
                case VNDatabase.ChangeType.WL:
                    queryString = statusInt == -1
                        ? $"set wishlist {vn.VNID}"
                        : $"set wishlist {vn.VNID} {{\"priority\":{statusInt}}}";
                    result = await TryQuery(queryString, Resources.cvns_query_error);
                    if (!result) return false;
                    vn.UserVN.WLStatus = remove ? null : (WishlistStatus?)statusInt;
                    vn.UserVN.WLAdded = statusDate;
                    break;
                case VNDatabase.ChangeType.Vote:
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
                LocalDatabase.UserVNList.Remove(vn.UserVN);
            }
            LocalDatabase.SaveChanges();
            _changeStatusAction(Status);
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

        public async Task GetLanguagesForProducers(int[] producerIDs)
        {
            if (!producerIDs.Any()) return;
            var producerList = new List<ProducerItem>();
            foreach (var producerID in producerIDs)
            {
                var result = await GetProducer(producerID, "GetLanguagesForProducers Error", false);
                if (!result.Item1 || result.Item2 == null) continue;
                producerList.Add(result.Item2);
                TitlesAdded++;
                if (producerList.Count > 24)
                {
                    foreach (var producer in producerList)
                    {
                        var dbProducer = LocalDatabase.ProducerList.Single(x => x.ID == producer.ID);
                        dbProducer.Language = producer.Language;
                    }
                    LocalDatabase.SaveChanges();
                    producerList.Clear();
                }
            }
            foreach (var producer in producerList)
            {
                var dbProducer = LocalDatabase.ProducerList.Single(x => x.ID == producer.ID);
                dbProducer.Language = producer.Language;
            }
            LocalDatabase.SaveChanges();
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
                if (LocalDatabase.ProducerList.Any(x => x.Name.Equals(prodItems[index].Name))) prodItems.RemoveAt(index);
            }
            foreach (var producer in prodItems) LocalDatabase.UpsertProducer(producer, true, false);
            LocalDatabase.SaveChanges();
            return prodItems;

        }

        public async Task<int[]> SearchByNameOrAlias(string searchString)
        {
            string vnSearchQuery = $"get vn basic (search ~ \"{searchString}\") {{{MaxResultsString}}}";
            var queryResult = await TryQuery(vnSearchQuery, Resources.vn_query_error);
            if (!queryResult) return null;
            var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
            List<VNItem> vnItems = vnRoot.Items;
            var pageNo = 1;
            var moreResults = vnRoot.More;
            while (moreResults)
            {
                pageNo++;
                vnSearchQuery = $"get vn basic (search ~ \"{searchString}\") {{{MaxResultsString}, \"page\":{pageNo}}}";
                queryResult = await TryQuery(vnSearchQuery, Resources.vn_query_error);
                if (!queryResult) return null;
                vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(LastResponse.JsonPayload);
                vnItems.AddRange(vnRoot.Items);
                moreResults = vnRoot.More;
            }
            var ids = vnItems.Select(x => x.ID).ToArray();
            await GetMultipleVN(ids, false);
            _changeStatusAction?.Invoke(Status);
            return ids;
        }

#if DEBUG
        private const int VNIDToDebug = 20367;
#endif

        /// <summary>
        ///     Get user's userlist from VNDB, add titles that aren't in local db already.
        /// </summary>
        /// <param name="urtList">list of title IDs (avoids duplicate fetching)</param>
        /// <returns>list of title IDs (avoids duplicate fetching)</returns>
        public async Task GetUserList(List<VNDatabase.UrtListItem> urtList)
        {
            LogToFile("Starting GetUserList");
            string userListQuery = $"get vnlist basic (uid = {Settings.UserID} ) {{\"results\":100}}";
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
                string userListQuery2 = $"get vnlist basic (uid = {Settings.UserID} ) {{\"results\":100, \"page\":{pageNo}}}";
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
                if (itemInlist == null) urtList.Add(new VNDatabase.UrtListItem(item));
                //update if it already exists
                else itemInlist.Update(item);
            }
        }

        public async Task GetWishList(List<VNDatabase.UrtListItem> urtList)
        {
            LogToFile("Starting GetWishList");
            string wishListQuery = $"get wishlist basic (uid = {Settings.UserID} ) {{\"results\":100}}";
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
                string wishListQuery2 = $"get wishlist basic (uid = {Settings.UserID} ) {{\"results\":100, \"page\":{pageNo}}}";
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
                if (itemInlist == null) urtList.Add(new VNDatabase.UrtListItem(item));
                //update if it already exists
                else itemInlist.Update(item);
            }
        }

        public async Task GetVoteList(List<VNDatabase.UrtListItem> urtList)
        {
            LogToFile("Starting GetVoteList");
            string voteListQuery = $"get votelist basic (uid = {Settings.UserID} ) {{\"results\":100}}";
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
                string voteListQuery2 = $"get votelist basic (uid = {Settings.UserID} ) {{\"results\":100, \"page\":{pageNo}}}";
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
                if (itemInlist == null) urtList.Add(new VNDatabase.UrtListItem(item));
                //update if it already exists
                else itemInlist.Update(item);
            }
        }

    }
}
