using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Happy_Reader.View
{
	public static class Theme
	{
		//tile background colors
		public static readonly Brush DefaultTileBrush = Brushes.LightBlue;
		public static readonly Brush WLHighBrush = Brushes.DeepPink;
		public static readonly Brush WLMediumBrush = Brushes.HotPink;
		public static readonly Brush WLLowBrush = Brushes.LightPink;
		public static readonly Brush WLBlacklistBrush = Brushes.Black;
		public static readonly Brush ULFinishedBrush = Brushes.LightGreen;
		public static readonly Brush ULStalledBrush = Brushes.DarkKhaki;
		public static readonly Brush ULDroppedBrush = Brushes.DarkOrange;
		public static readonly Brush ULUnknownBrush = Brushes.Gray;

		// ReSharper disable UnusedMember.Local
		//tile text colors
		public static readonly Brush FavoriteProducerBrush = Brushes.Yellow;
		public static readonly Brush ULPlayingBrush = Brushes.Yellow;
		public static readonly Brush UnreleasedBrush = Brushes.White;
		public static readonly Brush UnreleasedBorderBrush = Brushes.Black;

		public static readonly BitmapImage FileNotFoundImage;
		public static readonly BitmapImage ImageNotFoundImage;
		public static readonly BitmapImage NsfwImage;

		static Theme()
		{
			// ReSharper disable once PossibleNullReferenceException
			FileNotFoundImage = GetBitmapImageFromResourceFile(@"pack://application:,,,/Resources/file-not-found.png");
			ImageNotFoundImage = GetBitmapImageFromResourceFile(@"pack://application:,,,/Resources/no-image.png");
			NsfwImage = GetBitmapImageFromResourceFile(@"pack://application:,,,/Resources/nsfw-image.png");
		}


		private static BitmapImage GetBitmapImageFromResourceFile(string path)
		{
			var resourceStream = Application.GetResourceStream(new Uri(path));
			Debug.Assert(resourceStream != null, nameof(resourceStream) + " != null");
			using var stream = resourceStream.Stream;
			var bmp = new Bitmap(stream);
			using var memory = new MemoryStream();
			bmp.Save(memory, ImageFormat.Bmp);
			memory.Position = 0;
			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memory;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			return bitmapImage;
		}
	}
}
