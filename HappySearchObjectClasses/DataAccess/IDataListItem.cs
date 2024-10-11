using System.Data.Common;
using System.Data;

namespace Happy_Apps_Core.DataAccess
{
    public interface IDataListItem<out TListKey>
    {
        TListKey ListKey { get; }
    }

    public interface IDataGroupItem<out TGroupKey>
    {
        TGroupKey GroupKey { get; }

        DbCommand UpsertCommand(DbConnection connection, bool insertOnly);

        void LoadFromReader(IDataRecord reader);
    }
}