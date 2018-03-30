using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Google.Cloud.Translation.V2;

namespace HRGoogleTranslate
{
    public static class GoogleTranslate
    {
	    private static readonly Dictionary<string, GoogleTranslation> Cache = new Dictionary<string, GoogleTranslation>();
		private static readonly HashSet<string> UntouchedStrings = new HashSet<string>();
        private const string GoogleCredential = @"C:\Google\hrtranslate-credential.json";

        private static readonly TranslationClient Client;
        public static uint GotFromCacheCount { get; private set; }
        public static uint GotFromAPICount { get; private set; }
	    private static ObservableCollection<GoogleTranslation> _linkedCache = new ObservableCollection<GoogleTranslation>();

		public static void SetCache(ObservableCollection<GoogleTranslation> cache)
		{
			if (cache == null)
			{
				_linkedCache = new ObservableCollection<GoogleTranslation>();
				return;
			}
			_linkedCache = cache;
			foreach (var translation in cache) Cache[translation.Input] = translation;
	    }

	    static GoogleTranslate()
        {
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
		
        public static void Translate(StringBuilder text)
        {
#if NOTRANSLATION
            text.Clear();
            text.Append("Translation is blocked.");
#else
            if (UntouchedStrings.Contains(text.ToString())) return;
            var input = text.ToString();
	        bool inCache1 = Cache.TryGetValue(input, out var cachedTranslation);
            if (inCache1)
            {
                LogVerbose($"HRTranslate.Google - Getting string from cache, input: {input}");
				GotFromCacheCount++;
                text.Clear();
                text.Append(cachedTranslation.Output);
                return;
            }
            LogVerbose($"HRTranslate.Google - Getting string from API, input: {input}");
			var response = Client.TranslateText(text.ToString(), "en", "ja", TranslationModel.Base);
            GotFromAPICount++;
            text.Clear();
            if (!string.IsNullOrWhiteSpace(response?.TranslatedText))
            {
                text.Append(response.TranslatedText); var translation = new GoogleTranslation(input, response.TranslatedText);
	            _linkedCache.Add(translation);
	            Cache[input] = translation;
            }
            else text.Append("Failed to translate");
#endif
            }
		[Conditional("LOGVERBOSE")]
		private static void LogVerbose(string text) => Debug.WriteLine(text);
	}
}
