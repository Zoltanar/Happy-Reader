using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Google;
using Google.Cloud.Translation.V2;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using JetBrains.Annotations;

namespace HRGoogleTranslate
{
	[UsedImplicitly]
	public class GoogleTranslateApi : ITranslator
	{
		private const string CredentialPropertyKey = "Credential Location";
		private const string ModelPropertyKey = "Translation Model";

		private static TranslationClient _client;

		public string Version => "1.0";

		public string SourceName => "Google Translate API";
		public string Error { get; set; }
		public IReadOnlyDictionary<string, Type> Properties { get; } = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>()
		{
			{CredentialPropertyKey, typeof(string)},
			{ModelPropertyKey, typeof(TranslationModel)}
		});

		public void Initialise()
		{
			Error = null;
			try
			{
				SetGoogleCredential(Settings.GoogleCredentialPath);
			}
			catch (Exception ex)
			{
				Error = ex.Message;
			}
		}

		private ApiSettings Settings { get; set; }

		public bool Translate(string input, out string output)
		{
			try
			{
				var response = _client.TranslateText(input, "en", "ja", Settings.TranslationModel);
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

		public void SetProperty(string propertyKey, object value)
		{
			switch (propertyKey)
			{
				case CredentialPropertyKey when value is string credentialLocation:
					SetGoogleCredential(credentialLocation);
					break;
				case ModelPropertyKey when value is TranslationModel translationModel:
					Settings.TranslationModel = translationModel;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(propertyKey), $"Unrecognized property key: {propertyKey}");
			}
		}

		public object GetProperty(string propertyKey)
		{
			return propertyKey switch
			{
				CredentialPropertyKey => Settings.GoogleCredentialPath,
				ModelPropertyKey => Settings.TranslationModel,
				_ => throw new ArgumentOutOfRangeException(nameof(propertyKey), $"Unrecognized property key: {propertyKey}")
			};
		}

		public void LoadProperties(string filePath)
		{
			Settings = SettingsJsonFile.Load<ApiSettings>(filePath);
		}

		public void SaveProperties(string filePath)
		{
			//done automatically whenever a property changes.
		}

		private void SetGoogleCredential(string credentialPath)
		{
			Settings.GoogleCredentialPath = credentialPath;
			if (string.IsNullOrWhiteSpace(credentialPath)) throw new ArgumentNullException(credentialPath, "Google Credential path was empty.");
			if (!File.Exists(credentialPath)) throw new FileNotFoundException("Google Credential file not found", credentialPath);
			try
			{
				Debug.Assert(credentialPath != null, nameof(credentialPath) + " != null");
				using var stream = File.OpenRead(credentialPath);
				_client = TranslationClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(stream));
			}
			catch (Exception ex)
			{
				throw new Exception("Exception initialising Google API client.", ex);
			}
		}

		private class ApiSettings : SettingsJsonFile
		{
			// ReSharper disable StringLiteralTypo
			private string _googleCredentialPath = @"C:\Google\hrtranslate-credential.json";
			// ReSharper restore StringLiteralTypo

			private TranslationModel _translationModel = TranslationModel.NeuralMachineTranslation;

			public string GoogleCredentialPath
			{
				get => _googleCredentialPath;
				set
				{
					if (_googleCredentialPath == value) return;
					_googleCredentialPath = value;
					if (Loaded) Save();
				}
			}

			public TranslationModel TranslationModel
			{
				get => _translationModel;
				set
				{
					if (_translationModel == value) return;
					_translationModel = value;
					if (Loaded) Save();
				}
			}
		}

	}
}
