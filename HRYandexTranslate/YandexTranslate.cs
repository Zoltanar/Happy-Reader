using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRYandexTranslate
{
	// ReSharper disable once UnusedType.Global used with plugin model.
	public class YandexTranslate : ITranslator
	{
		public string Version => "1.0";
		public string SourceName => "Yandex API";
		public IReadOnlyDictionary<string, Type> Properties { get; } = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>
		{
			{UrlPropertyName, typeof(string)},
			{ApiKeyPropertyName, typeof(string)}
		});
		public string Error { get; set; }

		private const string TranslationFailed = @"Translation failed: ";
		private const string UrlPropertyName = @"API Url";
		private const string ApiKeyPropertyName = @"API Key";
		private static readonly HttpClient FreeClient = new();
		private YandexSettings Settings { get; set; }

		public void Initialise()
		{
			Error = string.IsNullOrWhiteSpace(Settings.ApiKey) ? "API Key is not set" : null;
		}

		public void LoadProperties(string filePath)
		{
			Settings = SettingsJsonFile.Load<YandexSettings>(filePath);
		}

		public void SaveProperties(string filePath)
		{
			//done automatically via Settings object.
		}

		public void SetProperty(string propertyName, object value)
		{
			Error = null;
			switch (propertyName)
			{
				case UrlPropertyName when value is string url:
					Settings.Url = url;
					break;
				case ApiKeyPropertyName when value is string apiKey:
					Settings.ApiKey = apiKey;
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
				UrlPropertyName => Settings.Url,
				ApiKeyPropertyName => Settings.ApiKey,
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
				return true;
			}
			catch (Exception ex)
			{
				//todo if result is html, extract visible text
				output = $"{TranslationFailed}{ex.Message}";
				return false;
			}
		}

		private string FormUrl(string input)
		{
			return $"{Settings.Url}&key={Settings.ApiKey}&text={Uri.EscapeDataString(input)}";
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
					output = $"{TranslationFailed}{message}";
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
				var code = jObject["code"];
				if (code?.Value<int>() != 200)
				{
					translated = jsonString;
					return false;
				}
				var translationsObject = jObject["text"] ?? throw new InvalidOperationException("Translations object not found");
				var firstTranslation = translationsObject.First ?? throw new InvalidOperationException("Translations object was empty.");
				translated = firstTranslation.Value<string>();
				return true;
			}
			catch (Exception ex)
			{
				translated = $"Failed to deserialize: {ex}";
				return false;
			}
		}

		public class YandexSettings : SettingsJsonFile
		{
			private string _url = @"https://translate.yandex.net/api/v1.5/tr.json/translate?lang=ja-en&format=plain";
			private string _apiKey = string.Empty;

			public string Url
			{
				get => _url;
				set
				{
					if (_url == value) return;
					_url = value;
					if (Loaded) Save();
				}
			}

			public string ApiKey
			{
				get => _apiKey;
				set
				{
					if (_apiKey == value) return;
					_apiKey = value;
					if (Loaded) Save();
				}
			}
		}
	}
}
