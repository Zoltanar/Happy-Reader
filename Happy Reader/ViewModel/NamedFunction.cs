using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Happy_Apps_Core.Database;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public class NamedFunction<T> : INotifyPropertyChanged
	{
		private static NamedFunction<T> _lastSelected;
		private bool _selected;
		public Func<VisualNovelDatabase, IEnumerable<T>> Function { get; }
		public string Name { get; }
		public bool AlwaysIncludeBlacklisted { get; }
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
		
		public NamedFunction(Func<VisualNovelDatabase, IEnumerable<T>> function, string name,bool alwaysIncludeBlacklisted)
		{
			Function = function;
			Name = name;
			AlwaysIncludeBlacklisted = alwaysIncludeBlacklisted;
		}

		public IEnumerable<T> SelectAndInvoke(VisualNovelDatabase localDatabase)
		{
			if (_lastSelected != null) _lastSelected.Selected = false;
			Selected = true;
			return Function(localDatabase);
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