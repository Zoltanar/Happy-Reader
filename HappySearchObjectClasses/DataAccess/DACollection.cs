using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Happy_Apps_Core.DataAccess
{
	// ReSharper disable once InconsistentNaming
	public class DACollection<TKey, TValue> : IEnumerable<TValue> where TValue : IDataItem<TKey>, new()
	{
		private readonly IDictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();
		private readonly Dictionary<TKey, TValue> _itemsToUpsertLater = new();

		private SqliteConnection Conn { get; }

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
        public IEnumerator<TValue> GetEnumerator() => List.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public int Count => _items.Count;

		public DACollection(SqliteConnection connection) => Conn = connection;

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
                Conn.Trace(StaticHelpers.LogDatabaseTrace);
            }
			try
			{
				var sql = $@"Select * from {typeof(TValue).Name}s";
				using var command = Conn.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                command.CommandText = sql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
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
                }
			}
		}

		public int Upsert(TValue item, bool openNewConnection, bool insertOnly = false, DbTransaction transaction = null)
		{
            if (openNewConnection)
            {
                Conn.Open();
                Conn.Trace(StaticHelpers.LogDatabaseTrace);
				Conn.Update(StaticHelpers.LogDatabaseUpdate);
            }
			DbCommand command = null;
            try
			{
				command = item.UpsertCommand(Conn, insertOnly);
                command.Transaction = transaction;
				var rowsAffected = command.ExecuteNonQuery();
				var result = rowsAffected != 0;
				if (result) _items[item.Key] = item;
				if (item.Key is long longKey && longKey > HighestKey) HighestKey = longKey;
				else if (item.Key is int intKey && intKey > HighestKey) HighestKey = intKey;
				return rowsAffected;
			}
			catch(Exception ex)
            {
                var parameters = command != null ? string.Join("|", command.Parameters.Cast<DbParameter>().Select(p => $"{p.ParameterName}={p.Value}")) : "null";
                StaticHelpers.Logger.ToFile($"Error upserting item with key {item.Key} of type {typeof(TValue).Name};CommandText={command?.CommandText};Parameters={parameters};Exception:{ex}");
                throw;
            }
            finally
            {
				command?.Dispose();
                if (openNewConnection)
                {
                    Conn.Close();
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
                Conn.Trace(StaticHelpers.LogDatabaseTrace);
                Conn.Update(StaticHelpers.LogDatabaseUpdate);
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

        public void Add(TValue item, bool openNewConnection, bool insertOnly = false, SqliteTransaction transaction = null)
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
