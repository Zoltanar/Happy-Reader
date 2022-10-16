using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace Happy_Reader.View
{
	public static class Theme
	{
		//tile background colors
		public static readonly Brush DefaultTileBrush = Brushes.LightBlue;
		public static readonly Brush WLHighBrush = Brushes.DeepPink;
		public static readonly Brush WLMediumBrush = Brushes.HotPink;
		public static readonly Brush WLLowBrush = Brushes.LightPink;
		public static readonly Brush WLBlacklistBrush = new SolidColorBrush(Color.FromRgb(35	,35,35));
		public static readonly Brush ULFinishedBrush = Brushes.LightGreen;
		public static readonly Brush ULStalledBrush = Brushes.DarkKhaki;
		public static readonly Brush ULDroppedBrush = Brushes.DarkOrange;
		public static readonly Brush ULUnknownBrush = Brushes.LightGray;
		//tab header background colors
		public static readonly Brush VNTabBackground = Brushes.HotPink;
		public static readonly Brush UserGameTabBackground = Brushes.DarkKhaki;
		public static readonly Brush ProducerTabBackground = Brushes.MediumPurple;
        public static readonly Brush TypeNotFoundTabBackground = Brushes.IndianRed;
        // ReSharper disable UnusedMember.Local
        //tile text colors
        public static readonly Brush FavoriteProducerBrush = Brushes.Yellow;
		public static readonly Brush FavoriteProducerDarkBrush = Brushes.DarkKhaki;
        public static readonly Brush NotFavoriteProducerBrush = Brushes.Black;
        public static readonly Brush ULPlayingBrush = Brushes.Yellow;
        public static readonly Brush UnreleasedBrush = Brushes.White;
        public static readonly Brush ReleasedBrush = Brushes.Black;
        public static readonly Brush UnreleasedBorderBrush = Brushes.Black;

		public static readonly Brush NewlyAddedBorderBrush = Brushes.Yellow;
        public static readonly Brush NotNewlyAddedBorderBrush = Brushes.Transparent;
        public static readonly Brush MtlBrush = new LinearGradientBrush(Colors.Black, Colors.Transparent, 0);
        public static readonly Brush LogForeground = Brushes.Green;
        public static readonly Brush MouseoverTooltipForeground = Brushes.White;
        public static readonly Brush MouseoverTooltipBackground = new SolidColorBrush(Colors.Black) { Opacity = 0.6};
        public static readonly Brush OutputSpacerForeground = Brushes.White;
        public static readonly Brush OutputErrorForeground = Brushes.Red;
        public static readonly Brush ProcessOffBackground = Brushes.Red;
        public static readonly Brush ProcessPausedBackground = Brushes.Yellow;
        public static readonly Brush ProcessOnBackground = Brushes.LimeGreen;

        public static readonly Brush NeverOwnedBackground = Brushes.Transparent;
        public static readonly Brush PastOwnedBackground = Brushes.DarkKhaki;
        public static readonly Brush CurrentlyOwnedBackground = Brushes.Green;

        public static readonly Brush MainCharacterBackground = Brushes.Gold;
        public static readonly Brush PrimaryCharacterBackground = Brushes.Orchid;
        public static readonly Brush SideCharacterBackground = Brushes.GreenYellow;
        public static readonly Brush AppearsCharacterBackground = Brushes.LightBlue;
        public static readonly Brush UnknownCharacterBackground = Brushes.Gray;

        public static Brush MtlBorder = Brushes.Yellow;
        public static Brush NonMtlBorder = Brushes.Black;

        public static readonly Brush VndbConnectionReadyForeground = Brushes.Black;
        public static readonly Brush VndbConnectionReadyBackground = Brushes.LightGreen;
        public static readonly Brush VndbConnectionBusyForeground = Brushes.Red;
        public static readonly Brush VndbConnectionBusyBackground = Brushes.Khaki;
        public static readonly Brush VndbConnectionThrottledForeground = Brushes.DarkRed;
        public static readonly Brush VndbConnectionThrottledBackground = Brushes.Khaki;
        public static readonly Brush VndbConnectionErrorForeground = Brushes.Black;
        public static readonly Brush VndbConnectionErrorBackground = Brushes.Red;
        public static readonly Brush VndbConnectionClosedForeground = Brushes.White;
        public static readonly Brush VndbConnectionClosedBackground = Brushes.Black;

        public static readonly Brush DeleteEntryPrimedBackground = Brushes.Red;
        public static readonly Brush DeleteEntryBackground = Brushes.CornflowerBlue;

        //images
        public static readonly BitmapImage FileNotFoundImage;
		public static readonly BitmapImage ImageNotFoundImage;
		public static readonly BitmapImage NsfwImage;
        public const string FileNotFoundPath = @"pack://application:,,,/Resources/file-not-found.png";
        public const string ImageNotFoundPath = @"pack://application:,,,/Resources/no-image.png";
        public const string NsfwImagePath = @"pack://application:,,,/Resources/nsfw-image.png";

        static Theme()
		{
			// ReSharper disable once PossibleNullReferenceException
			FileNotFoundImage = GetBitmapImageFromResourceFile(FileNotFoundPath);
			ImageNotFoundImage = GetBitmapImageFromResourceFile(ImageNotFoundPath);
			NsfwImage = GetBitmapImageFromResourceFile(NsfwImagePath);
		}

        public static readonly Brush InvalidColorBoxBackground = Brushes.Transparent;

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
