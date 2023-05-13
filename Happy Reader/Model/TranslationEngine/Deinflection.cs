using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Happy_Apps_Core;
using Happy_Reader.Model.TranslationEngine;
using Newtonsoft.Json;
using static Happy_Reader.JMDict;

namespace Happy_Reader.TranslationEngine
{
    internal class Deinflection
    {
        private DeinflectionReason[] _deinflectionReasons = Array.Empty<DeinflectionReason>();
        private readonly HashSet<string> _deinflectedTermsSet = new();
        internal int ReasonsCount => _deinflectionReasons.Length;
        private const int MaxDeinflectionDepth = 3;

        private readonly DeinflectionDatabase _database = new(StaticMethods.DeinflectionsDatabaseFile);

        internal List<DeinflectedTerm> GetDeinflections(Term term)
        {
            return _database.GetDeinflections(term);
        }

        private void SaveDeinflections(Term term, SQLiteTransaction transaction)
        {
            if (_deinflectedTermsSet.Contains(term.Expression)) return;
            CreateDeinflections(term, transaction);
            _deinflectedTermsSet.Add(term.Expression);

        }

        private void PreLoad(Term[] dictionaryTerms)
        {
            StaticHelpers.Logger.ToFile("Pre-loading Deinflections...");
            _database.Connection.Open();
            var transaction = _database.Connection.BeginTransaction();
            try
            {
                foreach (var term in dictionaryTerms)
                {
                    SaveDeinflections(term, transaction);
                    if (_deinflectedTermsSet.Count % 1000 == 0)
                    {
                        StaticHelpers.Logger.ToDebug($"Processed {_deinflectedTermsSet.Count} unique terms...");
                        transaction.Commit(); //commit in chunks of 1000 unique terms
                        transaction = _database.Connection.BeginTransaction();
                    }
                }
                transaction.Commit();
            }
            finally
            {
                _database.Connection.Close();
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
            if (_database.IsPopulated()) return;
            var watch = Stopwatch.StartNew();
            _deinflectionReasons = deinflections.SelectMany(p => p.Value.Select(v =>
            {
                v.Key = p.Key;
                return v;
            })).ToArray();
            PreLoad(dictionaryTerms);
            _database.SaveReasonMapTime(DateTime.UtcNow);
            watch.Stop();
            StaticHelpers.Logger.ToFile($"Created Deinflections database in {watch.Elapsed:g}");
        }

        private void CreateDeinflections(Term term, SQLiteTransaction transaction)
        {
            var withTermRules = _deinflectionReasons.Where(d => term.Expression.EndsWith(d.KanaOut) && d.RulesOut.Contains(term.Rules)).ToList();
            var deinflections = new List<DeinflectedTerm>();
            foreach (var deinflectionReason in withTermRules)
            {
                var result = deinflectionReason.KanaOut.Length == 0 ? term.Expression + deinflectionReason.KanaIn : term.Expression.Replace(deinflectionReason.KanaOut, deinflectionReason.KanaIn);
                var deinflectedTerm = new DeinflectedTerm(term, result, false, new List<DeinflectionReason> { deinflectionReason });
                deinflections.Add(deinflectedTerm);
                _database.SaveDeinflection(deinflectedTerm, transaction);
            }
            bool anyAdded;
            do
            {
                anyAdded = false;
                foreach (var deinflectedTerm in deinflections.ToArray())
                {
                    if (deinflectedTerm.Completed) continue;
                    var reasons = deinflectedTerm.ReasonsList;
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
                        var innerDeinflectedTerm = new DeinflectedTerm(term, result, false, new List<DeinflectionReason>(reasons) { deinflectionReason });
                        deinflections.Add(innerDeinflectedTerm);
                        _database.SaveDeinflection(innerDeinflectedTerm, transaction);
                        anyAdded = true;
                    }
                    deinflectedTerm.Completed = true;
                }
            } while (anyAdded);
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
}
