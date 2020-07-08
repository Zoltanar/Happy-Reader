using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Reader.View.SampleData
{
	[UsedImplicitly]
	public class SampleListedVN : ListedVN
	{
		public override OwnedStatus IsOwned => OwnedStatus.CurrentlyOwned;

		public override object FlagSource { get; } = BitmapSource.Create(18, 12, 1, 1, PixelFormats.Rgb24, BitmapPalettes.WebPalette, FlagArray, 18 * 3);

		public SampleListedVN()
		{
			Languages = JsonConvert.SerializeObject(new VNLanguages(new[] { "ja" }, new[] { "ja" }));
			Suggestion = new SuggestionScoreObject(.75,2);
			VNID = 7;
			ImageId = "(cv,2252)";
			Title = "Tsukihime";
			Rating = 9.8;
			VoteCount = 3546;
		}

		private static readonly byte[] FlagArray = GetFlagBytes();

		private static byte[] GetFlagBytes()
		{
			var bytes = new List<byte>();
			var red = ((byte)190, (byte)0, (byte)38);
			var strongEdge = ((byte)223, (byte)128, (byte)147);
			var weakEdge = ((byte)241, (byte)199, (byte)207);
			var white = ((byte)0xff, (byte)0xff, (byte)0xff);
			AddBytes(bytes, (white, 18 * 2)); //first lines
			AddBytes(bytes, (white, 7), (weakEdge, 1), (strongEdge, 2), (weakEdge, 1), (white, 7)); //edge 3
			AddBytes(bytes, (white, 6), (strongEdge, 1), (red, 4), (strongEdge, 1), (white, 6)); //middle 4
			AddBytes(bytes, (white, 5), (weakEdge, 1), (red, 6), (weakEdge, 1), (white, 5)); //middle 5
			AddBytes(bytes, (white, 5), (strongEdge, 1), (red, 6), (strongEdge, 1), (white, 5)); //middle 6
			AddBytes(bytes, (white, 5), (strongEdge, 1), (red, 6), (strongEdge, 1), (white, 5)); //middle 7
			AddBytes(bytes, (white, 5), (weakEdge, 1), (red, 6), (weakEdge, 1), (white, 5)); //middle 8
			AddBytes(bytes, (white, 6), (strongEdge, 1), (red, 4), (strongEdge, 1), (white, 6)); //middle 9
			AddBytes(bytes, (white, 7), (weakEdge, 1), (strongEdge, 2), (weakEdge, 1), (white, 7)); //edge 10
			AddBytes(bytes, (white, 18 * 2)); //last lines
			if (bytes.Count != 18 * 12 * 3) throw new Exception($"Expected {18 * 12 * 3} bytes, got {bytes.Count}");
			return bytes.ToArray();
		}

		private static void AddBytes(ICollection<byte> bytes, params ((byte r, byte g, byte b), int)[] quads)
		{
			foreach (var quad in quads)
			{
				for (int i = 0; i < quad.Item2; i++)
				{
					bytes.Add(quad.Item1.r);
					bytes.Add(quad.Item1.g);
					bytes.Add(quad.Item1.b);
				}
			}
		}

	}
}
