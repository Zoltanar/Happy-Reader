using OriginalTextObject = System.Collections.Generic.List<(string Original, string Romaji)>;

namespace Happy_Reader
{
    public struct TranslationItem
    {
        public OriginalTextObject OriginalText { get; }
        public string TranslatedText { get; }

        public TranslationItem(OriginalTextObject originalText, string translatedText)
        {
            OriginalText = originalText;
            TranslatedText = translatedText;
        }
    }
}
