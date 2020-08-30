using System.IO;
using Newtonsoft.Json;

namespace Happy_Apps_Core
{
	public class ScreenItem
	{
		public string ImageId { get; set; }
		public bool Nsfw { get; set; }
		public int Height { get; set; }
		public int Width { get; set; }

		private string _imageSource;

		/// <summary>
		/// Get path of stored screenshot
		/// </summary>
		[JsonIgnore]
		public string StoredLocation
		{
			get
			{
				if (_imageSource != null) return _imageSource;
				if (ImageId == null) _imageSource = Path.GetFullPath(StaticHelpers.NoImageFile);
				else
				{
					var filePath = StaticHelpers.GetImageLocation(ImageId);
					_imageSource = File.Exists(filePath) ? filePath : Path.GetFullPath(StaticHelpers.NoImageFile);
				}
				return _imageSource;
			}
		}
	}
}