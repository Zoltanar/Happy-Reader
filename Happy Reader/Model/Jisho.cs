using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Happy_Apps_Core;
using Newtonsoft.Json;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace Happy_Reader
{
	public static class Jisho
	{
		private static readonly HttpClient HttpClient = new HttpClient();
		private const string ApiUrl = @"http://jisho.org/api/v1/search/words?keyword=";
		
		public static async Task<JishoResponse> Search(string searchString)
		{
			JishoResponse jishoResponse = null;
			try
			{
				var response = await HttpClient.GetStringAsync($"{ApiUrl}\"{searchString}\"");
				jishoResponse = JsonConvert.DeserializeObject<JishoResponse>(response);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
			return jishoResponse;
		}
	}


	public class JishoResponse
	{
		public Meta Meta { get; set; }
		public Datum[] Data { get; set; }
	}

	public class Meta
	{
		public int Status { get; set; }
	}

	public class Datum
	{
		[JsonProperty("is_common")]
		public bool IsCommon { get; set; }
		public string[] Tags { get; set; }
		public Japanese[] Japanese { get; set; }
		public Sens[] Senses { get; set; }
		public Attribution Attribution { get; set; }

		public string Results()
		{
			var sb = new StringBuilder();
			sb.Append(Japanese[0].Word == null? Japanese[0].Reading: $"{Japanese[0].Word} ({Japanese[0].Reading})");
			sb.AppendLine($"({Kakasi.JapaneseToRomaji(Japanese[0].Reading)})");
			for (var index = 0; index < Senses.Length; index++)
			{
				var sense = Senses[index];
				if (sense.PartsOfSpeech.Length > 0)
				{
					if (sense.PartsOfSpeech[0] == @"Wikipedia definition") continue;
					sb.AppendLine(string.Join("; ", sense.PartsOfSpeech));
				}
				if (sense.EnglishDefinitions.Length > 0) sb.Append(string.Join("; ", sense.EnglishDefinitions));
				if (sense.Tags.Length > 0) sb.Append($"({string.Join("; ", sense.Tags)})");
				if (index + 1 < Senses.Length) sb.AppendLine();
			}
			return sb.ToString();
		}
	}

	public class Attribution
	{
		public bool Jmdict { get; set; }
		public bool Jmnedict { get; set; }
		public object Dbpedia { get; set; }
	}

	public class Japanese
	{
		public string Word { get; set; }
		public string Reading { get; set; }
	}

	public class Sens
	{
		[JsonProperty("english_definitions")]
		public string[] EnglishDefinitions { get; set; }
		[JsonProperty("parts_of_speech")]
		public string[] PartsOfSpeech { get; set; }
		public Link[] Links { get; set; }
		public string[] Tags { get; set; }
		public object[] Restrictions { get; set; }
		[JsonProperty("see_also")]
		public string[] SeeAlso { get; set; }
		public object[] Antonyms { get; set; }
		public object[] Source { get; set; }
		public object[] Info { get; set; }
		public object[] Sentences { get; set; }
	}

	public class Link
	{
		public string Text { get; set; }
		public string URL { get; set; }
	}

}
