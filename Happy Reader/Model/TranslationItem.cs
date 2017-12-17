using System.Linq;
using OriginalTextObject = System.Collections.Generic.List<(string Original, string Romaji)>;

namespace Happy_Reader
{
    public struct TranslationItem
    {
        public OriginalTextObject OriginalText { get; }
        public string RightLabel { get; }
        public string Character { get; }
        public string TranslatedText { get; }

        public TranslationItem(string rightLabel, OriginalTextObject originalText, string translatedText)
        {
            RightLabel = rightLabel;
            OriginalText = originalText;
            var original = string.Join("", originalText.Select(x => x.Original));
            var firstBracket = original.IndexOfAny(new[] { '『', '「' });
            if (firstBracket >= 0 && new[] { '』', '」' }.Contains(originalText.Last().Original.Last()))
            {
                Character = original.Substring(0, firstBracket);
            }
            else Character = "";
            TranslatedText = translatedText;
        }
    }
}
