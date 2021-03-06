﻿using System;
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
		private string _googleCredentialPath = "C:\\Google\\hrtranslate-credential.json";
		private string _freeUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
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
		public HashSet<string> UntouchedStrings { get; set; } = new() {"","\r\n"};

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

		//todo make editable
		[JsonIgnore]
		public Brush OriginalColor => Brushes.Ivory;

		//todo make editable
		[JsonIgnore]
		public Brush RomajiColor => Brushes.Pink;

		//todo make editable
		[JsonIgnore]
		public Brush TranslationColor => Brushes.GreenYellow;

		//todo make editable
		[JsonIgnore]
		public Brush ErrorColor => Brushes.Red;

		public double FontSize
		{
			get => _fontSize;
			set
			{
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
			set
			{
				if (_outputHorizontalAlignment == value) return;
				_outputHorizontalAlignment = value;
				if (Loaded) Save();
			}
		}

		public VerticalAlignment OutputVerticalAlignment
		{
			get => _outputVerticalAlignment;
			set
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