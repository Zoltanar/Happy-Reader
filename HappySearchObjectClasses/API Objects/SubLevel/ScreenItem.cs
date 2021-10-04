using Newtonsoft.Json;

namespace Happy_Apps_Core
{
	public class ScreenItem
	{
		public string ImageId { get; set; }
		public bool Nsfw { get; set; }
		public int Height { get; set; }
		public int Width { get; set; }

		private bool _imageSourceSet;
		private string _imageSource;

		/// <summary>
		/// Get path of stored screenshot
		/// </summary>
		[JsonIgnore]
		public string StoredLocation => StaticHelpers.GetImageSource(ImageId, ref _imageSourceSet, ref _imageSource, "st");
	}
}