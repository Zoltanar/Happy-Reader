using System;
using System.Data;
using System.Data.Common;
using Happy_Apps_Core.DataAccess;

namespace Happy_Apps_Core.Database;

/// <summary>
/// Key is (CharacterItemId, TraitId)
/// </summary>
public sealed class DbTrait : DumpItem, IDataGroupItem<int>
{
    // ReSharper disable once InconsistentNaming
    public int CharacterItem_Id { get; set; }
    public int TraitId { get; set; }
    public int Spoiler { get; set; }

    #region IDataItem Implementation
    public int GroupKey => CharacterItem_Id;

    public DbCommand UpsertCommand(DbConnection connection, bool insertOnly)
    {
        string sql = $@"INSERT {(insertOnly ? string.Empty : "OR REPLACE ")}INTO DbTraits (TraitId,Spoiler,CharacterItem_Id) VALUES (@TraitId,@Spoiler,@CharacterItem_Id)";
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.AddParameter("@TraitId", TraitId);
        command.AddParameter("@Spoiler", Spoiler);
        command.AddParameter("@CharacterItem_Id", CharacterItem_Id);
        return command;
    }

    public void LoadFromReader(IDataRecord reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        try
        {
            CharacterItem_Id = Convert.ToInt32(reader["CharacterItem_Id"]);
            TraitId = Convert.ToInt32(reader["TraitId"]);
            Spoiler = Convert.ToInt32(reader["Spoiler"]);
        }
        catch (Exception ex)
        {
            StaticHelpers.Logger.ToFile(ex);
            throw;
        }
    }
    #endregion

    public override void LoadFromStringParts(string[] parts)
    {
        CharacterItem_Id = GetInteger(parts, "id", 1);
        TraitId = GetInteger(parts, "tid", 1);
        Spoiler = GetInteger(parts, "spoil");
    }
}