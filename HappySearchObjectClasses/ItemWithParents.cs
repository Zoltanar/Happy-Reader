using System;
using System.Collections.Generic;
using System.Linq;

namespace Happy_Apps_Core
{
	public static partial class DumpFiles
	{
		public abstract class ItemWithParents
		{
			public int ID { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public List<string> Aliases { get; set; }
			public bool Meta { get; set; }
			public List<int> Parents { get; set; }
			public int[] Children { get; set; }
			/// <summary>
			/// Children+ID
			/// </summary>
			public int[] AllIDs { get; set; }

			public void SetItemChildren(List<ItemWithParents> list)
			{
				int[] children = Array.Empty<int>();
				int[] childrenForThisRound = list.Where(x => x.Parents.Contains(ID)).Select(x => x.ID).ToArray(); //at this moment, it contains direct sub-tags
				var difference = childrenForThisRound.Length;
				while (difference > 0)
				{
					var initial = children.Length;
					children = children.Union(childrenForThisRound).ToArray(); //first time, adds direct sub-tags, second time it adds 2-away sub-tags, etc...
					difference = children.Length - initial;
					var tmp = new List<int>();
					foreach (var child in childrenForThisRound)
					{
						IEnumerable<int> childsChildren = list.Where(x => x.Parents.Contains(child)).Select(x => x.ID);
						tmp.AddRange(childsChildren);
					}
					childrenForThisRound = tmp.ToArray();
				}
				Children = children;
				AllIDs = children.Union(new[] { ID }).ToArray();
			}

			public bool InCollection(IEnumerable<int> idCollection)
			{
				return InCollection(idCollection, out _);
			}

			public abstract bool InCollection(IEnumerable<int> idCollection, out int match);
		}
	}
}