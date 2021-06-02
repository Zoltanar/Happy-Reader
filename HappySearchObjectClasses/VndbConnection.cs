using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Happy_Apps_Core.Database;
using Happy_Apps_Core.Properties;
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
		private const string VndbHost = "api.vndb.org";
		private const ushort VndbPort = 19534;
		private const ushort VndbPortTls = 19535;
		private const byte EndOfStreamByte = 0x04;
		/// <summary>
		/// string is text to be passed, bool is true if query, false if response
		/// </summary>
		private readonly Action<string, bool> _advancedAction;
		private readonly Action<APIStatus> _changeStatusAction;
		[NotNull] private readonly Action<string, MessageSeverity> _textAction;
		[NotNull] private readonly Func<bool> _askForNonSslAction;
		private int _throttleWaitTime;
		private Stream _stream;
		private TcpClient _tcpClient;
		private Response _lastResponse;
		private LogInStatus _logIn = LogInStatus.No;
		private APIStatus _status = APIStatus.Closed;
		private LoginCredentials _loginCredentials;
		public ApiQuery ActiveQuery { get; private set; }

		public VndbConnection(
			[NotNull] Action<string, MessageSeverity> textAction,
			Action<string, bool> advancedModeAction,
			Func<bool> askForNonSsl,
			Action<APIStatus> changeStatusAction = null)
		{
			_textAction = textAction;
			_advancedAction = advancedModeAction;
			_askForNonSslAction = askForNonSsl;
			_changeStatusAction = changeStatusAction;
		}

		#region Public Functions
		/// <summary>
		/// Log into VNDB API, optionally using username/password.
		/// </summary>
		/// <param name="loginCredentials">Credentials to use for login command</param>
		/// <param name="printCertificates">Logs certificates and prints to debug</param>
		public string Login(LoginCredentials loginCredentials, bool printCertificates)
		{
			if (_status != APIStatus.Closed) Close();
			_logIn = LogInStatus.No;
			Open(printCertificates);
			string loginBuffer = $"login {{\"protocol\":1,\"client\":\"{loginCredentials.ClientName}\",\"clientver\":\"{loginCredentials.ClientVersion}\"{loginCredentials.CredentialsString}}}";
			Query(loginBuffer);
			if (_lastResponse.Type == ResponseType.Ok)
			{
				_logIn = loginCredentials.HasCredentials ? LogInStatus.YesWithPassword : LogInStatus.Yes;
				_status = APIStatus.Ready;
			}
			_loginCredentials = loginCredentials;
			_changeStatusAction?.Invoke(_status);

			return $"{(_logIn == LogInStatus.No ? "Failed to log in." : "Log in successful.")} {_lastResponse.JsonPayload}";
		}

		/// <summary>
		/// Change  user vote.
		/// </summary>
		/// <param name="vn">VN which will be changed</param>
		/// <param name="vote">New vote value</param>
		/// <returns>Returns whether it as successful.</returns>
		public async Task<bool> ChangeVote(ListedVN vn, int? vote)
		{
			return await WrapQuery(async () =>
			{
				bool remove = !vote.HasValue;
				_changeStatusAction?.Invoke(APIStatus.Busy);
				var userVn = vn.UserVN ?? new UserVN { UserId = CSettings.UserID, VNID = vn.VNID };
				var queryString = $"set ulist {vn.VNID} {{\"vote\":{vote}}}";
				if (!await TryQuery(queryString, Resources.cvns_query_error)) return false;
				userVn.Vote = vote;
				if (remove) userVn.Labels.Remove(UserVN.LabelKind.Voted);
				else userVn.Labels.Add(UserVN.LabelKind.Voted);
				userVn.VoteAdded = remove ? null : DateTime.UtcNow;
				if (userVn.Labels.Any()) LocalDatabase.UserVisualNovels.Upsert(userVn, true);
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
				_changeStatusAction?.Invoke(APIStatus.Busy);
				var userVn = vn.UserVN ?? new UserVN { UserId = CSettings.UserID, VNID = vn.VNID };
				var queryString = $"set ulist {vn.VNID} {{\"labels\":[{string.Join(",", labels.Cast<int>())}]}}";
				if (!await TryQuery(queryString, Resources.cvns_query_error)) return false;
				userVn.Labels = labels.ToHashSet();
				userVn.Added = DateTime.UtcNow;
				if (userVn.Labels.Any()) LocalDatabase.UserVisualNovels.Upsert(userVn, true);
				else LocalDatabase.UserVisualNovels.Remove(userVn, true);
				return true;
			});
		}

		/// <summary>
		/// Get username from VNDB user ID, returns empty string if error.
		/// </summary>
		public async Task<string> GetUsernameFromID(int userID)
		{
			return await WrapQuery(async () =>
			{
				if (!await TryQueryNoReply($"get user basic (id={userID})")) return string.Empty;
				var response = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(_lastResponse.JsonPayload);
				return response?.Items.Any() ?? false ? response.Items[0].Username : string.Empty;
			}, string.Empty);
		}

		/// <summary>
		/// Get user ID from VNDB username, returns -1 if error.
		/// </summary>
		public async Task<int> GetIDFromUsername(string username)
		{
			return await WrapQuery(async () =>
			{
				if (!await TryQueryNoReply($"get user basic (username=\"{username}\")")) return -1;
				var response = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(_lastResponse.JsonPayload);
				return response?.Items.Any() ?? false ?  response.Items[0].ID : -1;
			}, -1);
		}

		/// <summary>
		/// Sends a query to the API without waiting on throttle error.
		/// </summary>
		public void SendQuery(string text) => Query(text);

		/// <summary>
		/// Close connection with VNDB API
		/// </summary>
		public void Close()
		{
			try
			{
				if (_tcpClient.Connected) _tcpClient.GetStream().Close();
				_tcpClient.Close();
			}
			catch (ObjectDisposedException e)
			{
				Logger.ToFile("Failed to close connection.");
				Logger.ToFile(e.Message);
				Logger.ToFile(e.StackTrace);
			}
			_status = APIStatus.Closed;
		}
		#endregion

		/// <summary>
		/// Open stream with VNDB API.
		/// </summary>
		private void Open(bool printCertificates)
		{
			Logger.ToFile($"Attempting to open connection to {VndbHost}:{VndbPortTls}");
			var attempts = 0;
			var certs = GetCertificates(printCertificates);
			while (attempts < 5)
			{
				try
				{
					attempts++;
					Logger.ToFile($"Attempt number {attempts}...");
					_tcpClient = new TcpClient();
					_tcpClient.Connect(VndbHost, VndbPortTls);
					Logger.ToFile("TCP Client connection made...");
					var sslStream = new SslStream(_tcpClient.GetStream());
					Logger.ToFile("SSL Stream received...");
					sslStream.AuthenticateAsClient(VndbHost, certs, SslProtocols.Tls12, true);
					Logger.ToFile("SSL Stream authenticated...");
					if (!CheckRemoteCertificate(printCertificates, sslStream.RemoteCertificate)) return;
					_stream = sslStream;
					Logger.ToFile($"Connected after {attempts} attempts.");
					break;
				}
				catch (SocketException e)
				{
					Logger.ToFile(e);
					Thread.Sleep(1000);
				}
				catch (Exception ex)
				{
					Logger.ToFile(ex);
					break;
				}
			}
			if (_stream != null && _stream.CanRead) return;
			Logger.ToFile($"Failed to connect after {attempts} attempts.");
			_status = APIStatus.Error;
			AskForNonSsl();
		}

		private bool CheckRemoteCertificate(bool printCertificates, X509Certificate remoteCertificate)
		{
			if (remoteCertificate == null) return true;
			if (printCertificates)
			{
				Logger.ToFile("Remote Certificate data - subject/issuer/format/effectivedate/expirationdate", GetDetails(remoteCertificate));
			}
			var certificateTarget = remoteCertificate.Subject.Substring(3);
			if (certificateTarget.Equals(VndbHost)) return true;
			Logger.ToFile($"Certificate received isn't for {VndbHost} so connection is closed (it was for {certificateTarget})");
			_status = APIStatus.Error;
			return false;
		}

		private static X509CertificateCollection GetCertificates(bool printCertificates)
		{
			var certs = new X509CertificateCollection();
			var certFiles = Directory.GetFiles(CertificatesFolder);
			foreach (var certFile in certFiles) certs.Add(X509Certificate.CreateFromCertFile(certFile));
			if (!printCertificates) return certs;
			Logger.ToFile("Local Certificate data - subject/issuer/format/effectivedate/expirationdate");
			foreach (var cert in certs) Logger.ToFile(GetDetails(cert));

			return certs;
		}

		private static string GetDetails(X509Certificate cert)
			=> $"{cert.Subject}\t{cert.Issuer}\t{cert.GetFormat()}\t{cert.GetEffectiveDateString()}\t{cert.GetExpirationDateString()}";

		private void AskForNonSsl()
		{
			if (!_askForNonSslAction()) return;
			Logger.ToFile($"Attempting to open connection to {VndbHost}:{VndbPort} without SSL");
			_status = APIStatus.Closed;
			var complete = false;
			var attempts = 0;
			while (!complete && attempts < 5)
			{
				try
				{
					attempts++;
					Logger.ToFile($"Attempt number {attempts}...");
					_tcpClient = new TcpClient();
					_tcpClient.Connect(VndbHost, VndbPort);
					Logger.ToFile("TCP Client connection made...");
					_stream = _tcpClient.GetStream();
					Logger.ToFile("Stream received...");
					Logger.ToFile($"Connected after {attempts} attempts.");
					complete = true;
				}
				catch (IOException e)
				{
					Logger.ToFile(e);
				}
			}
			if (_stream != null && _stream.CanRead) return;
			Logger.ToFile($"Failed to connect after {attempts} attempts.");
			_status = APIStatus.Error;
		}

		private async Task<T> WrapQuery<T>(Func<Task<T>> task, T failValue = default, [CallerMemberName] string caller = null)
		{
			if (!StartQuery(caller)) return failValue;
			try
			{
				return await task();
			}
			catch (Exception ex)
			{
				ActiveQuery.SetException(ex);
				return failValue;
			}
			finally
			{
				EndQuery();
			}
		}

		/// <summary>
		/// Check if API Connection is ready, change status accordingly and write error if it isn't ready.
		/// </summary>
		/// <param name="featureName">Name of feature calling the query</param>
		/// <returns>If connection was ready</returns>
		private bool StartQuery(string featureName)
		{
			if (CSettings.UserID < 1) return false;
			if (ActiveQuery != null && !ActiveQuery.Completed)
			{
				_textAction($"Wait until {ActiveQuery.ActionName} is done.", MessageSeverity.Error);
				return false;
			}
			ActiveQuery = new ApiQuery(featureName);
			_textAction($"Running {featureName}...", MessageSeverity.Normal);
			return true;
		}

		private void EndQuery()
		{
			ActiveQuery.Completed = true;
			_textAction(ActiveQuery.CompletedMessage, ActiveQuery.CompletedMessageSeverity);
			_changeStatusAction?.Invoke(_status);
		}

		private void Query(string command)
		{
			if (_status == APIStatus.Error) return;
			LogQueryRequest(command);
			_status = APIStatus.Busy;
			byte[] encoded = Encoding.UTF8.GetBytes(command);
			var requestBuffer = new byte[encoded.Length + 1];
			Buffer.BlockCopy(encoded, 0, requestBuffer, 0, encoded.Length);
			requestBuffer[encoded.Length] = EndOfStreamByte;
			_stream.Write(requestBuffer, 0, requestBuffer.Length);
			var responseBuffer = new byte[4096];
			var totalRead = 0;
			while (true)
			{
				var currentRead = _stream.Read(responseBuffer, totalRead, responseBuffer.Length - totalRead);
				if (currentRead == 0) throw new Exception("Connection closed while reading login response");
				totalRead += currentRead;
				if (IsCompleteMessage(responseBuffer, totalRead)) break;
				if (totalRead != responseBuffer.Length) continue;
				var biggerBadderBuffer = new byte[responseBuffer.Length * 2];
				Buffer.BlockCopy(responseBuffer, 0, biggerBadderBuffer, 0, responseBuffer.Length);
				responseBuffer = biggerBadderBuffer;
			}
			_lastResponse = Parse(responseBuffer, totalRead);
			_advancedAction?.Invoke(_lastResponse.JsonPayload, false);
			SetStatusFromLastResponseType();
		}

		private void LogQueryRequest(string command)
		{
			if (_advancedAction == null) return;
			if (command.StartsWith("login") && command.Contains("password"))
			{
				try
				{
					var jsonString = command.Substring("login".Length).Trim().Replace("\\\"", "\"");
					var jObject = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(jsonString);
					Debug.Assert(jObject != null, nameof(jObject) + " != null");
					jObject["password"] = "***";
					jsonString = JsonConvert.SerializeObject(jObject).Replace("\"", "\\\"");
					_advancedAction.Invoke($"login {jsonString}", true);
					return;
				}
				catch (Exception ex)
				{
					Logger.ToFile(ex.Message, "Failed to hide password in login request.");
				}
			}
			_advancedAction.Invoke(command, true);
		}

		private async Task QueryAsync(string query)
		{
			byte[] encoded = Encoding.UTF8.GetBytes(query);
			var requestBuffer = new byte[encoded.Length + 1];
			Buffer.BlockCopy(encoded, 0, requestBuffer, 0, encoded.Length);
			requestBuffer[encoded.Length] = EndOfStreamByte;
			await _stream.WriteAsync(requestBuffer, 0, requestBuffer.Length);
			var responseBuffer = new byte[4096];
			var totalRead = 0;
			while (true)
			{
				var currentRead = await _stream.ReadAsync(responseBuffer, totalRead, responseBuffer.Length - totalRead);
				if (currentRead == 0) throw new Exception("Connection closed while reading login response");
				totalRead += currentRead;
				if (IsCompleteMessage(responseBuffer, totalRead)) break;
				if (totalRead != responseBuffer.Length) continue;
				var biggerBadderBuffer = new byte[responseBuffer.Length * 2];
				Buffer.BlockCopy(responseBuffer, 0, biggerBadderBuffer, 0, responseBuffer.Length);
				responseBuffer = biggerBadderBuffer;
			}
			_lastResponse = Parse(responseBuffer, totalRead);
			SetStatusFromLastResponseType();
		}

		private void SetStatusFromLastResponseType()
		{
			switch (_lastResponse.Type)
			{
				case ResponseType.Ok:
				case ResponseType.Results:
				case ResponseType.DBStats:
					_status = APIStatus.Ready;
					break;
				case ResponseType.Error:
					_status = _lastResponse.Error.ID.Equals("throttled") ? APIStatus.Throttled : APIStatus.Ready;
					break;
				case ResponseType.Unknown:
					_status = APIStatus.Error;
					break;
			}
		}

		private static bool IsCompleteMessage(byte[] message, int bytesUsed)
		{
			if (bytesUsed == 0) throw new Exception("You have a bug, dummy. You should have at least one byte here.");
			// ASSUMPTION: simple request-response protocol, so we should see at most one message in a given byte array.
			// So, there's no need to walk the whole array looking for validity - just be lazy and check the last byte for EOS.
			return message[bytesUsed - 1] == EndOfStreamByte;
		}

		private static Response Parse(byte[] message, int bytesUsed)
		{
			if (!IsCompleteMessage(message, bytesUsed)) throw new Exception("You have a bug, dummy.");
			var stringResponse = Encoding.UTF8.GetString(message, 0, bytesUsed - 1);
			var firstSpace = stringResponse.IndexOf(' ');
			var firstWord = firstSpace != -1 ? stringResponse.Substring(0, firstSpace) : stringResponse;
			var payload = firstSpace > 0 ? stringResponse.Substring(firstSpace) : "";
			if (firstSpace == bytesUsed - 1) throw new Exception("Protocol violation: last character in response is first space");
			return firstWord switch
			{
				"ok" => new Response(ResponseType.Ok, payload),
				"results" => new Response(ResponseType.Results, payload),
				"dbstats" => new Response(ResponseType.DBStats, payload),
				"error" => new Response(ResponseType.Error, payload),
				_ => new Response(ResponseType.Unknown, payload)
			};
		}

		/// <summary>
		/// Send query through API Connection.
		/// </summary>
		/// <param name="query">Command to be sent</param>
		/// <param name="errorMessage">Message to be printed in case of error</param>
		/// <returns>Returns whether it was successful.</returns>
		private async Task<QueryResult> TryQueryInner(string query, string errorMessage)
		{
			if (_status != APIStatus.Ready)
			{
				ActiveQuery.CompletedMessage = "API Connection isn't ready.";
				ActiveQuery.CompletedMessageSeverity = MessageSeverity.Error;
				return QueryResult.Fail;
			}
			_status = APIStatus.Busy;
			_changeStatusAction?.Invoke(_status);
			await RunQueryWithRetriesAsync(query);
			if (_lastResponse.Type == ResponseType.Unknown) return QueryResult.Fail;
			while (_lastResponse.Type == ResponseType.Error)
			{
				return _lastResponse.Error.ID.Equals("throttled") ? HandleThrottledResponse() : HandleFailResponse(errorMessage);
			}
			return QueryResult.Success;
		}

		private async Task RunQueryWithRetriesAsync(string query)
		{
			await Task.Run(() =>
			{
				Logger.ToFile(query);
				RunWithRetries(() => Query(query), () => Login(_loginCredentials, false), 5, ex =>
				{
					SocketException socketException;
					switch (ex)
					{
						case IOException ioException:
							if (ioException.InnerException is SocketException innerException) socketException = innerException;
							else return false;
							break;
						case SocketException sockException:
							socketException = sockException;
							break;
						default: return false;
					}
					//if error has code 10054, we try again.
					return socketException.ErrorCode == 10054;
				});
			});
		}

		private QueryResult HandleFailResponse(string errorMessage)
		{
			Logger.ToFile($"{nameof(TryQueryInner)} error: {_lastResponse.JsonPayload}");
			ActiveQuery.CompletedMessage = errorMessage;
			ActiveQuery.CompletedMessageSeverity = MessageSeverity.Error;
			return QueryResult.Fail;
		}

		private QueryResult HandleThrottledResponse()
		{
			var minWait = Math.Min(5 * 60, _lastResponse.Error.Fullwait); //wait 5 minutes
			var throttleMessage = $"Throttled for {Math.Floor(minWait / 60)} mins.";
			_textAction(throttleMessage, MessageSeverity.Warning);
			Logger.ToFile($"Local: {DateTime.Now} - {throttleMessage}");
			var waitMs = minWait * 1000;
			_throttleWaitTime = Convert.ToInt32(waitMs);
			return QueryResult.Throttled;
		}

		/// <summary>
		/// Send query through API Connection.
		/// </summary>
		/// <param name="query">Command to be sent</param>
		/// <param name="errorMessage">Message to be printed in case of error</param>
		/// <param name="setStatusOnEnd">Set to true to change status when ending this query (Only for direct calls)</param>
		/// <returns>Returns whether it was successful.</returns>
		private async Task<bool> TryQuery(string query, string errorMessage, bool setStatusOnEnd = false)
		{
			var result = await TryQueryInner(query, errorMessage);
			while (result == QueryResult.Throttled)
			{
				_changeStatusAction?.Invoke(_status);
				await Task.Delay(_throttleWaitTime);
				_status = APIStatus.Ready;
				result = await TryQueryInner(query, errorMessage);
			}
			if (setStatusOnEnd) _changeStatusAction?.Invoke(_status);
			return result == QueryResult.Success;
		}

		/// <summary>
		/// Send query through API Connection, don't post error messages back.
		/// </summary>
		/// <param name="query">Command to be sent</param>
		/// <returns>Returns whether it was successful.</returns>
		private async Task<bool> TryQueryNoReply(string query)
		{
			if (_status != APIStatus.Ready)
			{
				return false;
			}
			await Task.Run(() =>
			{
				Logger.ToFile(query);
				Query(query);
			});
			return _lastResponse.Type != ResponseType.Unknown && _lastResponse.Type != ResponseType.Error;
		}

	}
}