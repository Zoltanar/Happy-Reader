using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Happy_Apps_Core;
using Newtonsoft.Json;

namespace Happy_Reader
{
	public class TranslatorSettings : SettingsJsonFile
	{
		private bool _googleUseCredential;
		// ReSharper disable StringLiteralTypo
		private string _googleCredentialPath = @"C:\Google\hrtranslate-credential.json";
		private string _freeUserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
		// ReSharper restore StringLiteralTypo
		private int _maxOutputSize = 700;
		private double _fontSize = 22d;
		private bool _captureClipboard;
		private string _originalTextFont;
		private string _romajiTextFont;
		private string _translatedTextFont;
		private bool _settingsViewState = true;
		private VerticalAlignment _outputVerticalAlignment = VerticalAlignment.Top;
		private TextAlignment _outputHorizontalAlignment = TextAlignment.Center;

		[JsonIgnore]
		public Action<bool> CaptureClipboardChanged;

		public bool GoogleUseCredential
		{
			get => _googleUseCredential;
			set
			{
				if (_googleUseCredential == value) return;
				_googleUseCredential = value;
				if (Loaded) Save();
			}
		}

		public string GoogleCredentialPath
		{
			get => _googleCredentialPath;
			set
			{
				if (_googleCredentialPath == value) return;
				_googleCredentialPath = value;
				if (Loaded) Save();
			}
		}

		public string FreeUserAgent
		{
			get => _freeUserAgent;
			set
			{
				if (_freeUserAgent == value) return;
				_freeUserAgent = value;
				if (Loaded) Save();
			}
		}

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
					OriginalColor = (new SolidColorBrush(newColor),value);
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

		[JsonIgnore]
		public (SolidColorBrush Color, string Name) OriginalColor { get; private set; } = (new(Colors.Ivory), "Ivory");

		[JsonIgnore]
		public (SolidColorBrush Color, string Name) RomajiColor { get; private set; } = (new(Colors.Pink), "Pink");

		[JsonIgnore]
		public (SolidColorBrush Color, string Name) TranslatedColor { get; private set; } = (new(Colors.GreenYellow), "GreenYellow");

		[JsonIgnore]
		public Brush ErrorColor => Brushes.Red;

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
	}
}