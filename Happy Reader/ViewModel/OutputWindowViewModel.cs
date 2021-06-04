using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Reader.View;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public class OutputWindowViewModel : INotifyPropertyChanged
	{
		private bool _originalOn;
		private bool _romajiOn = true;
		private DateTime _lastOutputTime;
		private Func<string> _getSelectedText;
		private Action _scrollToBottom;
		private FlowDocument _flowDocument;
		private readonly RecentItemList<Translation> _translations = new(10);
		
		public bool TranslatePaused
		{
			get => StaticMethods.MainWindow.ViewModel?.TranslatePaused ?? false;
			set => StaticMethods.MainWindow.ViewModel.TranslatePaused = value;
		}
		public bool OriginalOn
		{
			get => _originalOn;
			set
			{
				_originalOn = value;
				UpdateOutput();
			}
		}
		public bool RomajiOn
		{
			get => _romajiOn;
			set
			{
				_romajiOn = value;
				UpdateOutput();
			}
		}
		public bool DisableCombine { get; set; }
		public ICommand AddEntryCommand { get; set; }
		public ICommand AskJishoCommand { get; set; }
		public ICommand SearchOnDictionaryCommand { get; set; }

		public OutputWindowViewModel()
		{
			AddEntryCommand = new CommandHandler(AddEntry, true);
			AskJishoCommand = new CommandHandler(AskJisho, true);
			SearchOnDictionaryCommand = new CommandHandler(() => SearchOnDictionary(_getSelectedText()), true);
		}

		private void AskJisho()
		{
			var input = _getSelectedText();
			Process.Start($"http://jisho.org/search/{input}");
		}

		private bool useJishoDictionary = false;

		private void SearchOnDictionary(string input)
		{
			string text;
			if (useJishoDictionary)
			{
				var task = System.Threading.Tasks.Task.Run(async () => await Jisho.Search(input));
				task.Wait();
				text = task.Result.Data.Length < 1 ? "No results found." : task.Result.Data[0].Results();
			}
			else
			{
				var results = StaticMethods.MainWindow.ViewModel.Translator.OfflineDictionary.Search(input);
				text = results.Count < 1 ? "No results found." : string.Join(Environment.NewLine, results.Select(c=>c.Detail()));
			}
			NotificationWindow.Launch("Dictionary", text);
		}

		public void Initialize(Func<string> getSelectedText, FlowDocument flowDocument, Action scrollToBottom)
		{
			_getSelectedText = getSelectedText;
			_flowDocument = flowDocument;
			_scrollToBottom = scrollToBottom;
			OnPropertyChanged(nameof(TranslatePaused));
		}


		private void AddEntry()
		{
			var input = _getSelectedText().Trim();
			Entry entry;
			if (Translator.LatinOnlyRegex.IsMatch(input))
			{
				entry = new Entry
				{
					Input = input,
					Output = input,
					SeriesSpecific = true,
					Type = EntryType.Output
				};
				var game = StaticMethods.MainWindow.ViewModel.UserGame;
				if(game?.VNID.HasValue ?? false) entry.SetGameId(game.VNID, false);
				else if(game != null) entry.SetGameId((int)game.Id, true);
			}
			else
			{
				var output = StaticMethods.MainWindow.ViewModel.Translator.GetRomaji(input);
				if (output.Length > 0) output = char.ToUpper(output[0]) + output.Substring(1);
				entry = new Entry
				{
					Input = input,
					Output = output.Replace(" ", ""),
					SeriesSpecific = true,
					Type = EntryType.Name
				};
				var game = StaticMethods.MainWindow.ViewModel.UserGame;
				if (game?.VNID.HasValue ?? false) entry.SetGameId(game.VNID, false);
				else if (game != null) entry.SetGameId((int)game.Id, true);
			}
			StaticMethods.MainWindow.CreateAddEntriesTab(new List<Entry> { entry });
		}

		public void UpdateOutput()
		{
			_flowDocument.Blocks.Clear();
			IEnumerable<Paragraph> items = _translations.Items.SelectMany(t => t.GetBlocks(_originalOn, _romajiOn));
			var fromBottom = StaticMethods.Settings.TranslatorSettings.OutputVerticalAlignment == VerticalAlignment.Bottom;
			if (fromBottom) items = items.Reverse();
			_flowDocument.Blocks.AddRange(items);
			if (fromBottom) _scrollToBottom?.Invoke();
		}

		public void AddTranslation(Translation translation)
		{
			var last = _translations.Items.Count == 0 ? null : _translations.Items[0];
			//remove error messages.
			if (last?.IsError ?? false) _translations.Items.Remove(last);
			if (translation.IsError)
			{
				translation.SetParagraphs();
				_translations.Add(translation);
				_lastOutputTime = DateTime.UtcNow;
				return;
			}
			var combine = false;
			if (DisableCombine) DisableCombine = false;
			else
			{
				var timeSinceLast = (DateTime.UtcNow - _lastOutputTime).TotalMilliseconds;
				if (last != null && (
					(timeSinceLast < 1000 && last.IsCharacterOnly && !translation.IsCharacterOnly)
					|| timeSinceLast < 100)) combine = true;
			}
			if (combine)
			{
				var combined = new Translation(last, translation);
				combined.SetParagraphs();
				_translations.Items[0] = combined;
			}
			else
			{
				if (last == null || !(translation.Original == last.Original && translation.Output == last.Output))
				{
					translation.SetParagraphs();
					_translations.Add(translation);
				}
			}
			_lastOutputTime = DateTime.UtcNow;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}