using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace Happy_Apps_Core.DataAccess
{
    // ReSharper disable once InconsistentNaming
    public class DAListCollection<TListKey, TItemKey, TItem> : IEnumerable<TItem> where TItem : IDataItem<TItemKey>, IDataListItem<TListKey>, new()
    {
        private readonly IDictionary<TListKey, Dictionary<TItemKey, TItem>> _items = new Dictionary<TListKey, Dictionary<TItemKey, TItem>>();
        private SQLiteConnection Conn { get; }

        private IEnumerable<TItem> List => _items.Values.SelectMany(i => i.Values);
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();
        public int Count => _items.Count;

        public DAListCollection(SQLiteConnection connection) => Conn = connection;

        public void Load(bool openAndCloseConnection)
        {
            if (_items.Count > 0)
            {
                StaticHelpers.Logger.ToDebug($"Loading {nameof(DACollection<TItemKey, TItem>)} after already loaded");
                _items.Clear();
            }

            if (openAndCloseConnection)
            {
                Conn.Open();
                Conn.Trace += StaticHelpers.LogDatabaseTrace;

            }
            try
            {
                var sql = $@"Select * from {typeof(TItem).Name}s";
                using var command = Conn.CreateCommand();
                command.CommandText = sql;
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var item = new TItem();
                    item.LoadFromReader(reader);
                    if (!_items.TryGetValue(item.ListKey, out var list)) list = _items[item.ListKey] = new Dictionary<TItemKey, TItem>();
                    list[item.Key] = item;
                }
            }
            finally
            {
                if (openAndCloseConnection)
                {
                    Conn.Close();
                    Conn.Trace -= StaticHelpers.LogDatabaseTrace;
                }
            }
        }

        public void Upsert(TItem item, bool openNewConnection, bool insertOnly = false, SQLiteTransaction transaction = null)
        {
            if (openNewConnection)
            {
                Conn.Open();
                Conn.Trace += StaticHelpers.LogDatabaseTrace;
            }
            try
            {
                using var command = item.UpsertCommand(Conn, insertOnly);
                command.Transaction = transaction;
                var rowsAffected = command.ExecuteNonQuery();
                var result = rowsAffected != 0;
                if (result)
                {
                    if (!_items.TryGetValue(item.ListKey, out var list)) list = _items[item.ListKey] = new Dictionary<TItemKey, TItem>();
                    list[item.Key] = item;
                }
                if (!result) { }
            }
            finally
            {
                if (openNewConnection)
                {
                    Conn.Close();
                    Conn.Trace -= StaticHelpers.LogDatabaseTrace;
                }
            }
        }

        public bool Remove(TItem item, bool openAndCloseConnection)
        {
            var result = _items.TryGetValue(item.ListKey, out var list) && list.Remove(item.Key);
            if (!result)
            {
                throw new InvalidOperationException("Key not found");
            }

            if (openAndCloseConnection)
            {
                Conn.Open();
                Conn.Trace += StaticHelpers.LogDatabaseTrace;
            }
            try
            {
                var sql = $@"Delete from {typeof(TItem).Name}s where {item.KeyField} = {item.Key}";
                using var command = Conn.CreateCommand();
                command.CommandText = sql;
                var rowsAffected = command.ExecuteNonQuery();
                result = rowsAffected != 0;
                if (!result) { }
            }
            finally
            {
                if (openAndCloseConnection)
                {
                    Conn.Close();
                    Conn.Trace -= StaticHelpers.LogDatabaseTrace;
                }
            }
            return result;
        }

        public void Add(TItem item, bool openNewConnection, bool insertOnly = false, SQLiteTransaction transaction = null)
        => Upsert(item, openNewConnection, insertOnly, transaction);

        /// <summary>
        /// Returns empty list if list does not exist.
        /// </summary>
        public IEnumerable<TItem> this[TListKey key] => _items.ContainsKey(key) ? _items[key].Values : Array.Empty<TItem>().AsEnumerable();

        public IEnumerable<TItem> WithKeyIn(IList<TListKey> keyCollection)
            => _items.Where(i => keyCollection.Contains(i.Key)).SelectMany(i => i.Value.Values);

        /// <summary>
        /// Returns item with given list and item keys, or default if not found.
        /// </summary>
        public TItem ByKey(TListKey listKey, TItemKey itemKey)
        {
            if (!_items.TryGetValue(listKey, out var list)) return default;
            return list.TryGetValue(itemKey, out var item) ? item : default;
        }
    }

    // ReSharper disable once InconsistentNaming
    public class DAGroupCollection<TGroupKey, TItem> : IEnumerable<TItem> where TItem : IDataGroupItem<TGroupKey>, new()
    {
        private readonly IDictionary<TGroupKey, List<TItem>> _items = new Dictionary<TGroupKey, List<TItem>>();
        private SQLiteConnection Conn { get; }

        private IEnumerable<TItem> List => _items.Values.SelectMany(i => i);
        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();
        public int Count => List.Count();

        public DAGroupCollection(SQLiteConnection connection) => Conn = connection;

        public void Load(bool openAndCloseConnection)
        {
            if (_items.Count > 0)
            {
                StaticHelpers.Logger.ToDebug($"Loading {nameof(DAGroupCollection<TGroupKey, TItem>)} after already loaded");
                _items.Clear();
            }

            if (openAndCloseConnection)
            {
                Conn.Open();
                Conn.Trace += StaticHelpers.LogDatabaseTrace;

            }
            try
            {
                var sql = $@"Select * from {typeof(TItem).Name}s";
                using var command = Conn.CreateCommand();
                command.CommandText = sql;
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var item = new TItem();
                    item.LoadFromReader(reader);
                    if (!_items.TryGetValue(item.GroupKey, out var list)) list = _items[item.GroupKey] = new List<TItem>();
                    list.Add(item);
                }
            }
            finally
            {
                if (openAndCloseConnection)
                {
                    Conn.Close();
                    Conn.Trace -= StaticHelpers.LogDatabaseTrace;
                }
            }
        }

        public void Upsert(TItem item, bool openNewConnection, bool insertOnly = false, SQLiteTransaction transaction = null)
        {
            if (openNewConnection)
            {
                Conn.Open();
                Conn.Trace += StaticHelpers.LogDatabaseTrace;
            }
            try
            {
                using var command = item.UpsertCommand(Conn, insertOnly);
                command.Transaction = transaction;
                var rowsAffected = command.ExecuteNonQuery();
                var result = rowsAffected != 0;
                if (result)
                {
                    if (!_items.TryGetValue(item.GroupKey, out var list)) list = _items[item.GroupKey] = new List<TItem>();
                    list.Add(item);
                }
                if (!result) { }
            }
            finally
            {
                if (openNewConnection)
                {
                    Conn.Close();
                    Conn.Trace -= StaticHelpers.LogDatabaseTrace;
                }
            }
        }

        public void Add(TItem item, bool openNewConnection, bool insertOnly = false, SQLiteTransaction transaction = null)
        => Upsert(item, openNewConnection, insertOnly, transaction);

        /// <summary>
        /// Returns empty list if list does not exist.
        /// </summary>
        public IEnumerable<TItem> this[TGroupKey key] => _items.ContainsKey(key) ? _items[key] : Array.Empty<TItem>().AsEnumerable();

        /// <summary>
        /// Returns items where group key matches one of the keys passed
        /// </summary>
        public IEnumerable<TItem> WithKeyIn(IList<TGroupKey> keyCollection)
            => _items.Where(i => keyCollection.Contains(i.Key)).SelectMany(i => i.Value);
    }
}
