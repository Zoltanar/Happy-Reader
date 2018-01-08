using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Happy_Reader.StaticMethods;

namespace Happy_Reader
{
    public class Translation
    {
        internal readonly List<(string Part, bool Translate)> Parts = new List<(string Part, bool Translate)>();
        private readonly List<string[]> _partResults = new List<string[]>();
        public readonly string[] Results = new string[8];
        public readonly string Original;
        public readonly string Romaji;
        public string Output => Results[7];

        public Translation(string original)
        {
            var s1Original = new StringBuilder(original);
            Translator.TranslateStageOne(s1Original, null);
            Original = s1Original.ToString();
            Romaji = Kakasi.JapaneseToRomaji(s1Original.ToString());
        }

        private Translation(string original, string romaji,string results)
        {
            Original = original;
            Romaji = romaji;
            for (int stage = 0; stage < Results.Length; stage++)
            {
                Results[stage] = results;
            }
        }

        public void TranslateParts()
        {
            try
            {
                foreach (var (part, translate) in Parts)
                {
                    if (!translate)
                    {
                        _partResults.Add(Enumerable.Repeat(part, 8).ToArray());
                        continue;
                    }
                    _partResults.Add(Translator.Translate(part));
                }
                for (int stage = 0; stage < 7; stage++)
                {
                    var stage1 = stage;
                    Results[stage] = string.Join(stage < 7 || _partResults[stage][0].Length == 1 ? string.Empty : " ", _partResults.Select(c => c[stage1]));
                }
                for (int part = 0; part < _partResults.Count; part++)
                {
                    var text = _partResults[part][7];
                    Results[7] += text;
                    if(part < _partResults.Count-1 && Parts[part].Translate && Parts[part+1].Translate) Results[7] += " ";
                }
                
            }
            catch (Exception ex)
            {
                LogToConsole(ex.Message); //todo log to file
            }

        }

        public static Translation Error(string error)
        {
            return new Translation(error, "",error);
        }
    }
}

