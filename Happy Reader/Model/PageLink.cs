using JetBrains.Annotations;

namespace Happy_Reader
{
	[UsedImplicitly]
	public class PageLink
	{
		public string Label { get; set; }

		public string Link { get; set; }

		public bool UseRomaji { get; set; }
	}
}
