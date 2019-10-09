using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using Google.Cloud.Translation.V2;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRGoogleTranslate
{
	public static class GoogleTranslate
	{
		private static Dictionary<string, GoogleTranslation> _cache = new Dictionary<string, GoogleTranslation>();
		private static readonly HashSet<string> UntouchedStrings = new HashSet<string>();
		private const string GoogleCredential = @"C:\Google\hrtranslate-credential.json";

		private static readonly TranslationClient Client;
		private static readonly HttpClient FreeClient = new HttpClient();
		private static Func<string, string> _japaneseToRomaji;
		public static uint GotFromCacheCount { get; private set; }
		public static uint GotFromAPICount { get; private set; }
		private static ObservableCollection<GoogleTranslation> _linkedCache = new ObservableCollection<GoogleTranslation>();

		public static void Initialize([NotNull] Dictionary<string, GoogleTranslation> existingCache, [NotNull]ObservableCollection<GoogleTranslation> inputCache, [NotNull] Func<string, string> japaneseToRomaji)
		{
			_japaneseToRomaji = japaneseToRomaji;
			_linkedCache = inputCache;
			_cache = existingCache;
		}

		static GoogleTranslate()
		{
			FreeClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
			using (var stream = File.OpenRead(GoogleCredential))
			{
				Client = TranslationClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(stream));
			}
			BuildUntouchedStrings();
		}

		private static void BuildUntouchedStrings()
		{
			UntouchedStrings.Clear();
			UntouchedStrings.Add("");
			UntouchedStrings.Add("\r\n");
		}

		public static void TranslateFree(StringBuilder text)
		{
#if NOTRANSLATION
            text.Clear();
            text.Append("Translation is blocked.");
#else
			if (UntouchedStrings.Contains(text.ToString())) return;
			var input = text.ToString();
			text.Clear();
			bool inCache1 = _cache.TryGetValue(input, out var cachedTranslation);
			if (inCache1)
			{
				LogVerbose($"HRTranslate.Google - Getting string from cache, input: {input}");
				GotFromCacheCount++;
				text.Append(cachedTranslation.Output);
				return;
			}
			if (input.Length == 1)
			{
				var character = input[0];
				// ReSharper disable once UnusedVariable
				if (character.IsHiragana() || character.IsKatakana())
				{
					var output = _japaneseToRomaji(input);
					text.Append(output);
					var translation = new GoogleTranslation(input, output);
					_linkedCache.Add(translation);
					_cache[input] = translation;
					return;
				}
			}
			LogVerbose($"HRTranslate.Google - Getting string from API, input: {input}");
			string translated;
			try
			{
				var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=ja&tl=en&dt=t&q=" + Uri.EscapeDataString(input);
				var task = FreeClient.PostAsync(url, null);
				task.Wait(2500);
				var task2 = task.Result.Content.ReadAsStringAsync();
				task2.Wait(2500);
				string jsonString = task2.Result;
				if (jsonString.Contains(
					"Our systems have detected unusual traffic from your computer network.  This page checks to see if it&#39;s really you sending the requests, and not a robot.")
				)
				{
					//Process.Start(url);
					text.Append("Failed to translate, detected by Google.");
					return;
				}
				var jArray = JsonConvert.DeserializeObject<JArray>(jsonString);
				var translatedObject = jArray[0][0];
				translated = translatedObject[0].Value<string>();
				GotFromAPICount++;
			}
			catch (Exception ex)
			{
				//todo if result is html, extract visible text
				text.Append($"Failed to translate. ({ex.Message})");
				return;
			}
			if (!string.IsNullOrWhiteSpace(translated))
			{
				text.Append(translated);
				var translation = new GoogleTranslation(input, translated);
				_linkedCache.Add(translation);
				_cache[input] = translation;
			}
			else text.Append("Failed to translate");
#endif
		}

		public static void Translate(StringBuilder text)
		{
#if NOTRANSLATION
            text.Clear();
            text.Append("Translation is blocked.");
#else
			if (UntouchedStrings.Contains(text.ToString())) return;
			var input = text.ToString();
			text.Clear();
			bool inCache1 = _cache.TryGetValue(input, out var cachedTranslation);
			if (inCache1)
			{
				LogVerbose($"HRTranslate.Google - Getting string from cache, input: {input}");
				GotFromCacheCount++;
				text.Append(cachedTranslation.Output);
				return;
			}
			if (input.Length == 1)
			{
				var character = input[0];
				// ReSharper disable once UnusedVariable
				if (character.IsHiragana() || character.IsKatakana())
				{
					var output = _japaneseToRomaji(input);
					text.Append(output);
					var translation = new GoogleTranslation(input, output);
					_linkedCache.Add(translation);
					_cache[input] = translation;
					return;
				}
			}
			LogVerbose($"HRTranslate.Google - Getting string from API, input: {input}");
			try
			{

				TranslationResult response = Client.TranslateText(input, "en", "ja", TranslationModel.Base);
				GotFromAPICount++;
				if (!string.IsNullOrWhiteSpace(response?.TranslatedText))
				{
					text.Append(response.TranslatedText);
					var translation = new GoogleTranslation(input, response.TranslatedText);
					_linkedCache.Add(translation);
					_cache[input] = translation;
				}
				else text.Append("Failed to translate");
			}
			catch (Google.GoogleApiException ex)
			{
				text.Append($"Failed: {ex.Error.Message}");
			}
			catch (Exception gex)
			{
				text.Append($"Failed: {gex.Message}");
			}
#endif
		}

		[Conditional("LOGVERBOSE")]
		private static void LogVerbose(string text) => Debug.WriteLine(text);

		/// <summary>
		/// Character is between points \u3040 and \u309f
		/// </summary>
		private static bool IsHiragana(this char character) => character >= 0x3040 && character <= 0x309f;

		/// <summary>
		/// Character is between points \u30a0 and \u30ff
		/// </summary>
		private static bool IsKatakana(this char character) => character >= 0x30a0 && character <= 0x30ff;
	}
}
