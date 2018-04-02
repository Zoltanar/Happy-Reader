using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Reader.View;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public sealed class OutputWindowViewModel : INotifyPropertyChanged
	{
		private bool _originalOn = true;
		private bool _romajiOn = true;
		private int _translationCounter;
		private DateTime _lastTranslated;
		private readonly RecentItemList<Translation> _translations = new RecentItemList<Translation>(10);

		public RichTextBox TextArea { get; set; }
		public MainWindow MainWindow { get; set; }
		public MainWindowViewModel MainViewModel { get; set; }
		public string IdText { get; set; }
		public bool TranslateOn
		{
			get => MainViewModel?.TranslateOn ?? true;
			set => MainViewModel.TranslateOn = value;
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
		public ICommand AddEntryForText { get; set; }
		public ICommand AskJishoCommand { get; set; }
		public ICommand AskJishoNotificationCommand { get; set; }

		public OutputWindowViewModel()
		{
			AddEntryForText = new CommandHandler(AddEntry, true);
			AskJishoCommand = new CommandHandler(AskJisho, true);
			AskJishoNotificationCommand = new CommandHandler(AskJishoNotification, true);
		}

		private void AskJisho()
		{
			var input = TextArea.Selection.Text;
			Process.Start($"http://jisho.org/search/{input}");
		}

		private void AskJishoNotification()
		{
			var input = TextArea.Selection.Text;
			var task = System.Threading.Tasks.Task.Run(async()=> await Jisho.Search(input));
			task.Wait();
			var text = task.Result.Data.Length < 1 ? "No results found." : task.Result.Data[0].Results();
			new NotificationWindow("Ask Jisho", text).Show();
		}

		public void Initialize(MainWindow mainWindow, RichTextBox debugTextbox)
		{
			TextArea = debugTextbox;
			MainWindow = mainWindow;
			MainViewModel = (MainWindowViewModel)mainWindow.DataContext;
			OnPropertyChanged(nameof(TranslateOn));
		}

		private void AddEntry()
		{
			var input = TextArea.Selection.Text;
			var output = Kakasi.JapaneseToRomaji(input);
			if (output.Length > 0) output = char.ToUpper(output[0]) + output.Substring(1);
			MainWindow.CreateAddEntryTab(new Entry(input,output));
		}

		public void UpdateOutput()
		{
			var doc = new FlowDocument();
			doc.Blocks.AddRange(_translations.Items.SelectMany(x => x.GetBlocks(_originalOn, _romajiOn)));
			TextArea.Document = doc;
			IdText = $"TL Count: {_translationCounter}";
			OnPropertyChanged(nameof(IdText));
		}

		public void AddTranslation(Translation translation)
		{
			var last = _translations.Items.Count == 0 ? null : _translations.Items[0];
			var timeSinceLast = (DateTime.UtcNow - _lastTranslated).TotalMilliseconds;
			//if(last != null) System.Diagnostics.Debug.WriteLine($"Milliseconds since last TL= {timeSinceLast.TotalMilliseconds}, last.IsCharacterOnly={last.IsCharacterOnly}, translation.IsCharacterOnly={translation.IsCharacterOnly}");
			if (last != null && (
				(timeSinceLast < 1000 && last.IsCharacterOnly && !translation.IsCharacterOnly)
				|| timeSinceLast < 100))
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
			_lastTranslated = DateTime.UtcNow;
			_translationCounter++;
		}

		public event PropertyChangedEventHandler PropertyChanged;


		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private class CommandHandler : ICommand
		{
			private readonly Action _action;
			private readonly bool _canExecute;
			public CommandHandler(Action action, bool canExecute)
			{
				_action = action;
				_canExecute = canExecute;
			}

			public bool CanExecute(object parameter) => _canExecute;

			public void Execute(object parameter) => _action();

#pragma warning disable CS0067
			public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
		}

	}
}