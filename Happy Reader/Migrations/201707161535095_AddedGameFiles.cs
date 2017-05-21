namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedGameFiles : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.UserGames", "Series_Id", "dbo.Series");
            DropIndex("dbo.UserGames", new[] { "Series_Id" });
            CreateTable(
                "dbo.GameFiles",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        MD5 = c.String(),
                        GameId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Entries", "FileId", c => c.Long());
            DropColumn("dbo.Entries", "GameId");
            DropColumn("dbo.UserGames", "Series_Id");
            DropTable("dbo.Series");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.Series",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.UserGames", "Series_Id", c => c.Long());
            AddColumn("dbo.Entries", "GameId", c => c.Long());
            DropColumn("dbo.Entries", "FileId");
            DropTable("dbo.GameFiles");
            CreateIndex("dbo.UserGames", "Series_Id");
            AddForeignKey("dbo.UserGames", "Series_Id", "dbo.Series", "Id");
        }
    }
}
