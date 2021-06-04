using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Happy_Apps_Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Happy_Reader
{
	public class JMDict
	{
		/// <summary>
		/// This list is ordered because when searching, we don't need to keep searching after strings have larger comparison value.
		/// </summary>
		private ReadOnlyCollection<Term> _terms = new(Array.Empty<Term>());

		public void ReadFiles(string folder, string prefix)
		{
			_terms = new ReadOnlyCollection<Term>(Array.Empty<Term>());
			if (string.IsNullOrWhiteSpace(folder))
			{
				StaticHelpers.Logger.ToFile("Folder string was empty.");
				return;
			}
			var directory = new DirectoryInfo(folder);
			if (!directory.Exists)
			{
				StaticHelpers.Logger.ToFile($"Folder did not exist: {folder}");
				return;
			}
			StaticHelpers.Logger.ToFile("Reading files for JMDict...");
			var loadingTerms = new List<Term>();
			foreach (var file in Directory.EnumerateFiles(folder, prefix))
			{
				try
				{
					ReadFile(loadingTerms, file);
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile($"Failed to read file for JMDict '{file}':", ex.ToString());
				}
			}
			StaticHelpers.Logger.ToFile("Finished reading files for JMDict.");
			_terms = new ReadOnlyCollection<Term>(loadingTerms.OrderBy(t => t.Expression, StringComparer.Ordinal).ToArray());
			StaticHelpers.Logger.ToFile($"JMDict: Loaded {_terms.Count:N0} terms.");
		}

		private void ReadFile(List<Term> loadingTerms, string filePath)
		{
			var text = File.ReadAllText(filePath);
			var terms = JsonConvert.DeserializeObject<List<List<object>>>(text) ?? new List<List<object>>();
			loadingTerms.AddRange(terms.Select(t => new Term(t)));
		}

		public List<Term> SearchExact(string term)
		{
			var list = new List<Term>();
			int index = 0;
			while (index < _terms.Count)
			{
				var compared = string.Compare(_terms[index].Expression, term, StringComparison.Ordinal);
				if (compared == 0) list.Add(_terms[index]);
				if (compared > 0) break;
				index++;
			}
			return list.OrderByDescending(t => t.Score).ToList();
		}

		public List<Term> Search(string term)
		{
			var list = new List<Term>();
			int index = 0;
			while (index < _terms.Count)
			{
				if (term.StartsWith(_terms[index].Expression)) list.Add(_terms[index]);
				if (string.Compare(_terms[index].Expression, term, StringComparison.Ordinal) > 0) break;
				index++;
			}
			return list.OrderByDescending(t => t.Expression.Length).ThenByDescending(t => t.Score).ToList();
		}

		public class Term
		{
			public Term(List<object> list)
			{
				Expression = (string)list[0];
				Reading = (string)list[1];
				DefinitionTags = (string)list[2];
				Rules = (string)list[3];
				Score = (long)list[4];
				Glossary = ((JArray)list[5]).ToObject<string[]>();
				Sequence = (long)list[6];
				TermTags = (string)list[7];
			}
			public string Expression { get; set; }
			public string Reading { get; set; }
			public string DefinitionTags { get; set; }
			public string Rules { get; set; }
			public long Score { get; set; }
			public string[] Glossary { get; set; }
			public long Sequence { get; set; }
			public string TermTags { get; set; }

			public string Detail()
			{
				var dReading = string.IsNullOrWhiteSpace(Reading)
					? string.Empty
					: $" ({Reading} {StaticMethods.MainWindow.ViewModel.Translator.GetRomaji(Reading)})";
				return $"{Expression}{dReading} {Environment.NewLine}{string.Join(", ", Glossary)}";
			}
		}
	}
}
