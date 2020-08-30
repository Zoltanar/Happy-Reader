using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google;
using Google.Cloud.Translation.V2;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRGoogleTranslate
{
	public static class GoogleTranslate
	{
		private const string GoogleDetectedString = @"Our systems have detected unusual traffic from your computer network.  This page checks to see if it&#39;s really you sending the requests, and not a robot.";
		private const string GoogleDetectedString2 = @"This page appears when Google automatically detects requests coming from your computer network";
		//todo make this an external string?
		private const string TranslateFreeUrl = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=ja&tl=en&dt=t&q=";
		private static Dictionary<string, GoogleTranslation> _cache = new Dictionary<string, GoogleTranslation>();
		private static readonly HashSet<string> UntouchedStrings = new HashSet<string>();

		private static bool _noApiTranslation;

		private static string _googleCredentialPath;
		private static bool _canUseGoogleCredential;
		private static TranslationClient _client;
		private static readonly HttpClient FreeClient = new HttpClient();
		private static Func<string, string> _japaneseToRomaji;
		public static uint GotFromCacheCount { get; private set; }
		public static uint GotFromAPICount { get; private set; }
		private static ObservableCollection<GoogleTranslation> _linkedCache = new ObservableCollection<GoogleTranslation>();

		public static void Initialize(
			[NotNull] Dictionary<string, GoogleTranslation> existingCache,
			[NotNull] ObservableCollection<GoogleTranslation> inputCache,
			[NotNull] Func<string, string> japaneseToRomaji,
			string credentialLocation,
			string userAgentString,
			HashSet<string> untouchedStrings,
			bool noApiTranslation)
		{
			_japaneseToRomaji = japaneseToRomaji;
			_linkedCache = inputCache;
			_cache = existingCache;
			_noApiTranslation = noApiTranslation;
			_googleCredentialPath = credentialLocation;
			_canUseGoogleCredential = !_noApiTranslation && !string.IsNullOrWhiteSpace(_googleCredentialPath) && File.Exists(_googleCredentialPath);
			FreeClient.DefaultRequestHeaders.Add(@"user-agent", userAgentString);
			if (_canUseGoogleCredential)
			{
				Debug.Assert(_googleCredentialPath != null, nameof(_googleCredentialPath) + " != null");
				using (var stream = File.OpenRead(_googleCredentialPath))
				{
					_client = TranslationClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(stream));
				}
			}
			UntouchedStrings.Clear();
			foreach (var untouchedString in untouchedStrings)
			{
				UntouchedStrings.Add(untouchedString);
			}
		}

		public static void TranslateFree(StringBuilder text)
		{
			if (TryGetWithoutAPI(text, _noApiTranslation, out var input)) return;
			LogVerbose($"HRTranslate.Google - Getting string from API, input: {input}");
			string translated;
			try
			{
				var jsonString = GetPostResultAsString(FreeClient, TranslateFreeUrl + Uri.EscapeDataString(input));
				if (jsonString.Contains(GoogleDetectedString) || jsonString.Contains(GoogleDetectedString2))
				{
					var extracted = ExtractText(jsonString);
					text.Append($"Failed to translate, detected by Google: {extracted}");
					return;
				}
				if (!TryDeserializeJsonResponse(text, jsonString, out translated)) return;
				GotFromAPICount++;
			}
			catch (Exception ex)
			{
				//todo if result is html, extract visible text
				text.Append($"Failed to translate. ({ex.Message})");
				return;
			}
			if (string.IsNullOrWhiteSpace(translated)) return;
			SetTranslationAndSaveToCache(text, translated, input);
		}

		private static readonly Regex CombineEmptyLinesRegex = new Regex(@"^(\s*\n){2,}");

		public static string ExtractText(string html)
		{
			// Where m_whitespaceRegex is a Regex with [\s].
			// Where sampleHtmlText is a raw HTML string.

			var extractedSampleText = new StringBuilder();
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);

			if (doc.DocumentNode == null) return string.Empty;
			foreach (var node in doc.DocumentNode.Descendants("script")
				.Concat(doc.DocumentNode.Descendants("style"))
				.Concat(doc.DocumentNode.Descendants("head")).ToArray())
			{
				node.Remove();
			}
			var allTextNodes = doc.DocumentNode.SelectNodes("//text()");
			if (allTextNodes != null && allTextNodes.Count > 0)
			{
				foreach (var node in allTextNodes)
				{
					if (string.IsNullOrWhiteSpace(node.InnerText) || AnyParentHasAttribute(node, "style", v => v.Contains("display:none"))) continue;
					extractedSampleText.Append(node.InnerText);
				}
			}
			var text = extractedSampleText.ToString();
			var finalText = CombineEmptyLinesRegex.Replace(text, "\n").Trim();
			return finalText;
		}

		private static bool AnyParentHasAttribute(HtmlNode startNode, string name, Func<string, bool> function)
		{
			var node = startNode;
			while (node.ParentNode != null)
			{
				if (node.ParentNode.Attributes.Any(a => a.Name == name && function(a.Value))) return true;
				node = node.ParentNode;
			}
			return false;
		}

		private static void SetTranslationAndSaveToCache(StringBuilder text, string translated, string input)
		{
			text.Append(translated);
			var translation = new GoogleTranslation(input, translated);
			_linkedCache.Add(translation);
			_cache[input] = translation;
		}

		private static bool TryDeserializeJsonResponse(StringBuilder text, string jsonString, out string translated)
		{
			translated = null;
			try
			{
				var jArray = JsonConvert.DeserializeObject<JArray>(jsonString);
				var translatedObject = jArray[0][0];
				Debug.Assert(translatedObject != null, nameof(translatedObject) + " != null");
				translated = (translatedObject[0] ?? throw new InvalidOperationException("Json object was not o expected format.")).Value<string>();
				return true;
			}
			catch (Exception ex)
			{
				text.Append($"Failed to deserialize: {ex}");
				return false;
			}
		}

		private static string GetPostResultAsString(HttpClient client, string url)
		{
			LogVerbose($"Posting to url: {url}");
			Task<HttpResponseMessage> task = client.PostAsync(url, null);
			task.Wait(2500);
			Task<string> task2 = task.Result.Content.ReadAsStringAsync();
			task2.Wait(2500);
			string jsonString = task2.Result;
			return jsonString;
		}

		private static bool GetFromCache(StringBuilder text, string input)
		{
			bool inCache1 = _cache.TryGetValue(input, out var cachedTranslation);
			if (!inCache1) return false;
			LogVerbose($"HRTranslate.Google - Getting string from cache, input: {input}");
			GotFromCacheCount++;
			text.Append(cachedTranslation.Output);
			return true;
		}

		public static void Translate(StringBuilder text)
		{
			if (TryGetWithoutAPI(text, !_canUseGoogleCredential, out var input)) return;
			try
			{
				LogVerbose($"{nameof(HRGoogleTranslate)} - Getting string from API, input: {input}");
				var response = _client.TranslateText(input, "en", "ja", TranslationModel.Base);
				GotFromAPICount++;
				if (!string.IsNullOrWhiteSpace(response?.TranslatedText))
				{
					SetTranslationAndSaveToCache(text, response.TranslatedText, input);
				}
				else text.Append("Failed to translate");
			}
			catch (Exception ex)
			{
				text.Append($"Failed: {(ex is GoogleApiException gex ? gex.Error.Message : ex.Message)}");
			}
		}

		public static bool TranslateSingleKana(StringBuilder text, string input)
		{
			if (input.Length != 1) return false;
			var character = input[0];
			if (!character.IsHiragana() && !character.IsKatakana()) return false;
			var output = _japaneseToRomaji(input);
			text.Clear();
			text.Append(output);
			var translation = new GoogleTranslation(input, output);
			_linkedCache.Add(translation);
			_cache[input] = translation;
			return true;
		}

		private static bool TryGetWithoutAPI(StringBuilder text, bool isBlocked, out string input)
		{
			input = text.ToString();
			if (UntouchedStrings.Contains(input)) return true;
			text.Clear();
			if (GetFromCache(text, input)) return true;
			if (TranslateSingleKana(text, input)) return true;
			if (!isBlocked) return false;
			text.Append("Failed: Translation is blocked.");
			return true;

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
