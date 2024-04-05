using Happy_Apps_Core.API_Objects;
using Newtonsoft.Json;

namespace Happy_Apps_Core
{
	partial class VndbConnection
	{
		public enum MessageSeverity { Normal, Warning, Error }
		private enum QueryResult { Fail, Success, Throttled }
		private enum LogInStatus { No, Yes, YesWithPassword }
		public enum APIStatus
		{
			Ready,
			Busy,
			Throttled,
			Error,
			Closed
		}
		/// <summary>
		/// Type of API Response
		/// </summary>
		private enum ResponseType
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

		public class LoginCredentials
		{
			public string ClientName { get; }
			public string ClientVersion { get; }
			private string Username { get; }
            private char[] Password { get; }
            public string ApiToken { get; }
            public bool HasCredentials => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(new string(Password));
			public string CredentialsString => HasCredentials ? $",\"username\":\"{Username}\",\"password\":\"{new string(Password)}\"" : string.Empty;
            public AuthInfo AuthInfo { get; set; }

            public LoginCredentials(string clientName, string clientVersion, string username = null, char[] password = null, string apiToken = null)
			{
				ClientName = clientName;
				ClientVersion = clientVersion;
				Username = username;
                ApiToken = apiToken;
			}
		}

		/// <summary>
		/// Holds API's response to commands.
		/// </summary>
		private class Response
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
            /// Response in plain text
            /// </summary>
            public readonly string Message;
            /// <summary>
            /// Type of response
            /// </summary>
            public readonly ResponseType Type;

            /// <summary>
            /// Constructor for Response
            /// </summary>
            /// <param name="type">Type of response</param>
            /// <param name="jsonPayload">Response in JSON format</param>
            /// <param name="message">Response in plain text</param>
            public Response(ResponseType type, string jsonPayload, string message = null)
			{
				Type = type;
				JsonPayload = jsonPayload;
				if (type == ResponseType.Error && jsonPayload != null) Error = JsonConvert.DeserializeObject<ErrorResponse>(jsonPayload);
				if(message != null) Message = message;
			}
		}
	}
}
