﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Happy_Reader
{
    public class PausableUpdateList<T> : ObservableCollection<T>
    {
        private bool _updatesPaused;
        
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_updatesPaused) return;
            base.OnCollectionChanged(e);
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
