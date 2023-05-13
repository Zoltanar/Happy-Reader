using Newtonsoft.Json;

namespace Happy_Reader.TranslationEngine;

internal struct DeinflectionReason
{
    public string Key { get; set; }
    public string KanaIn { get; set; }
    public string KanaOut { get; set; }
    public string[] RulesIn { get; set; }
    public string[] RulesOut { get; set; }
    public override string ToString() => JsonConvert.SerializeObject(this);
}