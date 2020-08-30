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
	public class OutputWindowViewModel : INotifyPropertyChanged
	{
		private bool _originalOn;
		private bool _romajiOn = true;
		private int _translationCounter;
		private DateTime _lastTranslated;
		private readonly RecentItemList<Translation> _translations = new RecentItemList<Translation>(10);

		public RichTextBox TextArea { get; set; }
		public MainWindow MainWindow { get; set; }
		public MainWindowViewModel MainViewModel { get; set; }
		public string IdText { get; set; }
		public bool TranslatePaused
		{
			get => MainViewModel?.TranslatePaused ?? false;
			set => MainViewModel.TranslatePaused = value;
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
			var input = TextArea.Selection.Text;
			Process.Start($"http://jisho.org/search/{input}");
		}

		private void AskJishoNotification()
		{
			var input = TextArea.Selection.Text;
			var task = System.Threading.Tasks.Task.Run(async () => await Jisho.Search(input));
			task.Wait();
			var text = task.Result.Data.Length < 1 ? "No results found." : task.Result.Data[0].Results();
			NotificationWindow.Launch("Ask Jisho", text);
		}

		public void Initialize(MainWindow mainWindow, RichTextBox outputTextBox)
		{
			TextArea = outputTextBox;
			MainWindow = mainWindow;
			MainViewModel = (MainWindowViewModel)mainWindow.DataContext;
			OnPropertyChanged(nameof(TranslatePaused));
		}

		private void AddEntry()
		{
			var input = TextArea.Selection.Text;
			var output = Kakasi.JapaneseToRomaji(input);
			if (output.Length > 0) output = char.ToUpper(output[0]) + output.Substring(1);
			output = output.Replace(" ", "");
			MainWindow.CreateAddEntryTab(new Entry(input, output) { SeriesSpecific = true });
		}

		private FlowDocument _flowDocument;

		public void UpdateOutput()
		{
			if (_flowDocument == null)
			{
				_flowDocument = new FlowDocument();
				TextArea.Document = _flowDocument;
			}
			_flowDocument.Blocks.Clear();
			_flowDocument.Blocks.AddRange(_translations.Items.SelectMany(x => x.GetBlocks(_originalOn, _romajiOn)));
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
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}