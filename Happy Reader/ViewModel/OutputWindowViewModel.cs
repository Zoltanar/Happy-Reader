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
		private int _translationCounter;
		private DateTime _lastOutputTime;
		private Func<string> _getSelectedText;
		private Action _scrollToBottom;
		private FlowDocument _flowDocument;
		private readonly RecentItemList<Translation> _translations = new(10);

		public string IdText { get; set; }
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
		public ICommand AskJishoNotificationCommand { get; set; }

		public OutputWindowViewModel()
		{
			AddEntryCommand = new CommandHandler(AddEntry, true);
			AskJishoCommand = new CommandHandler(AskJisho, true);
			AskJishoNotificationCommand = new CommandHandler(AskJishoNotification, true);
		}

		private void AskJisho()
		{
			var input = _getSelectedText();
			Process.Start($"http://jisho.org/search/{input}");
		}

		private void AskJishoNotification()
		{
			var input = _getSelectedText();
			var task = System.Threading.Tasks.Task.Run(async () => await Jisho.Search(input));
			task.Wait();
			var text = task.Result.Data.Length < 1 ? "No results found." : task.Result.Data[0].Results();
			NotificationWindow.Launch("Ask Jisho", text);
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
					GameId = StaticMethods.MainWindow.ViewModel.UserGame?.VNID,
					Type = EntryType.Output
				};
			}
			else
			{
				var output = Kakasi.JapaneseToRomaji(input);
				if (output.Length > 0) output = char.ToUpper(output[0]) + output.Substring(1);
				entry = new Entry
				{
					Input = input,
					Output = output.Replace(" ", ""),
					SeriesSpecific = true,
					GameId = StaticMethods.MainWindow.ViewModel.UserGame?.VNID,
					Type = EntryType.Name
				};
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
			IdText = $"TL Count: {_translationCounter}";
			OnPropertyChanged(nameof(IdText));
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
				_translationCounter++;
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
			_translationCounter++;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}