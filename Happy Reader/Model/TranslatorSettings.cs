using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using IthVnrSharpLib.Properties;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class TranslatorSettings : SettingsJsonFile
	{
		private static readonly NoTranslator NoTranslator = new();
		[JsonIgnore] public static IEnumerable<string> RomajiTranslators => Translator.RomajiTranslators.Keys;
		[JsonIgnore] public ICollection<ITranslator> Translators { get; set; } = new List<ITranslator>() { NoTranslator };

		private int _maxOutputSize = 700;
		private double _fontSize = 22d;
		private bool _captureClipboard;
		private bool _mouseoverDictionary;
		private string _originalTextFont;
		private string _romajiTextFont;
		private string _translatedTextFont;
		private string _offlineDictionaryFolder;
		private string _selectedTranslatorName;
		private string _selectedRomajiTranslator = RomajiTranslators.First();
		private bool _settingsViewState = true;
		private VerticalAlignment _outputVerticalAlignment = VerticalAlignment.Top;
		private TextAlignment _outputHorizontalAlignment = TextAlignment.Center;

		[JsonIgnore] public Action<bool> CaptureClipboardChanged;
		[JsonIgnore] public Action UpdateOfflineDictionaryFolder;

		//todo make editable
		public HashSet<string> UntouchedStrings { get; set; } = new() { "", "\r\n" };

		//not sure if this has to be made editable in GUI
		public string ExclusiveSeparators { get; set; } = "『「」』…♥";
		public string InclusiveSeparators { get; set; } = "。？！";

		public string OriginalTextFont
		{
			get => _originalTextFont;
			set
			{
				if (_originalTextFont == value) return;
				if (!string.IsNullOrWhiteSpace(value) && !StaticMethods.FontsInstalled.ContainsKey(value))
				{
					StaticHelpers.Logger.ToFile($"Did not find font with name '{value}'");
				}
				_originalTextFont = value;
				if (Loaded) Save();
			}
		}

		public string RomajiTextFont
		{
			get => _romajiTextFont;
			set
			{
				if (_romajiTextFont == value) return;
				if (!string.IsNullOrWhiteSpace(value) && !StaticMethods.FontsInstalled.ContainsKey(value))
				{
					StaticHelpers.Logger.ToFile($"Did not find font with name '{value}'");
				}
				_romajiTextFont = value;
				if (Loaded) Save();
			}
		}

		public string TranslatedTextFont
		{
			get => _translatedTextFont;
			set
			{
				if (_translatedTextFont == value) return;
				if (!string.IsNullOrWhiteSpace(value) && !StaticMethods.FontsInstalled.ContainsKey(value))
				{
					StaticHelpers.Logger.ToFile($"Did not find font with name '{value}'");
				}
				_translatedTextFont = value;
				if (Loaded) Save();
			}
		}

		public bool SettingsViewState
		{
			get => _settingsViewState;
			set
			{
				if (_settingsViewState == value) return;
				_settingsViewState = value;
				if (Loaded) Save();
			}
		}

		public string OriginalColorString
		{
			get => OriginalColor.Name;
			set
			{
				try
				{
					// ReSharper disable once PossibleNullReferenceException
					var newColor = (Color)ColorConverter.ConvertFromString(value);
					if (OriginalColor.Color.Color == newColor) return;
					OriginalColor = (new SolidColorBrush(newColor), value);
				}
				catch
				{
					OriginalColor = (new SolidColorBrush(Colors.Ivory), "Ivory");
				}
				if (Loaded)
				{
					Save();
				}
			}
		}

		public string RomajiColorString
		{
			get => RomajiColor.Name;
			set
			{
				try
				{
					// ReSharper disable once PossibleNullReferenceException
					var newColor = (Color)ColorConverter.ConvertFromString(value);
					if (RomajiColor.Color.Color == newColor) return;
					RomajiColor = (new SolidColorBrush(newColor), value);
				}
				catch
				{
					OriginalColor = (new SolidColorBrush(Colors.Pink), "Pink");
				}
				if (Loaded)
				{
					Save();
				}
			}
		}

		public string TranslatedColorString
		{
			get => TranslatedColor.Name;
			set
			{
				try
				{
					// ReSharper disable once PossibleNullReferenceException
					var newColor = (Color)ColorConverter.ConvertFromString(value);
					if (TranslatedColor.Color.Color == newColor) return;
					TranslatedColor = (new SolidColorBrush(newColor), value);
				}
				catch
				{
					TranslatedColor = (new SolidColorBrush(Colors.GreenYellow), "GreenYellow");
				}
				if (Loaded)
				{
					Save();
				}
			}
		}

		[JsonIgnore] public (SolidColorBrush Color, string Name) OriginalColor { get; private set; } = (new(Colors.Ivory), "Ivory");
		[JsonIgnore] public (SolidColorBrush Color, string Name) RomajiColor { get; private set; } = (new(Colors.Pink), "Pink");
		[JsonIgnore] public (SolidColorBrush Color, string Name) TranslatedColor { get; private set; } = (new(Colors.GreenYellow), "GreenYellow");
		[JsonIgnore] public Brush ErrorColor => Brushes.Red;

		public double FontSize
		{
			get => _fontSize;
			set
			{
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (_fontSize == value) return;
				_fontSize = value;
				if (Loaded) Save();
			}
		}

		public int MaxOutputSize
		{
			get => _maxOutputSize;
			set
			{
				if (_maxOutputSize == value) return;
				_maxOutputSize = value;
				if (Loaded) Save();
			}
		}

		public bool CaptureClipboard
		{
			get => _captureClipboard;
			set
			{
				if (_captureClipboard == value) return;
				_captureClipboard = value;
				if (Loaded)
				{
					Save();
					CaptureClipboardChanged?.Invoke(value);
				}
			}
		}

		public bool MouseoverDictionary
		{
			get => _mouseoverDictionary;
			set
			{
				if (_mouseoverDictionary == value) return;
				_mouseoverDictionary = value;
				if (Loaded)
				{
					Save();
				}
			}
		}
		public TextAlignment OutputHorizontalAlignment
		{
			get => _outputHorizontalAlignment;
			private set
			{
				if (_outputHorizontalAlignment == value) return;
				_outputHorizontalAlignment = value;
				if (Loaded) Save();
			}
		}

		public VerticalAlignment OutputVerticalAlignment
		{
			get => _outputVerticalAlignment;
			private set
			{
				if (_outputVerticalAlignment == value) return;
				_outputVerticalAlignment = value;
				if (Loaded) Save();
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		[UsedImplicitly] public string SelectedTranslatorName
		{
			get => _selectedTranslatorName;
			set
			{
				if (_selectedTranslatorName == value) return;
				_selectedTranslatorName = value;
				if (Loaded) Save();
			}
		}
		
		public string SelectedRomajiTranslator
		{
			get => _selectedRomajiTranslator;
			set
			{
				if (_selectedRomajiTranslator == value) return;
				if (!RomajiTranslators.Any(t => t.Equals(value, StringComparison.OrdinalIgnoreCase))) return;
				_selectedRomajiTranslator = value;
				if (Loaded) Save();
			}
		}

		[JsonIgnore]
		public ITranslator SelectedTranslator
		{
			get => Translators.FirstOrDefault(t => t.SourceName == _selectedTranslatorName) ?? NoTranslator;
			set
			{
				var selectedTranslator = SelectedTranslator;
				if (selectedTranslator == value) return;
				//save previous translator properties
				selectedTranslator.SaveProperties(StaticHelpers.GetTranslatorSettings(selectedTranslator.SourceName));
				//load and initialise new translator
				value.LoadProperties(StaticHelpers.GetTranslatorSettings(value.SourceName));
				value.Initialise();
				SelectedTranslatorName = value.SourceName;
				if (Loaded) Save();
			}
		}

		public string OfflineDictionaryFolder
		{
			get => _offlineDictionaryFolder;
			set
			{
				if (_offlineDictionaryFolder == value) return;
				_offlineDictionaryFolder = value;
				UpdateOfflineDictionaryFolder?.Invoke();
				if (Loaded) Save();
			}
		}

		public TextAlignment SetNextHorizontalAlignmentState()
		{
			return OutputHorizontalAlignment switch
			{
				TextAlignment.Left => OutputHorizontalAlignment = TextAlignment.Center,
				TextAlignment.Center => OutputHorizontalAlignment = TextAlignment.Right,
				TextAlignment.Right => OutputHorizontalAlignment = TextAlignment.Left,
				_ => OutputHorizontalAlignment = TextAlignment.Left
			};
		}
		public VerticalAlignment SetNextVerticalAlignmentState()
		{
			return OutputVerticalAlignment switch
			{
				VerticalAlignment.Top => OutputVerticalAlignment = VerticalAlignment.Bottom,
				VerticalAlignment.Bottom => OutputVerticalAlignment = VerticalAlignment.Top,
				_ => OutputVerticalAlignment = VerticalAlignment.Top,
			};
		}


		public void LoadTranslationPlugins(string folder)
		{
			var directory = new DirectoryInfo(folder);
			if (!directory.Exists) return;
			Translators.Clear();
			Translators.Add(NoTranslator);
			foreach (var file in directory.GetFiles("*.dll", SearchOption.TopDirectoryOnly))
			{
				try
				{
					LoadPlugin(Translators, file);
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile($"Failed to load translation plugin from {file}", ex.ToString());
				}
			}
			SelectedTranslator.LoadProperties(StaticHelpers.GetTranslatorSettings(SelectedTranslator.SourceName));
			SelectedTranslator.Initialise();
			OnPropertyChanged(nameof(Translators));
			OnPropertyChanged(nameof(SelectedTranslator));
		}

		private void LoadPlugin(ICollection<ITranslator> translators, FileInfo file)
		{
			var assembly = Assembly.LoadFile(file.FullName);
			var translatorsDefined = assembly.ExportedTypes.Where(t => t.GetInterfaces().Any(i => i == typeof(ITranslator)))
				.ToList();
			foreach (var translatorType in translatorsDefined)
			{
				var translator = (ITranslator)translatorType.GetConstructors().First().Invoke(new object[0]);
				translator.LoadProperties(StaticHelpers.GetTranslatorSettings(translator.SourceName));
				translators.Add(translator);
			}
		}

	}
	public class NoTranslator : ITranslator
	{
		public string Version => "1.0";
		public string SourceName => "No Translator";

		public IReadOnlyDictionary<string, Type> Properties { get; } = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>());
		public string Error { get; set; } = "No Translator Selected";

		public void Initialise() {/*ignore*/}

		public void LoadProperties(string filePath) {/*ignore*/}

		public void SaveProperties(string filePath) {/*ignore*/}

		public void SetProperty(string propertyName, object value) => throw new NotSupportedException();
		public object GetProperty(string propertyName) => throw new NotSupportedException();

		public bool Translate(string input, out string output)
		{
			output = input;
			return false;
		}
	}

}