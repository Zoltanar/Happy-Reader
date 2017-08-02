using OriginalTextObject = System.Collections.Generic.List<(string Original, string Romaji)>;

namespace Happy_Reader
{
    public struct TranslationItem
    {
        public OriginalTextObject OriginalText { get; }
        public string Context { get; }
        public string Character { get; }
        public string TranslatedText { get; }

        public TranslationItem(HookInfo context, OriginalTextObject originalText, string translatedText)
        {
            Context = $"[{context.ContextId:x}]{context.Name}";
            OriginalText = originalText;
            Character = "<>";
            TranslatedText = translatedText;
        }
    }
}
