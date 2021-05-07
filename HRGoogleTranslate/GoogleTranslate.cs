using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Translation;
using JetBrains.Annotations;

namespace HRGoogleTranslate
{
	public static class GoogleTranslate
	{
		private static readonly HashSet<string> UntouchedStrings = new HashSet<string>();
		private static DACollection<string, CachedTranslation> _cache;
		private static bool _logVerbose;
		private static ITranslator _credentialTranslator;
		private static ITranslator _freeTranslator;
		private static Func<string, string> _japaneseToRomaji;
		private static bool _useAnyCached = true; //todo keep cache collection grouped by source and change primary key to input + source
		private static uint GotFromCacheCount { get; set; }
		private static uint GotFromApiCount { get; set; }

		public static void Initialize(
			[NotNull] DACollection<string, CachedTranslation> existingCache,
			[NotNull] Func<string, string> japaneseToRomaji,
			string credentialLocation,
			string userAgentString,
			HashSet<string> untouchedStrings,
			bool noApiTranslation,
			bool logVerbose)
		{
			_logVerbose = logVerbose;
			_japaneseToRomaji = japaneseToRomaji;
			_cache = existingCache;
			UntouchedStrings.Clear();
			foreach (var untouchedString in untouchedStrings)
			{
				UntouchedStrings.Add(untouchedString);
			}
			_credentialTranslator = new GoogleTranslateApi();
			_freeTranslator = new GoogleTranslateFree();
			if (!noApiTranslation)
			{
				InitialiseTranslator(_credentialTranslator, new Dictionary<string, object>()
				{
					{GoogleTranslateApi.CredentialPropertyName, credentialLocation}
				});
			}
			else _credentialTranslator.Error = "Disabled by user.";
			InitialiseTranslator(_freeTranslator, new Dictionary<string, object>()
			{
				{GoogleTranslateFree.UserAgentPropertyName, userAgentString}
			});
		}

		private static void InitialiseTranslator(ITranslator translator, Dictionary<string, object> properties)
		{
			try
			{
				translator.Initialise(properties);
			}
			catch (Exception ex)
			{
				translator.Error = $"Failed to initialise: {ex.Message}";
			}
		}

		private static void SetTranslationAndSaveToCache(StringBuilder text, string translated, string input, string sourceName)
		{
			GotFromApiCount++;
			text.Append(translated);
			var translation = new CachedTranslation(input, translated, sourceName);
			_cache.UpsertLater(translation);
		}

		private static bool GetFromCache(string cacheSource, StringBuilder text, string input)
		{
			//todo keep cache collection grouped by source and change primary key to input + source
			var item = cacheSource == null ? _cache[input] : _cache.FirstOrDefault(i=>i.Source == cacheSource && i.Key == input);
			if (item == null) return false;
			LogVerbose($"HRTranslate.Google - Getting string from cache, input: {input}");
			GotFromCacheCount++;
			item.Update();
			_cache.UpsertLater(item);
			text.Append(item.Output);
			return true;
		}

		public static void Translate(StringBuilder text, bool useCredential)
		{
			var translator = useCredential ? _credentialTranslator : _freeTranslator;
			if (TryGetWithoutApi(_useAnyCached ? null : translator.SourceName, text, false, out var input)) return;
			var success = translator.Translate(input, out var translated);
			if (success) SetTranslationAndSaveToCache(text, translated, input, translator.SourceName);
		}

		public static bool TranslateSingleKana(StringBuilder text, string input)
		{
			if (input.Length != 1) return false;
			var character = input[0];
			if (!character.IsHiragana() && !character.IsKatakana()) return false;
			//if character is 'tsu' on its own, we remove it.
			var output = character == 'っ' || character == 'ッ' ? string.Empty : _japaneseToRomaji(input);
			text.Clear();
			SetTranslationAndSaveToCache(text, output, input, "Single Kana");
			return true;
		}

		private static bool TryGetWithoutApi(string cacheSource, StringBuilder text, bool isBlocked, out string input)
		{
			input = text.ToString();
			if (UntouchedStrings.Contains(input)) return true;
			text.Clear();
			if (GetFromCache(cacheSource, text, input)) return true;
			if (TranslateSingleKana(text, input)) return true;
			if (!isBlocked) return false;
			text.Append("Failed: Translation is blocked.");
			return true;

		}

		private static void LogVerbose(string text)
		{
			if (!_logVerbose) return;
			Debug.WriteLine(text);
		}

		/// <summary>
		/// Character is between points \u3040 and \u309f
		/// </summary>
		private static bool IsHiragana(this char character) => character >= 0x3040 && character <= 0x309f;

		/// <summary>
		/// Character is between points \u30a0 and \u30ff
		/// </summary>
		private static bool IsKatakana(this char character) => character >= 0x30a0 && character <= 0x30ff;

		public static void ExitProcedures(Func<int> saveData)
		{
			Debug.WriteLine($"[{nameof(GoogleTranslate)}] Got from cache {GotFromCacheCount}");
			Debug.WriteLine($"[{nameof(GoogleTranslate)}] Got from API {GotFromApiCount}");
			saveData();
		}
	}
}
