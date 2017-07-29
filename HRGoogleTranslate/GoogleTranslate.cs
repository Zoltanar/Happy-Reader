using System.IO;
using System.Text;
using Google.Cloud.Translation.V2;

namespace HRGoogleTranslate
{
    public static class GoogleTranslate
    {
#if !NOTRANSLATION
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
        public static void Translate(StringBuilder text, string fromLanguage = "en", string toLanguage = "ja")
        {
#if NOTRANSLATION
            text.Clear();
            text.Append("Translation is blocked.");
#else
            var response = Client.TranslateText(text.ToString(), fromLanguage, toLanguage);
            text.Clear();
            text.Append(
            string.IsNullOrWhiteSpace(response?.TranslatedText)
                ? "Failed to translate"
                : response.TranslatedText);
#endif
        }
    }
}
