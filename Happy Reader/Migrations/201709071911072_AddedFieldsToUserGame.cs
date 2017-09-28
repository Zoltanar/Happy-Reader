namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedFieldsToUserGame : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserGames", "VNID", c => c.Int());
            AddColumn("dbo.UserGames", "FilePath", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserGames", "FilePath");
            DropColumn("dbo.UserGames", "VNID");
        }
    }
}
