using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database;

public class StaffItem : DumpItem, IDataItem<int>
{
    public int ID { get; set; }
    public int AliasID { get; set; }
    public string Gender { get; set; }
    public string Language { get; set; }
    public string Description { get; set; }

    public string KeyField => nameof(ID);
    public int Key => ID;

    public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
    {
        string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO {nameof(StaffItem)}s " +
                     "(ID,AID,Gender,Lang,Desc) VALUES " +
                     "(@ID,@AID,@Gender,@Lang,@Desc)";
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.AddParameter("@ID", ID);
        command.AddParameter("@AID", AliasID);
        command.AddParameter("@Gender", Gender);
        command.AddParameter("@Lang", Language);
        command.AddParameter("@Desc", Description);
        return command;
    }

    public void LoadFromReader(IDataRecord reader)
    {
        try
        {
            ID = Convert.ToInt32(reader["ID"]);
            AliasID = Convert.ToInt32(reader["AID"]);
            Gender = Convert.ToString(reader["Gender"]);
            Language = Convert.ToString(reader["Lang"]);
            Description = Convert.ToString(reader["Desc"]);
        }
        catch (Exception ex)
        {
            StaticHelpers.Logger.ToFile(ex);
            throw;
        }
    }

    public override void LoadFromStringParts(string[] parts)
    {
        ID = GetInteger(parts, "id", 1);
        AliasID = GetInteger(parts, "aid");
        Gender = GetPart(parts, "gender");
        Language = GetPart(parts, "lang");
        Description = GetPart(parts, "desc");
    }
}