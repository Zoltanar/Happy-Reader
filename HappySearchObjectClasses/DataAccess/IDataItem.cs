using System.Data;
using System.Data.Common;

namespace Happy_Apps_Core.DataAccess
{
	public interface IDataItem<out TKey>
	{
		string KeyField { get; }
		TKey Key { get; }
		DbCommand UpsertCommand(DbConnection connection, bool insertOnly);

		void LoadFromReader(IDataRecord reader);
	}
}
