using System;
using System.Collections.Generic;
using System.Linq;

namespace Happy_Reader.TranslationEngine;

internal class DeinflectedTerm : JMDict.ITerm
{
    public JMDict.Term DictionaryTerm { get; set; }
    public string Expression { get; }
    public string Text { get;  }
    private string ReasonsText { get; }
    public long Score => 0;
    public bool Completed { get; set; }
    public List<DeinflectionReason> ReasonsList { get; }
    
    public DeinflectedTerm(JMDict.Term term, string text, bool completed, List<DeinflectionReason> reasonsList)
    {
        DictionaryTerm = term;
        Expression = DictionaryTerm.Expression;
        Text = text;
        Completed = completed;
        ReasonsList = reasonsList;
        ReasonsText = $"{string.Join(" ≪ ", reasonsList.Select(r => r.Key))}";
    }

    public DeinflectedTerm(string expression, string text, string reasonsText)
    {
        Expression = expression;
        Text = text;
        ReasonsText = reasonsText;
    }

    public string Detail(JMDict jmDict)
    {
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
        return $"{Expression}{dReading}{Environment.NewLine}{ReasonsText}{Environment.NewLine}{tags}{string.Join(", ", DictionaryTerm.Glossary)}";
    }
    
    public override string ToString() => $"{Text} ({ReasonsText})";
}