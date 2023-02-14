using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Happy_Apps_Core;
using Newtonsoft.Json;
using static Happy_Reader.JMDict;

namespace Happy_Reader.TranslationEngine
{
    internal class Deinflection
    {
        private DeinflectionReason[] _deinflectionReasons = Array.Empty<DeinflectionReason>();
        private readonly Dictionary<string, DeinflectedTerm[]> _deinflectedTerms = new();
        internal int ReasonsCount => _deinflectionReasons.Length;
        private const int MaxDeinflectionDepth = 3;
        
        internal DeinflectedTerm[] GetDeinflections(Term term)
        {
            if (!_deinflectedTerms.TryGetValue(term.Expression, out var deinflections))
            {
                deinflections = _deinflectedTerms[term.Expression] = CreateDeinflections(term);
            }
            return deinflections;
        }

        private void PreLoad(Term[] dictionaryTerms)
        {
            StaticHelpers.Logger.ToFile("Pre-loading Deinflections...");
            int counter = 0;
            foreach (var term in dictionaryTerms)
            {
                counter++;
                GetDeinflections(term);
                if (counter % 1000 == 0) StaticHelpers.Logger.ToDebug($"Processed {counter} terms, deinflections total {_deinflectedTerms.Sum(p => p.Value.Length):N0}...");
            }
            StaticHelpers.Logger.ToFile("Finished pre-loading deinflections.");
        }

        internal Dictionary<string, DeinflectionReason[]> ReadFile(string filePath)
        {
            StaticHelpers.Logger.ToFile("Reading file for Deinflections...");
            try
            {
                var text = File.ReadAllText(filePath);
                var reasons = JsonConvert.DeserializeObject<Dictionary<string, DeinflectionReason[]>>(text) ?? new Dictionary<string, DeinflectionReason[]>();
                return reasons;
            }
            catch (Exception ex)
            {
                StaticHelpers.Logger.ToFile($"Failed to read file for Deinflections '{filePath}':", ex.ToString());
                return new Dictionary<string, DeinflectionReason[]>();
            }
        }

        internal void CreateReasons(Dictionary<string, DeinflectionReason[]> deinflections, Term[] dictionaryTerms)
        {
            _deinflectionReasons = deinflections.SelectMany(p => p.Value.Select(v =>
            {
                v.Key = p.Key;
                return v;
            })).ToArray();
            PreLoad(dictionaryTerms);
        }

        private DeinflectedTerm[] CreateDeinflections(Term term)
        {
            var withTermRules = _deinflectionReasons.Where(d => term.Expression.EndsWith(d.KanaOut) && d.RulesOut.Contains(term.Rules)).ToList();
            var deinflections = new List<DeinflectedTerm>();
            foreach (var deinflectionReason in withTermRules)
            {
                var result = deinflectionReason.KanaOut.Length == 0 ? term.Expression + deinflectionReason.KanaIn : term.Expression.Replace(deinflectionReason.KanaOut, deinflectionReason.KanaIn);
                deinflections.Add(new DeinflectedTerm(term, result, false, new List<DeinflectionReason> { deinflectionReason }));
            }
            bool anyAdded;
            do
            {
                anyAdded = false;
                foreach (var deinflectedTerm in deinflections.ToArray())
                {
                    if (deinflectedTerm.Completed) continue;
                    var reasons = deinflectedTerm.Reasons;
                    var text = deinflectedTerm.Text;
                    if (reasons.Count >= MaxDeinflectionDepth)
                    {
                        deinflectedTerm.Completed = true;
                        continue;
                    }
                    var withKanaOut2 = _deinflectionReasons.Where(d => text.EndsWith(d.KanaOut) && d.RulesOut.Intersect(reasons.Last().RulesIn).Any()).ToList();
                    foreach (var deinflectionReason in withKanaOut2)
                    {
                        if (deinflectionReason.Equals(reasons.Last())) continue;
                        var result = deinflectionReason.KanaOut.Length == 0
                            ? text + deinflectionReason.KanaIn
                            : text.Replace(deinflectionReason.KanaOut, deinflectionReason.KanaIn);
                        deinflections.Add(new DeinflectedTerm(term, result, false, new List<DeinflectionReason>(reasons) { deinflectionReason }));
                        anyAdded = true;
                    }
                    deinflectedTerm.Completed = true;
                }
            } while (anyAdded);
            return deinflections.ToArray();
        }

        internal static readonly string[] Rules =
        {
            "v1", // Verb ichidan
            "v5", // Verb godan
            "vs", // Verb suru
            "vk", // Verb kuru
            "vz", // Verb zuru
            "adj-i", // Adjective i
            "iru", // Intermediate -iru endings for progressive or perfect tense
        };
    }

    internal class DeinflectedTerm : ITerm
    {
        public Term DictionaryTerm { get; }
        public string Text { get; }
        public long Score => 0;
        public bool Completed { get; set; }
        public List<DeinflectionReason> Reasons { get; }

        public DeinflectedTerm(Term term, string text, bool completed, List<DeinflectionReason> reasons)
        {
            DictionaryTerm = term;
            Text = text;
            Completed = completed;
            Reasons = reasons;
        }

        public string Detail(JMDict jmDict)
        {
            var deinflectedReasons = $"{string.Join(" ≪ ", Reasons.Select(r => r.Key))}";
            var dReading = $" ({Text} {Translator.Instance.GetRomaji(Text)})";
            string tags;
            if (StaticMethods.MainWindow.ViewModel.SettingsViewModel.TranslatorSettings.ShowTagsOnMouseover)
            {
                var dTags = jmDict.GetTags(DictionaryTerm.DefinitionTags, false);
                var tTags = jmDict.GetTags(DictionaryTerm.TermTags, false);
                tags = string.IsNullOrWhiteSpace(dTags) && string.IsNullOrWhiteSpace(tTags)
                    ? string.Empty
                    : $"{dTags} {tTags}{Environment.NewLine}";
            }
            else tags = string.Empty;
            return $"{DictionaryTerm.Expression}{dReading}{Environment.NewLine}{deinflectedReasons}{Environment.NewLine}{tags}{string.Join(", ", DictionaryTerm.Glossary)}";
        }

        public override string ToString() => $"{Text} ({string.Join(" ≪ ", Reasons.Select(r => r.Key))})";
    }

    internal struct DeinflectionReason
    {
        public string Key { get; set; }
        public string KanaIn { get; set; }
        public string KanaOut { get; set; }
        public string[] RulesIn { get; set; }
        public string[] RulesOut { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}
