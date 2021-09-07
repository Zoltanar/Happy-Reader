using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public class NamedFunction : INotifyPropertyChanged
	{
		private static NamedFunction _lastSelected;
		private bool _selected;

		public IEnumerable<IDataItem<int>> Function(
			VisualNovelDatabase db,
			Func<VisualNovelDatabase, IEnumerable<IDataItem<int>>> getAllFunc,
			Func<VisualNovelDatabase, int[], IEnumerable<IDataItem<int>>> getAllWithKeyFunc)
			=> _customFilter.GetAllResults(db, getAllFunc, getAllWithKeyFunc);

		private readonly CustomFilter _customFilter;
		public string Name { get; }
		public bool Selected
		{
			get => _selected;
			private set
			{
				_selected = value;
				if (value)
				{
					if (_lastSelected != null) _lastSelected._selected = false;
					_lastSelected = this;
				}
				OnPropertyChanged(null);
			}
		}

		public NamedFunction(CustomFilter customFilter)
		{
			_customFilter = customFilter;
			Name = customFilter.Name;
		}

		public IEnumerable<IDataItem<int>> SelectAndInvoke(VisualNovelDatabase localDatabase, DatabaseViewModelBase databaseViewModelBase)
		{
			if (_lastSelected != null) _lastSelected.Selected = false;
			Selected = true;
			return Function(localDatabase, databaseViewModelBase.GetAll, databaseViewModelBase.GetAllWithKeyIn);
		}

		public override string ToString() => (Selected ? "[✔] " : "") + Name;
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}