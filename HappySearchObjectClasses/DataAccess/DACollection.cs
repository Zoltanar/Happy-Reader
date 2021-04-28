using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;

namespace Happy_Apps_Core.DataAccess
{
	// ReSharper disable once InconsistentNaming
	public class DACollection<TKey, TValue> : IEnumerable<TValue> where TValue : IDataItem<TKey>, new()
	{
		private readonly IDictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();
		private readonly Dictionary<TKey, TValue> _itemsToUpsert = new();

		private DbConnection Conn { get; }

		private IEnumerable<TValue> List => _items.Values;
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => List.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();
		public int Count => _items.Count;

		public DACollection(DbConnection connection) => Conn = connection;

		public void Load(bool openAndCloseConnection)
		{
			if (_items.Count > 0)
			{
				StaticHelpers.Logger.ToDebug($"Loading {nameof(DACollection<TKey, TValue>)} after already loaded");
				_items.Clear();
				_itemsToUpsert.Clear();
			}
			if (openAndCloseConnection) Conn.Open();
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
				}
			}
			finally
			{
				if (openAndCloseConnection) Conn.Close();
			}
		}

		public void Upsert(TValue item, bool openNewConnection, bool insertOnly = false, DbTransaction transaction = null)
		{
			if (openNewConnection) Conn.Open();
			try
			{
				using var command = item.UpsertCommand(Conn, insertOnly);
				command.Transaction = transaction;
				var rowsAffected = command.ExecuteNonQuery();
				var result = rowsAffected != 0;
				if (result) _items[item.Key] = item;
				if (!result) { }
			}
			finally
			{
				if (openNewConnection) Conn.Close();
			}
		}

		public bool Remove(TValue item, bool openAndCloseConnection)
		{
			var result = _items.Remove(item.Key);
			if (!result)
			{
				throw new InvalidOperationException("Key not found");
			}
			if (openAndCloseConnection) Conn.Open();
			try
			{
				var sql = $@"Delete from {typeof(TValue).Name}s where {item.KeyField} = {item.Key}";
				using var command = Conn.CreateCommand();
				command.CommandText = sql;
				var rowsAffected = command.ExecuteNonQuery();
				result = rowsAffected != 0;
				if (!result) { }
			}
			finally
			{
				if (openAndCloseConnection) Conn.Close();
			}
			return result;
		}
		
		public void Add(TValue item, bool openNewConnection, bool insertOnly = false, SQLiteTransaction transaction = null)
		=> Upsert(item, openNewConnection, insertOnly, transaction);

		/// <summary>
		/// Returns default if item does not exist.
		/// </summary>
		public TValue this[TKey key] => _items.ContainsKey(key) ? _items[key] : default;
		
		public IEnumerable<TValue> WithKeyIn(IList<TKey> keyCollection)
		{
			return _items.Where(i => keyCollection.Contains(i.Key)).Select(i => i.Value);
		}
		

		public void SaveChanges()
		{
			if (_itemsToUpsert.Count == 0) return;
			Conn.Open();
			DbTransaction transaction = null;
			try
			{
				transaction = Conn.BeginTransaction();
				foreach (var item in _itemsToUpsert.Values)
				{
					Upsert(item, false, false, transaction);
				}

				transaction.Commit();
			}
			catch
			{
				transaction?.Rollback();
				throw;
			}
			finally
			{
				_itemsToUpsert.Clear();
				Conn.Close();

			}
		}

		public void UpsertLater(TValue item)
		{
			_items[item.Key] = item;
			_itemsToUpsert[item.Key] = item;
		}
	}
}
