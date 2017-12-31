using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.Cloud.Translation.V2;

namespace HRGoogleTranslate
{
    public static class GoogleTranslate
    {
#if !NOTRANSLATION
        private static readonly ConcurrentDictionary<string, Translation> Cache = new ConcurrentDictionary<string, Translation>();
        private static readonly HashSet<string> UntouchedStrings = new HashSet<string>();
        private const string GoogleCredential = @"C:\Google\hrtranslate-credential.json";

        private static readonly TranslationClient Client;
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
        }

        public static void LoadCache(IEnumerable<Translation> translations)
        {
            Cache.Clear();
            foreach (var translation in translations)
            {
                Cache.TryAdd(translation.Input, translation);
            }
        }

        public static IEnumerable<Translation> GetCache() => Cache.Values;
#endif
        public static void Translate(StringBuilder text)
        {
#if NOTRANSLATION
            text.Clear();
            text.Append("Translation is blocked.");
#else
            if (UntouchedStrings.Contains(text.ToString())) return;
            var input = text.ToString();
            if (Cache.ContainsKey(input))
            {
#if LOGVERBOSE
                System.Diagnostics.Debug.WriteLine($"HRTranslate.Google - Getting string from cache, input: {input}");
#endif
                text.Clear();
                text.Append(Cache[input].Output);
                return;
            }
#if LOGVERBOSE
            System.Diagnostics.Debug.WriteLine($"HRTranslate.Google - Getting string from API, input: {input}");
#endif
            var response = Client.TranslateText(text.ToString(), "en", "ja", TranslationModel.Base);
            text.Clear();
            if (!string.IsNullOrWhiteSpace(response?.TranslatedText))
            {
                text.Append(response.TranslatedText);
                Cache[input] = new Translation(input, response.TranslatedText);
            }
            else text.Append("Failed to translate");

#endif
            }
        }
}
