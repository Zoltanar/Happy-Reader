using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRDeepLTranslate
{
	// ReSharper disable once UnusedType.Global used with plugin model.
	public class DeepLTranslateFree : ITranslator
	{
		private const string Url = @"https://api-free.deepl.com/v2/translate?source_lang=JA&target_lang=EN-US&split_sentences=0"; //todo make editable
		private const string AuthKeyPropertyName = @"Authentication Key";
		private const string PreventDetailsPropertyName = @"Prevent Details";

		public string Version => @"1.0";
		public string SourceName => @"DeepL API Free";

		private static readonly HttpClient FreeClient = new();
		private FreeSettings Settings { get; set; }

		public IReadOnlyDictionary<string, Type> Properties { get; } = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>
		{
			{AuthKeyPropertyName, typeof(string)},
			{PreventDetailsPropertyName, typeof(bool)}
		});

		public string Error { get; set; }

		public void Initialise()
		{
			Error = string.IsNullOrWhiteSpace(Settings.AuthenticationKey) ? "Authentication Key is not set" : null;
		}

		public void LoadProperties(string filePath)
		{
			Settings = SettingsJsonFile.Load<FreeSettings>(filePath);
		}

		public void SaveProperties(string filePath)
		{
			//done automatically via Settings object.
		}

		public void SetProperty(string propertyName, object value)
		{
			switch (propertyName)
			{
				case AuthKeyPropertyName when value is string authenticationKey:
					Settings.AuthenticationKey = authenticationKey;
					break;
				case PreventDetailsPropertyName when value is bool preventDetails:
					Settings.PreventDetails = preventDetails;
					break;
				default:
					throw new ArgumentOutOfRangeException(propertyName,
						$"Property not found or wrong type: '{propertyName}' '{value.GetType()}'.");
			}
		}

		public object GetProperty(string propertyName)
		{
			return propertyName switch
			{
				AuthKeyPropertyName => Settings.AuthenticationKey,
				PreventDetailsPropertyName => Settings.PreventDetails,
				_ => throw new ArgumentOutOfRangeException(propertyName, $"Property not found: '{propertyName}'.")
			};
		}

		public bool Translate(string input, out string output)
		{
			try
			{
				var url = FormUrl(input);
				var success = GetPostResultAsString(FreeClient, url, out output);
				if (!success) return false;
				success = TryDeserializeJsonResponse(output, out output);
				if (!success) return false;
				if (Settings.PreventDetails && input.Length <= 4) PreventDetails(ref output);
				return true;
			}
			catch (Exception ex)
			{
				//todo if result is html, extract visible text
				output = $"Failed to translate. ({ex.Message})";
				return false;
			}
		}

		private void PreventDetails(ref string output)
		{
			var openBracket = output.IndexOf('(');
			if (openBracket <= 0) return;
			var closeBracket = output.IndexOf(')', openBracket);
			if (closeBracket == -1 || closeBracket != output.Length-1) return;
			output = output.Substring(0, openBracket).Trim();
		}

		private string FormUrl(string input)
		{
			return $"{Url}&auth_key={Settings.AuthenticationKey}&text={Uri.EscapeDataString(input)}";
		}

		private static bool GetPostResultAsString(HttpClient client, string url, out string output)
		{
			var task = client.PostAsync(url, null);
			task.Wait(2500);
			var result = task.Result;
			var task2 = result.Content.ReadAsStringAsync();
			task2.Wait(2500);
			var response = task2.Result;
			if (!result.IsSuccessStatusCode)
			{
				if (response.Length > 0)
				{
					TryDeserializeJsonResponse(response, out var message);
					output = $"Translation failed: {message}";
					return false;
				}
				output = $"Post was not successful: {result.StatusCode}";
				return false;
			}
			output = response;
			return true;
		}

		private static bool TryDeserializeJsonResponse(string jsonString, out string translated)
		{
			translated = null;
			try
			{
				var jObject = JsonConvert.DeserializeObject<JObject>(jsonString) ?? throw new InvalidOperationException("Json Response was null.");
				var message = jObject["message"];
				if (message != null)
				{
					translated = message.Value<string>();
					return false;
				}
				var translationsObject = jObject["translations"] ?? throw new InvalidOperationException("Translations object not found");
				var firstTranslation = translationsObject.First ?? throw new InvalidOperationException("Translations object was empty.");
				translated = (firstTranslation["text"] ?? throw new InvalidOperationException("Text element not found.")).Value<string>();
				return true;
			}
			catch (Exception ex)
			{
				translated = $"Failed to deserialize: {ex}";
				return false;
			}
		}
	}

	public class FreeSettings : SettingsJsonFile
	{
		private string _authenticationKey = string.Empty;
		private bool _preventDetails = true;


		public string AuthenticationKey
		{
			get => _authenticationKey;
			set
			{
				if (_authenticationKey == value) return;
				_authenticationKey = value;
				if (Loaded) Save();
			}
		}

		public bool PreventDetails
		{
			get => _preventDetails;
			set
			{
				if (_preventDetails == value) return;
				_preventDetails = value;
				if (Loaded) Save();
			}
		}

	}
}
