using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using JetBrains.Annotations;

namespace HRGoogleTranslate
{
	[UsedImplicitly]
	public class GoogleTranslateApi : ITranslator
	{
		private const string CredentialPropertyKey = "API Key / File Path";
		private const string ModelPropertyKey = "Translation Model";
		private const string BadRepetitionKey = "Prevent Bad Repetition";
		private const string TargetLanguage = "en";
		private const string SourceLanguage = "ja";
		private const string EmptyResponseMessage = "Failed to translate, empty response.";
		private readonly Regex _badRepetitionPattern = new(@"(.)\1{5,}", RegexOptions.Compiled);

		private TranslationClient _client;

		public string Version => "1.1";
		public string SourceName => "Google Translate API";
		public string Error { get; set; }
		public IReadOnlyDictionary<string, Type> Properties { get; } = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>()
		{
			{CredentialPropertyKey, typeof(string)},
			{ModelPropertyKey, typeof(TranslationModel)},
			{BadRepetitionKey, typeof(bool)}
		});

		public void Initialise()
		{
			Error = null;
			try
			{
				SetGoogleCredential(Settings.GoogleCredential);
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
				var response = _client.TranslateText(input, TargetLanguage, SourceLanguage, Settings.TranslationModel);
				if (!string.IsNullOrWhiteSpace(response?.TranslatedText))
				{
					output = response.TranslatedText;
					if (Settings.PreventBadRepetition)
					{
						var success = PreventBadRepetition(input, ref output);
						return success;
					}
					return true;
				}
				output = EmptyResponseMessage;
				return false;
			}
			catch (Exception ex)
			{
				output = $"Failed: {(ex is GoogleApiException gex ? gex.Error.Message : ex.Message)}";
				return false;
			}
		}

		/// <summary>
		/// If translation output has an incorrect repetition pattern
		/// like 'Hmmmmmmm...', and input has Japanese comma,
		/// split the text by the commas and translate each part individually.
		/// </summary>
		private bool PreventBadRepetition(string input, ref string output)
		{
			const char jComma = '、';
			if (!input.Any(c => c.Equals(jComma)) || !_badRepetitionPattern.IsMatch(output)) return true;
			var parts = input.Split(jComma);
			var outputParts = _client.TranslateText(parts, TargetLanguage, SourceLanguage, Settings.TranslationModel);
			if (outputParts.Any(p => string.IsNullOrWhiteSpace(p.TranslatedText)))
			{
				output = EmptyResponseMessage;
				return false;
			}
			output = string.Join(", ", outputParts.Select(p => p.TranslatedText));
			return true;
		}

		public void SetProperty(string propertyKey, object value)
		{
			switch (propertyKey)
			{
				case CredentialPropertyKey when value is string credentialLocation:
					try
					{
						SetGoogleCredential(credentialLocation);
					}
					catch (Exception ex)
					{
						Error = ex.Message;
					}
					break;
				case ModelPropertyKey when value is TranslationModel translationModel:
					Settings.TranslationModel = translationModel;
					break;
				case BadRepetitionKey when value is bool preventBadRepetition:
					Settings.PreventBadRepetition = preventBadRepetition;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(propertyKey), $"Unrecognized property key: {propertyKey}");
			}
		}

		public object GetProperty(string propertyKey)
		{
			return propertyKey switch
			{
				CredentialPropertyKey => Settings.GoogleCredential,
				ModelPropertyKey => Settings.TranslationModel,
				BadRepetitionKey => Settings.PreventBadRepetition,
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

		private void SetGoogleCredential(string credential)
		{
			Settings.GoogleCredential = credential;
			if (string.IsNullOrWhiteSpace(credential)) throw new ArgumentNullException(credential, "Google Credential was empty.");
			try
			{
				_client = File.Exists(credential) ? TranslationClient.Create(GoogleCredential.FromFile(credential)) : TranslationClient.CreateFromApiKey(credential, Settings.TranslationModel);
				Error = null;
			}
			catch (Exception ex)
			{
				throw new Exception("Exception initialising Google API client.", ex);
			}
		}

		private class ApiSettings : SettingsJsonFile
		{
			private string _googleCredential = @"";
			private TranslationModel _translationModel = TranslationModel.NeuralMachineTranslation;
			private bool _preventBadRepetition = true;

			public string GoogleCredential
			{
				get => _googleCredential;
				set
				{
					if (_googleCredential == value) return;
					_googleCredential = value;
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

			/// <summary>
			/// If translation output has an incorrect repetition pattern
			/// like 'Hmmmmmmm...', and input has Japanese comma,
			/// split the text by the commas and translate each part individually.
			/// </summary>
			public bool PreventBadRepetition
			{
				get => _preventBadRepetition;
				set
				{
					if (_preventBadRepetition == value) return;
					_preventBadRepetition = value;
					if (Loaded) Save();
				}
			}
		}

	}
}
