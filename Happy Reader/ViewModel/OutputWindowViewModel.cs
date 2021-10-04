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
using Happy_Reader.TranslationEngine;
using Happy_Reader.View;
using IthVnrSharpLib;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public class OutputWindowViewModel : INotifyPropertyChanged
	{
		private bool _originalOn;
		private bool _romajiOn;
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

		public bool SettingsOn
		{
			get => StaticMethods.Settings.TranslatorSettings.SettingsViewState;
			set => StaticMethods.Settings.TranslatorSettings.SettingsViewState = value;
		}

		public bool OriginalOn
		{
			get => _originalOn;
			set
			{
				_originalOn = value;
				StaticMethods.Settings.TranslatorSettings.OutputOriginal = value;
				UpdateOutput();
			}
		}
		public bool RomajiOn
		{
			get => _romajiOn;
			set
			{
				_romajiOn = value;
				StaticMethods.Settings.TranslatorSettings.OutputRomaji = value;
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

		private void SearchOnDictionary(string input)
		{
			var offlineDict = Translator.Instance.OfflineDictionary;
			var success = offlineDict.SearchOuter(input, out var result);
			var text = !success ? "No results found." : result;
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
				if (game?.VNID.HasValue ?? false) entry.SetGameId(game.VNID, false);
				else if (game != null) entry.SetGameId((int)game.Id, true);
			}
			else
			{
				var output = Translator.Instance.GetRomaji(input);
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

		public bool IsClipboardCopy(TextOutputEventArgs textOutput)
		{
			var translation = _translations.Items.FirstOrDefault();
			var result = translation != null && textOutput.FromClipboard && textOutput.Text.Equals(translation.Untouched);
			return result;
		}
	}
}