using System.Windows.Media;

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
	}
}
