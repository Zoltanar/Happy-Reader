using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database;

public class VnSeiyuu : DumpItem, IDataItem<(int, int, int)>
{
    public int VNID { get; set; }
    public int AliasID { get; set; }
    public int CharacterID { get; set; }
    public string Note { get; set; }

    public string KeyField => "(VNID, AliasID, CharacterID)";
    public (int, int, int) Key => (VNID, AliasID, CharacterID);

    public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
    {
        string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(VnSeiyuu)}s " +
                     "(VNID,AID,CID,Note) VALUES " +
                     "(@VNID,@AID,@CID,@Note)";
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.AddParameter("@VNID", VNID);
        command.AddParameter("@AID", AliasID);
        command.AddParameter("@CID", CharacterID);
        command.AddParameter("@Note", Note);
        return command;
    }

    public void LoadFromReader(IDataRecord reader)
    {
        try
        {
            VNID = Convert.ToInt32(reader["VNID"]);
            AliasID = Convert.ToInt32(reader["AID"]);
            CharacterID = Convert.ToInt32(reader["CID"]);
            Note = Convert.ToString(reader["Note"]);
        }
        catch (Exception ex)
        {
            StaticHelpers.Logger.ToFile(ex);
            throw;
        }
    }

    public override void LoadFromStringParts(string[] parts)
    {
        VNID = GetInteger(parts, "id", 1);
        AliasID = GetInteger(parts, "aid");
        CharacterID = GetInteger(parts, "cid", 1);
        Note = GetPart(parts, "note");
    }

    public override string ToString()
    {
        var alias = StaticHelpers.LocalDatabase.StaffAliases[AliasID];
        var original = string.IsNullOrWhiteSpace(alias.Original) ? "" : $" ({alias.Original})";
        return $"{alias.Name}{original}";
    }
}