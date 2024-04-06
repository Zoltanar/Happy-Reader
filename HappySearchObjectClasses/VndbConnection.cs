using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Happy_Apps_Core.API_Objects;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
    /// <summary>
    /// Class for establishing connection with VNDB API and interacting with it.
    /// </summary>
    public partial class VndbConnection
    {
        private const string VndbHost = "api.vndb.org/kana";

        private readonly Action<ApiStatus> _changeStatusAction;
        [NotNull] private readonly Action<string, MessageSeverity> _textAction;
        private LogInStatus _logIn = LogInStatus.No;
        private ApiStatus _status = ApiStatus.Closed;
        private AuthInfo _authInfo;
        private string _apiToken;

        public ApiQuery ActiveQuery { get; private set; }

        public VndbConnection([NotNull] Action<string, MessageSeverity> textAction, Action<ApiStatus> changeStatusAction)
        {
            _textAction = textAction;
            _changeStatusAction = changeStatusAction;
        }

        #region Public Functions
        /// <summary>
        /// Authenticate VNDB API, using API Token.
        /// </summary>
        public string Login(string apiToken)
        {
            _status = ApiStatus.Closed;
            _logIn = LogInStatus.No;
            _apiToken = apiToken;
            var response = Query("/authinfo", null, WebRequestMethods.Http.Get, typeof(AuthInfo)).Result;
            _changeStatusAction?.Invoke(_status);
            if (!response.success)
            {
                return $"Failed to get Authentication Info with API Token {_apiToken}";
            }
            _authInfo = (AuthInfo)response.returnObject;
            _logIn = LogInStatus.Yes;
            return $"Authenticated as {_authInfo.UserName} (u{_authInfo.Id})";
        }

        /// <summary>
        /// Change  user vote.
        /// </summary>
        /// <param name="vn">VN which will be changed</param>
        /// <param name="vote">New vote value</param>
        /// <returns>Returns whether it was successful.</returns>
        public async Task<bool> ChangeVote(ListedVN vn, int? vote)
        {
            return await WrapQuery(async () =>
            {
                bool remove = !vote.HasValue;
                var userVn = vn.UserVN ?? new UserVN { UserId = CSettings.UserID, VNID = vn.VNID, Labels = [] };
                //only delete if no label (including voted) or note present
                var method = userVn.Labels.Any() || !string.IsNullOrWhiteSpace(userVn.ULNote) ? "PATCH" : "DELETE";
                var response = await Query($"/ulist/v{vn.VNID}", $"{{\"vote\":{(vote.HasValue ? vote.Value : "null")}}}", method, null);
                if (!response.success) return false;
                userVn.Vote = vote;
                if (remove) userVn.Labels.Remove(UserVN.LabelKind.Voted);
                else userVn.Labels.Add(UserVN.LabelKind.Voted);
                userVn.VoteAdded = remove ? null : DateTime.UtcNow;
                if (userVn.Labels.Any() || !string.IsNullOrWhiteSpace(userVn.ULNote))
                    LocalDatabase.UserVisualNovels.Upsert(userVn, true);
                else LocalDatabase.UserVisualNovels.Remove(userVn, true);
                return true;
            });
        }

        /// <summary>
        /// Change labels of VN.
        /// </summary>
        /// <param name="vn">VN which will be changed</param>
        /// <param name="labels">New labels to set</param>
        /// <returns>Returns whether it as successful.</returns>
        public async Task<bool> ChangeVNStatus(ListedVN vn, HashSet<UserVN.LabelKind> labels)
        {
            return await WrapQuery(async () =>
            {
                var userVn = vn.UserVN ?? new UserVN { UserId = CSettings.UserID, VNID = vn.VNID, Added = DateTime.UtcNow };
                var jsonObject = $"{{\"labels\":{(labels.Any() ? $"[{string.Join(",", labels.Where(l => l != UserVN.LabelKind.Voted).Cast<int>())}]" : "null")}}}";
                //only delete if no label (including voted) or note present
                var method = labels.Any() || !string.IsNullOrWhiteSpace(userVn.ULNote) ? "PATCH" : "DELETE";
                var result = await Query($"/ulist/v{vn.VNID}", jsonObject, method, null);
                if (!result.success) return false;
                userVn.Labels = labels.ToHashSet();
                userVn.LastModified = DateTime.UtcNow;
                if (userVn.Labels.Any() || !string.IsNullOrWhiteSpace(userVn.ULNote)) LocalDatabase.UserVisualNovels.Upsert(userVn, true);
                else LocalDatabase.UserVisualNovels.Remove(userVn, true);
                return true;
            });
        }

        public async Task<bool> ChangeVNNote(ListedVN vn, string note)
        {
            return await WrapQuery(async () =>
            {
                var userVn = vn.UserVN ?? new UserVN { UserId = CSettings.UserID, VNID = vn.VNID, Added = DateTime.UtcNow, Labels = [] };
                //escape note string
                var jsonObject = $"{{\"notes\":{(!string.IsNullOrWhiteSpace(note) ? $"\"{JsonConvert.ToString(note)}\"" : "null")}}}";
                //only delete if no label (including voted) or note present
                var method = userVn.Labels.Any() || !string.IsNullOrWhiteSpace(userVn.ULNote) ? "PATCH" : "DELETE";
                var result = await Query($"/ulist/v{vn.VNID}", jsonObject, method, null);
                if (!result.success) return false;
                userVn.ULNote = note;
                userVn.LastModified = DateTime.UtcNow;
                if (userVn.Labels.Any() || !string.IsNullOrWhiteSpace(userVn.ULNote)) LocalDatabase.UserVisualNovels.Upsert(userVn, true);
                else LocalDatabase.UserVisualNovels.Remove(userVn, true);
                return true;
            });
        }
        #endregion

        /// <summary>
        /// Sets status to busy beforehand, runs task, then updates UI with status set in <see cref="Query"/> in finally block.
        /// </summary>
        /// <remarks>Callers do not need to modify status or update UI status.</remarks>
        private async Task<T> WrapQuery<T>(Func<Task<T>> task, [CallerMemberName] string caller = null)
        {
            if (!StartQuery(caller)) return default;
            try
            {
                _status = ApiStatus.Busy;
                _changeStatusAction?.Invoke(_status);
                return await task();
            }
            catch (Exception ex)
            {
                ActiveQuery.SetException(ex);
                return default;
            }
            finally
            {
                ActiveQuery.Completed = true;
                _textAction(ActiveQuery.CompletedMessage, ActiveQuery.CompletedMessageSeverity);
                _changeStatusAction?.Invoke(_status);
            }
        }

        /// <summary>
        /// Check if API Connection is ready, change status accordingly and write error if it isn't ready.
        /// </summary>
        /// <param name="featureName">Name of feature calling the query</param>
        /// <returns>If connection was ready</returns>
        private bool StartQuery(string featureName)
        {
            if (_logIn != LogInStatus.Yes)
            {
                _textAction("Must be authenticated with API token.", MessageSeverity.Error);
                return false;
            }
            if (ActiveQuery != null && !ActiveQuery.Completed)
            {
                _textAction($"Wait until {ActiveQuery.ActionName} is done.", MessageSeverity.Error);
                return false;
            }
            ActiveQuery = new ApiQuery(featureName);
            _textAction($"Running {featureName}...", MessageSeverity.Normal);
            return true;
        }

        private async Task<(bool success, object returnObject)> Query(string uri, string jsonObject, string method, Type typeOfReturnObject)
        {
            var url = $"https://{VndbHost}{uri}";
            Logger.ToFile($"VNDB API: {method} - {uri} - {jsonObject}");
            var request = WebRequest.Create(url);
            request.Method = method;
            if (jsonObject != null)
            {
                request.ContentType = "application/json";
                using var requestStream = await request.GetRequestStreamAsync();
                using var streamWriter = new StreamWriter(requestStream);
                await streamWriter.WriteAsync(jsonObject);
                streamWriter.Close();
            }
            request.Headers.Add(HttpRequestHeader.Authorization, $"token {_apiToken}");
            try
            {
                var response = await request.GetResponseAsync();
                using var stream = response.GetResponseStream()!;
                using var streamReader = new StreamReader(stream);
                var responseText = await streamReader.ReadToEndAsync();
                object returnObject = null;
                if (typeOfReturnObject != null)
                {
                    returnObject = JsonConvert.DeserializeObject(responseText, typeOfReturnObject);
                }
                _status = ApiStatus.Ready;
                return (true, returnObject);
            }
            catch (Exception ex)
            {
                Logger.ToFile(ex);
                ActiveQuery.SetException(ex);
                _status = ApiStatus.Error;
                return (false, null);
            }
        }
    }
}