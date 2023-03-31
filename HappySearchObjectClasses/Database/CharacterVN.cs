using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database;

public sealed class CharacterVN : DumpItem, IDataItem<(int, int)>, IDataListItem<int>
{
    public int CharacterId { get; set; }
    public int RId { get; set; }
    public int Spoiler { get; set; }
    public string RoleString { get; set; }
    public CharacterRole Role { get; private set; }
    public int VNId { get; set; }

    public override string ToString() => $"[CID: {CharacterId}, VNID: {VNId}]";

    public override void LoadFromStringParts(string[] parts)
    {
        CharacterId = GetInteger(parts, "id", 1);
        VNId = GetInteger(parts, "vid", 1);
        RId = 0;//Convert.ToInt32(parts[2]); //todo ??
        Spoiler = GetInteger(parts, "spoil");
        RoleString = GetPart(parts, "role");
    }

    #region IDataItem Implementation

    public string KeyField => "(CharacterId,VNID)";
    public (int, int) Key => (CharacterId, VNId);
    public int ListKey => VNId;
    public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
    {
        string sql = $"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO CharacterVNs" +
                     "(CharacterId,VNID,RId,Spoiler,Role) VALUES " +
                     "(@CharacterId,@VNID,@RId,@Spoiler,@Role)";
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.AddParameter("@CharacterId", CharacterId);
        command.AddParameter("@VNID", VNId);
        command.AddParameter("@RId", RId);
        command.AddParameter("@Spoiler", Spoiler);
        command.AddParameter("@Role", RoleString);
        return command;
    }

    public void LoadFromReader(IDataRecord reader)
    {
        CharacterId = Convert.ToInt32(reader["CharacterId"]);
        VNId = Convert.ToInt32(reader["VNID"]);
        RId = Convert.ToInt32(reader["RId"]);
        Spoiler = Convert.ToInt32(reader["Spoiler"]);
        RoleString = Convert.ToString(reader["Role"]);
        Role = Enum.TryParse(RoleString, true, out CharacterRole role) ? role : CharacterRole.Undefined;
    }
    #endregion
}