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
		/// <returns>If connection was ready</returns>
		internal bool StartQuery(string featureName, bool refreshList, bool additionalMessage)
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
		
		#region Public Functions
		/// <summary>
		/// Change  user vote.
		/// </summary>
		/// <param name="vn">VN which will be changed</param>
		/// <param name="vote">New vote value</param>
		/// <returns>Returns whether it as successful.</returns>
		public async Task<bool> ChangeVote(ListedVN vn, int? vote)
		{
			if (!StartQuery(nameof(ChangeVote), false, false)) return false;
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
			if (!StartQuery(nameof(ChangeVNStatus), false, false)) return false;
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
			if (!StartQuery(nameof(GetUsernameFromID), false, false)) return "";
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
			if (!StartQuery(nameof(GetIDFromUsername), false, false)) return -1;
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
		#endregion
	}
}
