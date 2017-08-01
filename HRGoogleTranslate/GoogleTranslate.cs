using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Google.Cloud.Translation.V2;

namespace HRGoogleTranslate
{
    public static class GoogleTranslate
    {
#if !NOTRANSLATION
        private static readonly ConcurrentDictionary<string, Translation> Cache = new ConcurrentDictionary<string, Translation>();
        private const string GoogleCredential = @"C:\Google\hrtranslate-credential.json";

        private static readonly TranslationClient Client;
        static GoogleTranslate()
        {
            using (var stream = File.OpenRead(GoogleCredential))
            {
                Client = TranslationClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(stream));
            }
        }
#endif
        public static void Translate(StringBuilder text)
        {
#if NOTRANSLATION
            text.Clear();
            text.Append("Translation is blocked.");
#else
            var input = text.ToString();
            if (Cache.ContainsKey(input))
            {
                text.Clear();
                text.Append(Cache[input].Output);
                return;
            }
            var response = Client.TranslateText(text.ToString(), "ja", "en");
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
