using System.Collections.Generic;

namespace Happy_Reader
{
	public class SavedData
	{
		public HashSet<SavedTab> Tabs { get; set; } = new();

		public struct SavedTab
		{
			public long Id { get; set; }
			public string TypeName { get; set; }

			public SavedTab(long id, string typeName)
			{
				Id = id;
				TypeName = typeName;
			}

		}
	}
}