namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeGamesToUserGames : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.Games", newName: "UserGames");
            DropForeignKey("dbo.Entries", "GameId", "dbo.Games");
            DropIndex("dbo.Entries", new[] { "GameId" });
            DropIndex("dbo.UserGames", new[] { "SeriesID" });
            RenameColumn(table: "dbo.UserGames", name: "SeriesID", newName: "Series_Id");
            AlterColumn("dbo.UserGames", "Series_Id", c => c.Long());
            CreateIndex("dbo.UserGames", "Series_Id");
        }
        
        public override void Down()
        {
            DropIndex("dbo.UserGames", new[] { "Series_Id" });
            AlterColumn("dbo.UserGames", "Series_Id", c => c.Long(nullable: false));
            RenameColumn(table: "dbo.UserGames", name: "Series_Id", newName: "SeriesID");
            CreateIndex("dbo.UserGames", "SeriesID");
            CreateIndex("dbo.Entries", "GameId");
            AddForeignKey("dbo.Entries", "GameId", "dbo.Games", "Id");
            RenameTable(name: "dbo.UserGames", newName: "Games");
        }
    }
}
