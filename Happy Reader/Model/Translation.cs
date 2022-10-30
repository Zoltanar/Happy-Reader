using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Happy_Apps_Core;
using Happy_Reader.Database;
using Happy_Reader.TranslationEngine;
using Happy_Reader.View;

namespace Happy_Reader
{
	public class Translation
	{
		internal readonly List<(string Part, bool Translate)> Parts = new();
		private readonly List<TranslationResults> _partResults = new();
		private readonly List<Entry> _entriesUsedStageOne = new();
		public readonly string[] Results = new string[8];
		public readonly string Untouched;
		public readonly string Original;
		public string Romaji { get; }
		public string Output => Results[7];
		public bool IsCharacterOnly { get; } //todo change this
		public Paragraph OriginalBlock { get; private set; }
		public Paragraph RomajiBlock { get; private set; }
		public Paragraph TranslatedBlock { get; private set; }
		public Paragraph ErrorBlock { get; private set; }
		public bool IsError { get; private set; }

		public Translation(string original, bool modify)
		{
			Untouched = original;
			var stageOneResult = new TranslationResults(true);
			if (!modify)
			{
				for (int i = 0; i < Results.Length; i++) Results[i] = original;
				Original = original;
				Romaji = original;
				return;
			}
			var originalSb = new StringBuilder(original);
			Translator.Instance.TranslateStageOne(originalSb, stageOneResult);
			Original = originalSb.ToString();
			var romajiSb1 = new StringBuilder(original);
			Translator.Instance.GetRomajiFiltered(romajiSb1, stageOneResult);
			Romaji = romajiSb1.ToString();
			_entriesUsedStageOne.AddRange(stageOneResult.EntriesUsed.SelectMany(i => i));
			IsCharacterOnly = Original.IndexOfAny(new[] { '「', '」' }) < 0 && Original.Length < 10;
		}


		public Translation(Translation first, Translation second)
		{
			Original = $"{first.Original} {second.Original}";
			Romaji = $"{first.Romaji} {second.Romaji}";
			for (int stage = 0; stage < Results.Length; stage++)
			{
				Results[stage] = $"{first.Results[stage]} {second.Results[stage]}";
			}
			IsCharacterOnly = false;
		}

		public IEnumerable<Entry> GetEntriesUsed()
		{
			return _partResults.Where(pr => pr.EntriesUsed != null)
				.SelectMany(pr => pr.EntriesUsed.SelectMany(eu => eu))
				.Concat(_entriesUsedStageOne).Distinct();
		}

		public void TranslateParts(bool saveEntriesUsed)
		{
			try
			{
				foreach (var (part, translate) in Parts)
				{
					if (!translate)
					{
						_partResults.Add(new TranslationResults(part));
						continue;
					}
					_partResults.Add(Translator.Instance.TranslatePart(part, saveEntriesUsed));
				}
				for (int stage = 0; stage < 7; stage++)
				{
					var stage1 = stage;
					Results[stage] = string.Join(string.Empty, _partResults.Select(c => c[stage1]));
				}
				for (int part = 0; part < _partResults.Count; part++)
				{
					var text = _partResults[part][7];
					Results[7] += text;
					if (part < _partResults.Count - 1 && Parts[part].Translate && Parts[part + 1].Translate) Results[7] += " ";
				}
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex.Message);
				throw;
			}
		}

		public void SetParagraphs()
		{
			if (IsError)
			{
				var errorP = new Paragraph(new Run(Original));
				errorP.Inlines.FirstInline.Foreground = StaticMethods.Settings.TranslatorSettings.ErrorColor;
				SetFont(errorP, StaticMethods.Settings.TranslatorSettings.TranslatedTextFont);
				ErrorBlock = errorP;
				return;
			}
			var originalP = new Paragraph(new Run(Original));
			originalP.Inlines.FirstInline.Foreground = StaticMethods.Settings.TranslatorSettings.OriginalColor.Color;
			SetFont(originalP, StaticMethods.Settings.TranslatorSettings.OriginalTextFont);
			OriginalBlock = originalP;
			if (!string.IsNullOrWhiteSpace(Romaji) && !Romaji.Equals(Original))
			{
				var romajiP = new Paragraph(new Run(Romaji));
				romajiP.Inlines.FirstInline.Foreground = StaticMethods.Settings.TranslatorSettings.RomajiColor.Color;
				SetFont(romajiP, StaticMethods.Settings.TranslatorSettings.RomajiTextFont);
				RomajiBlock = romajiP;
			}
			if (!string.IsNullOrWhiteSpace(Output) && !Output.Equals(Original))
			{
				var translatedP = new Paragraph(new Run(Output));
				translatedP.Inlines.FirstInline.Foreground = StaticMethods.Settings.TranslatorSettings.TranslatedColor.Color;
				SetFont(translatedP, StaticMethods.Settings.TranslatorSettings.TranslatedTextFont);
				TranslatedBlock = translatedP;
			}
		}

		private static void SetFont(TextElement block, string fontName)
		{
			if (string.IsNullOrWhiteSpace(fontName)) return;
			if (!StaticMethods.FontsInstalled.TryGetValue(fontName, out var fontFamily)) return;
			block.FontFamily = fontFamily;
		}

		public List<Paragraph> GetBlocks(bool original, bool romaji, bool translation, bool separator)
		{
			var blocks = new List<Paragraph>();
			if (IsError) blocks.Add(ErrorBlock);
			if (original && OriginalBlock != null) blocks.Add(OriginalBlock);
			if (romaji && RomajiBlock != null) blocks.Add(RomajiBlock);
			if (translation && TranslatedBlock != null) blocks.Add(TranslatedBlock);
			foreach (Paragraph block in blocks)
			{
				block.Margin = new Thickness(0);
				block.TextAlignment = StaticMethods.Settings.TranslatorSettings.OutputHorizontalAlignment;
				block.FontSize = StaticMethods.Settings.TranslatorSettings.FontSize;
				block.Tag = this;
			}
			if (!blocks.Any()) return blocks;
            if (!separator) return blocks;
			var spacer = new Paragraph(new Run("￣￣￣"));
			spacer.Inlines.FirstInline.Foreground = Theme.OutputSpacerForeground;
			spacer.Margin = new Thickness(0);
			spacer.TextAlignment = StaticMethods.Settings.TranslatorSettings.OutputHorizontalAlignment;
			spacer.FontSize = 3d;
			spacer.Padding = new Thickness(0);
			blocks.Add(spacer);
			return blocks;
		}

		public static Translation Error(string message)
		{
			return new(message, false) { IsError = true };
		}

	}
}

