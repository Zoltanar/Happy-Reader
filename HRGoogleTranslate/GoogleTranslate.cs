using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Google.Cloud.Translation.V2;

namespace HRGoogleTranslate
{
    public static class GoogleTranslate
    {
#if !NOTRANSLATION
        private static ImmutableDictionary<string, Translation> _presentCache;
        private static readonly Dictionary<string, Translation> NewCache = new Dictionary<string, Translation>();
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
            var builder = ImmutableDictionary.CreateBuilder<string,Translation>();
            foreach (var translation in translations)
            {
                builder.Add(translation.Input, translation);
            }

            _presentCache = builder.ToImmutable();
        }

        public static IEnumerable<Translation> GetNewCache() => NewCache.Values;
#endif
        public static void Translate(StringBuilder text)
        {
#if NOTRANSLATION
            text.Clear();
            text.Append("Translation is blocked.");
#else
            if (UntouchedStrings.Contains(text.ToString())) return;
            var input = text.ToString();
            if (_presentCache.ContainsKey(input))
            {
#if LOGVERBOSE
                System.Diagnostics.Debug.WriteLine($"HRTranslate.Google - Getting string from cache, input: {input}");
#endif
                text.Clear();
                text.Append(_presentCache[input].Output);
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
                NewCache[input] = new Translation(input, response.TranslatedText);
            }
            else text.Append("Failed to translate");

#endif
            }
        }
}
