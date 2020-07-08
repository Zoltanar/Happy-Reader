using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core
{
	/// <summary>
	/// Class for establishing connection with VNDB API and interacting with it.
	/// </summary>
	public partial class VndbConnection
	{
		public const int VndbAPIMaxYear = 99999999;
		private const string VndbHost = "api.vndb.org";
		private const ushort VndbPort = 19534;
		private const ushort VndbPortTLS = 19535;
		private const byte EndOfStreamByte = 0x04;
		private int _throttleWaitTime;
		private Stream _stream;
		private TcpClient _tcpClient;
		public Response LastResponse;
		public LogInStatus LogIn = LogInStatus.No;
		public APIStatus Status = APIStatus.Closed;
		private LoginCredentials _loginCredentials;

		/// <summary>
		/// Open stream with VNDB API.
		/// </summary>
		private void Open(bool printCertificates)
		{
			Logger.ToFile($"Attempting to open connection to {VndbHost}:{VndbPortTLS}");
			var complete = false;
			var retries = 0;
			var certs = new X509CertificateCollection();
			var certFiles = Directory.GetFiles("Program Data\\Certificates");
			foreach (var certFile in certFiles) certs.Add(X509Certificate.CreateFromCertFile(certFile));
#if !DONTPRINTCERTS
			if (printCertificates)
			{
				Logger.ToFile("Local Certificate data - subject/issuer/format/effectivedate/expirationdate");
				foreach (var cert in certs) Logger.ToFile($"{cert.Subject}\t{cert.Issuer}\t{cert.GetFormat()}\t{cert.GetEffectiveDateString()}\t{cert.GetExpirationDateString()}");
			}
#endif
			while (!complete && retries < 5)
			{
				try
				{
					retries++;
					Logger.ToFile($"Trying for the {retries}'th time...");
					_tcpClient = new TcpClient();
					_tcpClient.Connect(VndbHost, VndbPortTLS);
					Logger.ToFile("TCP Client connection made...");
					var sslStream = new SslStream(_tcpClient.GetStream());
					Logger.ToFile("SSL Stream received...");
					sslStream.AuthenticateAsClient(VndbHost, certs, SslProtocols.Tls12, true);
					Logger.ToFile("SSL Stream authenticated...");
					if (sslStream.RemoteCertificate != null)
					{
						var subject = sslStream.RemoteCertificate.Subject;
						if (printCertificates)
						{
							Logger.ToFile("Remote Certificate data - subject/issuer/format/effectivedate/expirationdate",
								$"{subject}\t-{sslStream.RemoteCertificate.Issuer}\t-{sslStream.RemoteCertificate.GetFormat()}\t-{sslStream.RemoteCertificate.GetEffectiveDateString()}\t-{sslStream.RemoteCertificate.GetExpirationDateString()}");
						}
						if (!subject.Substring(3).Equals(VndbHost))
						{
							Logger.ToFile(
								$"Certificate received isn't for {VndbHost} so connection is closed (it was for {subject.Substring(3)})");
							Status = APIStatus.Error;
							return;
						}
					}

					_stream = sslStream;
					complete = true;
					Logger.ToFile($"Connected after {retries} tries.");
				}
				catch (SocketException e)
				{
					Logger.ToFile(e, "Conn Socket Error");
					if (e.InnerException == null) continue;
					Logger.ToFile(e, "Conn Socket Error - Inner");
					Thread.Sleep(1000);
				}
				catch (IOException e)
				{
					Logger.ToFile(e, "Conn Open Error");
				}
				catch (AuthenticationException e)
				{
					Logger.ToFile(e, "Conn Authentication Error");
					if (e.InnerException == null) continue;
					Logger.ToFile(e, "Conn Authentication Error - Inner");
				}
				catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
				{
					Logger.ToFile(ex, "Conn Other Error");
				}
			}
			if (_stream != null && _stream.CanRead) return;
			Logger.ToFile($"Failed to connect after {retries} tries.");
			Status = APIStatus.Error;
			AskForNonSsl();
		}

		private void AskForNonSsl()
		{
			if (!AskForNonSslAction()) return;
			Logger.ToFile($"Attempting to open connection to {VndbHost}:{VndbPort} without SSL");
			Status = APIStatus.Closed;
			var complete = false;
			var retries = 0;
			while (!complete && retries < 5)
			{
				try
				{
					retries++;
					Logger.ToFile($"Trying for the {retries}'th time...");
					_tcpClient = new TcpClient();
					_tcpClient.Connect(VndbHost, VndbPort);
					Logger.ToFile("TCP Client connection made...");
					_stream = _tcpClient.GetStream();
					Logger.ToFile("Stream received...");
					Logger.ToFile($"Connected after {retries} tries.");
					complete = true;
				}
				catch (IOException e)
				{
					Logger.ToFile(e, "Conn Open Error");
				}
				catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
				{
					Logger.ToFile(ex, "Conn Other Error");
				}
				catch (Exception otherXException)
				{
					Logger.ToFile(otherXException, "Conn Other2 Error");
				}
			}
			if (_stream != null && _stream.CanRead) return;
			Logger.ToFile($"Failed to connect after {retries} tries.");
			Status = APIStatus.Error;
		}

		/// <summary>
		/// Log into VNDB API, optionally using username/password.
		/// </summary>
		/// <param name="loginCredentials">Credentials to use for login command</param>
		/// <param name="printCertificates">Default is true, logs certificates and prints to debug</param>
		public void Login(LoginCredentials loginCredentials, bool printCertificates = true)
		{
			if (Status != APIStatus.Closed) Close();
			Open(printCertificates);
			string loginBuffer;

			if (loginCredentials.Username != null && loginCredentials.Password != null)
			{
				loginBuffer =
					$"login {{\"protocol\":1,\"client\":\"{loginCredentials.ClientName}\",\"clientver\":\"{loginCredentials.ClientVersion}\",\"username\":\"{loginCredentials.Username}\",\"password\":\"{new string(loginCredentials.Password)}\"}}";
				Query(loginBuffer);
				if (LastResponse.Type == ResponseType.Ok)
				{
					LogIn = LogInStatus.YesWithPassword;
					Status = APIStatus.Ready;
				}
			}
			else
			{
				loginBuffer = $"login {{\"protocol\":1,\"client\":\"{loginCredentials.ClientName}\",\"clientver\":\"{loginCredentials.ClientVersion}\"}}";
				Query(loginBuffer);
				if (LastResponse.Type == ResponseType.Ok)
				{
					LogIn = LogInStatus.Yes;
					Status = APIStatus.Ready;
				}
			}

			_loginCredentials = loginCredentials;
			_changeStatusAction?.Invoke(Status);
		}

		public readonly struct LoginCredentials
		{
			public string ClientName { get; }
			public string ClientVersion { get; }
			public string Username { get; }
			public char[] Password { get; }

			public LoginCredentials(string clientName, string clientVersion, string username = null, char[] password = null)
			{
				ClientName = clientName;
				ClientVersion = clientVersion;
				Username = username;
				Password = password;
			}
		}

		private void Query(string command)
		{
			if (Status == APIStatus.Error) return;
			_advancedAction?.Invoke(command, true);
			Status = APIStatus.Busy;
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
			LastResponse = Parse(responseBuffer, totalRead);
			_advancedAction?.Invoke(LastResponse.JsonPayload, false);
			SetStatusFromLastResponseType();
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
			LastResponse = Parse(responseBuffer, totalRead);
			SetStatusFromLastResponseType();
		}

		private void SetStatusFromLastResponseType()
		{
			switch (LastResponse.Type)
			{
				case ResponseType.Ok:
				case ResponseType.Results:
				case ResponseType.DBStats:
					Status = APIStatus.Ready;
					break;
				case ResponseType.Error:
					Status = LastResponse.Error.ID.Equals("throttled") ? APIStatus.Throttled : APIStatus.Ready;
					break;
				case ResponseType.Unknown:
					Status = APIStatus.Error;
					break;
			}
		}

		/// <summary>
		/// Close connection with VNDB API
		/// </summary>
		public void Close()
		{
			try
			{
				if(_tcpClient.Connected) _tcpClient.GetStream().Close();
				_tcpClient.Close();
			}
			catch (ObjectDisposedException e)
			{
				Logger.ToFile("Failed to close connection.");
				Logger.ToFile(e.Message);
				Logger.ToFile(e.StackTrace);
			}
			Status = APIStatus.Closed;
		}

		private static bool IsCompleteMessage(byte[] message, int bytesUsed)
		{
			if (bytesUsed == 0)
			{
				throw new Exception("You have a bug, dummy. You should have at least one byte here.");
			}

			// ASSUMPTION: simple request-response protocol, so we should see at most one message in a given byte array.
			// So, there's no need to walk the whole array looking for validity - just be lazy and check the last byte for EOS.
			return message[bytesUsed - 1] == EndOfStreamByte;
		}

		private static Response Parse(byte[] message, int bytesUsed)
		{
			if (!IsCompleteMessage(message, bytesUsed))
			{
				throw new Exception("You have a bug, dummy.");
			}

			var stringifiedResponse = Encoding.UTF8.GetString(message, 0, bytesUsed - 1);
			var firstSpace = stringifiedResponse.IndexOf(' ');
			var firstWord = firstSpace != -1 ? stringifiedResponse.Substring(0, firstSpace) : stringifiedResponse;
			var payload = firstSpace > 0 ? stringifiedResponse.Substring(firstSpace) : "";
			if (firstSpace == bytesUsed - 1)
			{
				// protocol violation!
				throw new Exception("Protocol violation: last character in response is first space");
			}
			switch (firstWord)
			{
				case "ok":
					return new Response(ResponseType.Ok, payload);
				case "results":
					return new Response(ResponseType.Results, payload);
				case "dbstats":
					return new Response(ResponseType.DBStats, payload);
				case "error":
					return new Response(ResponseType.Error, payload);
				default:
					return new Response(ResponseType.Unknown, payload);
			}
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
				ActiveQuery.CompletedMessage = "API Connection isn't ready.";
				ActiveQuery.CompletedMessageSeverity = MessageSeverity.Error;
				return QueryResult.Fail;
			}
			Status = APIStatus.Busy;
			_changeStatusAction?.Invoke(Status);
			await Task.Run(() =>
			{
				if (CSettings.DecadeLimit && (!ActiveQuery?.IgnoreDateLimit ?? false) && query.StartsWith("get vn ") && !query.Contains("id = "))
				{
					query = Regex.Replace(query, "\\)", $" and released > \"{DateTime.UtcNow.Year - 10}\")");
				}
				Logger.ToFile(query);
				RunWithRetries(() => Query(query),()=> Login(_loginCredentials), 5, ex =>
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

					return socketException.ErrorCode == 10054;
				});
			});
			if (LastResponse.Type == ResponseType.Unknown)
			{
				return QueryResult.Fail;
			}
			while (LastResponse.Type == ResponseType.Error)
			{
				if (!LastResponse.Error.ID.Equals("throttled"))
				{
					Logger.ToFile($"{nameof(TryQueryInner)} error: {LastResponse.JsonPayload}");
					ActiveQuery.CompletedMessage = errorMessage;
					ActiveQuery.CompletedMessageSeverity = MessageSeverity.Error;
					return QueryResult.Fail;
				}
				string fullThrottleMessage = "";
				double minWait = 0;
				await Task.Run(() =>
				{
					minWait = Math.Min(5 * 60, LastResponse.Error.Fullwait); //wait 5 minutes
					string normalWarning = $"Throttled for {Math.Floor(minWait / 60)} mins.";
					string additionalWarning = "";
					if (ActiveQuery.TitlesAdded.Count > 0) additionalWarning += $" Added {ActiveQuery.TitlesAdded.Count}.";
					if (ActiveQuery.TitlesSkipped.Count > 0) additionalWarning += $" Skipped {ActiveQuery.TitlesSkipped.Count}.";
					fullThrottleMessage = ActiveQuery.AdditionalMessage ? normalWarning + additionalWarning : normalWarning;
				});
				TextAction(fullThrottleMessage, MessageSeverity.Warning);
				Logger.ToFile($"Local: {DateTime.Now} - {fullThrottleMessage}");
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
		private async Task<bool> TryQuery(string query, string errorMessage, bool setStatusOnEnd = false)
		{
			var result = await TryQueryInner(query, errorMessage);
			while (result == QueryResult.Throttled)
			{
				if (ActiveQuery.RefreshList)
				{
					await Task.Run(ActiveQuery.RunActionOnAdd);
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
		/// Send query through API Connection, don't post error messages back.
		/// </summary>
		/// <param name="query">Command to be sent</param>
		/// <returns>Returns whether it was successful.</returns>
		private async Task<bool> TryQueryNoReply(string query)
		{
			if (Status != APIStatus.Ready)
			{
				return false;
			}
			await Task.Run(() =>
			{
				Logger.ToFile(query);
				Query(query);
			});
			return LastResponse.Type != ResponseType.Unknown && LastResponse.Type != ResponseType.Error;
		}

		public enum LogInStatus
		{
			No,
			Yes,
			YesWithPassword
		}

		public enum APIStatus
		{
			Ready,
			Busy,
			Throttled,
			Error,
			Closed
		}

		/// <summary>
		/// Holds API's response to commands.
		/// </summary>
		public class Response
		{
			/// <summary>
			/// If response is of type 'error', holds ErrorResponse
			/// </summary>
			public readonly ErrorResponse Error;
			/// <summary>
			/// Response in JSON format
			/// </summary>
			public readonly string JsonPayload;
			/// <summary>
			/// Type of response
			/// </summary>
			public readonly ResponseType Type;

			/// <summary>
			/// Constructor for Response
			/// </summary>
			/// <param name="type">Type of response</param>
			/// <param name="jsonPayload">Response in JSON format</param>
			public Response(ResponseType type, string jsonPayload)
			{
				Type = type;
				JsonPayload = jsonPayload;
				if (type == ResponseType.Error) Error = JsonConvert.DeserializeObject<ErrorResponse>(jsonPayload);
			}

		}

		/// <summary>
		/// Type of API Response
		/// </summary>
		public enum ResponseType
		{
			/// <summary>
			/// Returned by login command
			/// </summary>
			Ok,
			/// <summary>
			/// Returned by get commands 
			/// </summary>
			Results,
			/// <summary>
			/// Returned by dbstats command
			/// </summary>
			DBStats,
			/// <summary>
			/// Returned when there is an error
			/// </summary>
			Error,
			/// <summary>
			/// Returned in all other cases
			/// </summary>
			Unknown
		}

	}

}