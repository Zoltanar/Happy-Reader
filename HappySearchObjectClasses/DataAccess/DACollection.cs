﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Happy_Apps_Core.DataAccess
{
	// ReSharper disable once InconsistentNaming
	public class DACollection<TKey, TValue> : IEnumerable<TValue> where TValue : IDataItem<TKey>, new()
	{
		private readonly IDictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();
		private readonly Dictionary<TKey, TValue> _itemsToUpsertLater = new();

		private SQLiteConnection Conn { get; }

		private long _highestKey;

		public long HighestKey
		{
			get
			{
				if (!typeof(TKey).IsPrimitive) throw new NotSupportedException($"Can't order keys for non-primitive types.");
				return _highestKey;
			}
			private set
			{
				_highestKey = value;
			}
		}

		private IEnumerable<TValue> List => _items.Values;
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => List.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();
		public int Count => _items.Count;

		public DACollection(SQLiteConnection connection) => Conn = connection;

		public void Load(bool openAndCloseConnection)
		{
			if (_items.Count > 0)
			{
				StaticHelpers.Logger.ToDebug($"Loading {nameof(DACollection<TKey, TValue>)} after already loaded");
				_items.Clear();
				_itemsToUpsertLater.Clear();
			}

            if (openAndCloseConnection)
            {
                Conn.Open();
                Conn.Trace += StaticHelpers.LogDatabaseTrace;
            }
			try
			{
				var sql = $@"Select * from {typeof(TValue).Name}s";
				using var command = Conn.CreateCommand();
				command.CommandText = sql;
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var item = new TValue();
					item.LoadFromReader(reader);
					_items.Add(item.Key, item);
					if (item.Key is long longKey && longKey > HighestKey) HighestKey = longKey;
					else if (item.Key is int intKey && intKey > HighestKey) HighestKey = intKey;
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

		public int Upsert(TValue item, bool openNewConnection, bool insertOnly = false, DbTransaction transaction = null)
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
				if (result) _items[item.Key] = item;
				if (item.Key is long longKey && longKey > HighestKey) HighestKey = longKey;
				else if (item.Key is int intKey && intKey > HighestKey) HighestKey = intKey;
				return rowsAffected;
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

		public bool Remove(TValue item, bool openAndCloseConnection)
		{
			var result = _items.Remove(item.Key);
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
				using var command = Conn.CreateCommand();
				command.CommandText = $@"Delete from {typeof(TValue).Name}s ";
                PopulateKeyClause(command, item);
				var rowsAffected = command.ExecuteNonQuery();
				result = rowsAffected != 0;
				if (!result || rowsAffected > 1) { }
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

        private void PopulateKeyClause(DbCommand command, TValue item)
        {
            if (item.Key is ITuple tuple)
            {
                var parameters = string.Join(",", Enumerable.Range(0, tuple.Length).Select(i => $"@Key{i:0}"));
                command.CommandText += $"where {item.KeyField} = ({parameters});";
                for (int i = 0; i < tuple.Length; i++) command.AddParameter($"@Key{i:0}", tuple[i]);
                return;
            }
            command.CommandText += $"where {item.KeyField} = @Key;";
			command.AddParameter("@Key", item.Key);
        }

        public void Add(TValue item, bool openNewConnection, bool insertOnly = false, SQLiteTransaction transaction = null)
		=> Upsert(item, openNewConnection, insertOnly, transaction);

		/// <summary>
		/// Returns default if item does not exist.
		/// </summary>
		public TValue this[TKey key] => _items.ContainsKey(key) ? _items[key] : default;
		
		public IEnumerable<TValue> WithKeyIn(ICollection<TKey> keyCollection)
		{
			return _items.Where(i => keyCollection.Contains(i.Key)).Select(i => i.Value);
		}

		private static readonly bool ImplementsIReadyToUpsert = typeof(IReadyToUpsert).IsAssignableFrom(typeof(TValue));

		public int SaveChanges()
		{
			var otherItemsToUpsert = ImplementsIReadyToUpsert
				? _items.Values.Where(i => ((IReadyToUpsert) i).ReadyToUpsert).ToArray() : Array.Empty<TValue>();
			if (_itemsToUpsertLater.Count == 0 && otherItemsToUpsert.Length == 0) return 0;
			Conn.Open();
            var databaseLogging = StaticHelpers.Logger.LogDatabase;
            StaticHelpers.Logger.LogDatabase = false;
            DbTransaction transaction = null;
			int rowsAffected = 0;
			try
			{
				transaction = Conn.BeginTransaction();
				foreach (var item in _itemsToUpsertLater.Values.Concat(otherItemsToUpsert))
				{
					rowsAffected += Upsert(item, false, false, transaction);
				}
				transaction.Commit();
				return rowsAffected;
			}
			catch
			{
				transaction?.Rollback();
				throw;
			}
			finally
			{
				_itemsToUpsertLater.Clear();
				foreach (var item in otherItemsToUpsert)
				{
					((IReadyToUpsert)item).ReadyToUpsert = false;
				}
				Conn.Close();
                StaticHelpers.Logger.LogDatabase = databaseLogging;

            }
		}

		public void UpsertLater(TValue item)
		{
			_items[item.Key] = item;
			_itemsToUpsertLater[item.Key] = item;
		}
	}
}
