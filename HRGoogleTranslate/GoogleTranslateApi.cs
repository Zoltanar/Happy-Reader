using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Google;
using Google.Cloud.Translation.V2;
using Happy_Apps_Core.Translation;

namespace HRGoogleTranslate
{
	public class GoogleTranslateApi : ITranslator
	{
		public const string CredentialPropertyName = "Credential Location";

		public string SourceName => "Google Translate API";

		public string Error { get; set; }

		public Dictionary<string, Type> Properties { get; } = new Dictionary<string, Type>
		{
			{"Credential Location", typeof(string)}
		};
		
		private static TranslationClient _client;

		public void Initialise(Dictionary<string, object> properties)
		{
			Error = null;
			properties.TryGetValue(CredentialPropertyName, out var credentialLocationObject);
			var credentialLocation = credentialLocationObject as string;
			SetGoogleCredential(credentialLocation);
		}

		private static void SetGoogleCredential(string credentialPath)
		{
			if (string.IsNullOrWhiteSpace(credentialPath)) throw new ArgumentNullException(credentialPath, "Google Credential path was empty.");
			if (!File.Exists(credentialPath)) throw new FileNotFoundException("Google Credential file not found", credentialPath);
			try
			{
				Debug.Assert(credentialPath != null, nameof(credentialPath) + " != null");
				using (var stream = File.OpenRead(credentialPath))
				{
					_client = TranslationClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(stream));
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Exception initialising Google API client.", ex);
			}
		}

		public bool Translate(string input, out string output)
		{
			try
			{
				var response = _client.TranslateText(input, "en", "ja", TranslationModel.NeuralMachineTranslation);
				if (!string.IsNullOrWhiteSpace(response?.TranslatedText))
				{
					output = response.TranslatedText;
					return true;
				}
				output = "Failed to translate";
				return false;
			}
			catch (Exception ex)
			{
				output = $"Failed: {(ex is GoogleApiException gex ? gex.Error.Message : ex.Message)}";
				return false;
			}
		}
		
	}
}
