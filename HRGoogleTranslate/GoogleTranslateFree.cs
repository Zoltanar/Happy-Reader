using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Happy_Apps_Core.Translation;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HRGoogleTranslate
{
	public class GoogleTranslateFree : ITranslator
	{
		private const string GoogleDetectedString = @"Our systems have detected unusual traffic from your computer network.  This page checks to see if it&#39;s really you sending the requests, and not a robot.";
		private const string GoogleDetectedString2 = @"This page appears when Google automatically detects requests coming from your computer network";
		public const string UserAgentPropertyName = "Credential Location";
		private static readonly Regex CombineEmptyLinesRegex = new Regex(@"^(\s*\n){2,}");
		//todo make this an external string?
		private const string TranslateFreeUrl = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=ja&tl=en&dt=t&q=";
		private static readonly HttpClient FreeClient = new HttpClient();
		public string Error { get; set; }

		public string SourceName => "Google Translate Free";
		public Dictionary<string, Type> Properties { get; } = new Dictionary<string, Type>();
		public void Initialise(Dictionary<string, object> properties)
		{
			properties.TryGetValue(UserAgentPropertyName, out var userAgentObject);
			var userAgentString = userAgentObject as string;
			FreeClient.DefaultRequestHeaders.Add(@"user-agent", userAgentString);
		}

		public bool Translate(string input, out string output)
		{
			try
			{
				var jsonString = GetPostResultAsString(FreeClient, TranslateFreeUrl + Uri.EscapeDataString(input));
				if (jsonString.Contains(GoogleDetectedString) || jsonString.Contains(GoogleDetectedString2))
				{
					var extracted = ExtractText(jsonString);
					output = $"Failed to translate, detected by Google: {extracted}";
					return false;
				}
				return TryDeserializeJsonResponse(jsonString, out output);
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
				var jArray = JsonConvert.DeserializeObject<JArray>(jsonString);
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

	}
}
