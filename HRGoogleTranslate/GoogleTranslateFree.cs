using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRGoogleTranslate
{
	[UsedImplicitly]
	public class GoogleTranslateFree : ITranslator
	{
		private const string GoogleDetectedString = @"Our systems have detected unusual traffic from your computer network.  This page checks to see if it&#39;s really you sending the requests, and not a robot.";
		private const string GoogleDetectedString2 = @"This page appears when Google automatically detects requests coming from your computer network";
		private const string UserAgentPropertyKey = "User Agent";
		private static readonly Regex CombineEmptyLinesRegex = new(@"^(\s*\n){2,}");
		private const string TranslateFreeUrl = @"https://translate.googleapis.com/translate_a/single?client=gtx&sl=ja&tl=en&dt=t&q="; //todo make editable
		private static HttpClient _freeClient = new();
		public string Error { get; set; }
		public string Version => "1.0";
		public string SourceName => "Google Translate Free";
		public IReadOnlyDictionary<string, Type> Properties { get; } = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>()
		{
			{
				UserAgentPropertyKey, typeof(string)
			}
		});
		private FreeSettings Settings { get; set; }
		public void Initialise()
		{
			SetUserAgent(Settings.FreeUserAgent);
		}
		
		public void LoadProperties(string filePath)
		{
			Settings = SettingsJsonFile.Load<FreeSettings>(filePath);
			SetUserAgent(Settings.FreeUserAgent);
		}

		public void SaveProperties(string filePath)
		{
			//done automatically via Settings object.
		}

		public void SetProperty(string propertyKey, object value)
		{
			if (propertyKey != UserAgentPropertyKey || value is not string userAgentString) return;
			SetUserAgent(userAgentString);
		}

		public object GetProperty(string propertyKey)
		{
			if (propertyKey != UserAgentPropertyKey) throw new NotSupportedException($"Property not supported: '{propertyKey}'");
			return Settings.FreeUserAgent;
		}

		private void SetUserAgent(string userAgent)
		{
			_freeClient = new HttpClient();
			Settings.FreeUserAgent = userAgent;
			_freeClient.DefaultRequestHeaders.Clear();
			_freeClient.DefaultRequestHeaders.Add(@"user-agent", userAgent);
		}

		public bool Translate(string input, out string output)
		{
			try
			{
				int attempts = 0;
				do
				{
					attempts++;
					var jsonString = GetPostResultAsString(_freeClient, TranslateFreeUrl + Uri.EscapeDataString(input));
					if (jsonString.Contains(GoogleDetectedString) || jsonString.Contains(GoogleDetectedString2))
					{
						if (attempts < 2)
						{
							SetUserAgent(Settings.FreeUserAgent);
							continue;
						}
						var extracted = ExtractText(jsonString);
						output = $"Failed to translate, detected by Google: {extracted}";
						return false;
					}

					return TryDeserializeJsonResponse(jsonString, out output);
				} while (true);
			}
			catch (Exception ex)
			{
				//todo if result is html, extract visible text
				output = $"Failed to translate. ({ex.Message})";
				return false;
			}
		}

		private static bool AnyParentHasAttribute(HtmlNode startNode, string name, Func<string, bool> function)
		{
			var node = startNode;
			while (node.ParentNode != null)
			{
				if (node.ParentNode.Attributes.Any(a => a.Name == name && function(a.Value))) return true;
				node = node.ParentNode;
			}
			return false;
		}

		private static string ExtractText(string html)
		{
			// Where m_whitespaceRegex is a Regex with [\s].
			// Where sampleHtmlText is a raw HTML string.

			var extractedSampleText = new StringBuilder();
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);

			if (doc.DocumentNode == null) return string.Empty;
			foreach (var node in doc.DocumentNode.Descendants("script")
				.Concat(doc.DocumentNode.Descendants("style"))
				.Concat(doc.DocumentNode.Descendants("head")).ToArray())
			{
				node.Remove();
			}
			var allTextNodes = doc.DocumentNode.SelectNodes("//text()");
			if (allTextNodes != null && allTextNodes.Count > 0)
			{
				foreach (var node in allTextNodes)
				{
					if (string.IsNullOrWhiteSpace(node.InnerText) || AnyParentHasAttribute(node, "style", v => v.Contains("display:none"))) continue;
					extractedSampleText.Append(node.InnerText);
				}
			}
			var text = extractedSampleText.ToString();
			var finalText = CombineEmptyLinesRegex.Replace(text, "\n").Trim();
			return finalText;
		}

		private static bool TryDeserializeJsonResponse(string jsonString, out string translated)
		{
			translated = null;
			try
			{
				var jArray = JsonConvert.DeserializeObject<JArray>(jsonString) ?? throw new InvalidOperationException("Json Response was null.");
				var translatedObject = jArray[0][0];
				Debug.Assert(translatedObject != null, nameof(translatedObject) + " != null");
				translated = (translatedObject[0] ?? throw new InvalidOperationException("Json object was not o expected format.")).Value<string>();
				return true;
			}
			catch (Exception ex)
			{
				translated = $"Failed to deserialize: {ex}";
				return false;
			}
		}

		private static string GetPostResultAsString(HttpClient client, string url)
		{
			var task = client.PostAsync(url, null);
			task.Wait(2500);
			var task2 = task.Result.Content.ReadAsStringAsync();
			task2.Wait(2500);
			var jsonString = task2.Result;
			return jsonString;
		}

		private class FreeSettings : SettingsJsonFile
		{
			// ReSharper disable once StringLiteralTypo
			private string _freeUserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

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
		}
	}
}
