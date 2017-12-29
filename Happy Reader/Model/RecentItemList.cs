using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Happy_Apps_Core;

namespace Happy_Reader
{
	public class RecentItemList<T>
	{
		private readonly int _size;
		public readonly BindingList<T> Items;
		public RecentItemList(int size = 25, IEnumerable<T> items = null)
		{
			_size = size;
			Items = new BindingList<T>();
			if (items != null) Items.AddRange(items);
		}
		public void Add(T item)
		{
			var foundItem = Items.FirstOrDefault(x => x.Equals(item));
			//probably won't work well with value types
			if (foundItem != null)
			{
				Items.Remove(foundItem);
				Items.Insert(0, item);
				return;
			}
			if (Items.Count == _size) Items.RemoveAt(Items.Count - 1);
			Items.Insert(0, item);
		}
    }

    public class RecentStringList : RecentItemList<string>
    {
        private int _idCounter;

        public RecentStringList(int size = 25, IEnumerable<string> items = null) : base(size, items)
        {
        }

        public void AddWithId(string item)
        {
            _idCounter++;
            Add($"[{_idCounter}] {item}");
        }
    }
}
