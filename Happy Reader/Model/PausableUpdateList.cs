using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Happy_Reader
{
    public class PausableUpdateList<T> : ObservableCollection<T>
    {
        private bool _updatesPaused;
        private readonly Func<T, bool> _functionOnRemoval;

        /// <summary>
        /// Can pass a function to occur when an item is removed from the collection when updates are not paused.
        /// </summary>
        public PausableUpdateList(Func<T, bool> functionOnRemoval = null)
        {
            _functionOnRemoval = functionOnRemoval;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_updatesPaused) return;
            base.OnCollectionChanged(e);
            //only call function on removal if updates are not paused, so this isn't triggered when setting and clearing the collection.
            if (e.Action != NotifyCollectionChangedAction.Remove || e.OldItems.Count <= 0 || _functionOnRemoval == null) return;
            foreach (T item in e.OldItems) _functionOnRemoval(item);
        }

        public void SetRange(IEnumerable<T> list)
        {
            _updatesPaused = true;
            Clear();
            AddRange(list);
        }
        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
            {
                _updatesPaused = false;
                throw new ArgumentNullException(nameof(list));
            }
            _updatesPaused = true;
            foreach (var item in list) Add(item);
            _updatesPaused = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
