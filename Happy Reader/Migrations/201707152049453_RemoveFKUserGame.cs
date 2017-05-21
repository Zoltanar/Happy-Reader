namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveFKUserGame : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Entries", "UserId", "dbo.Users");
            DropForeignKey("dbo.Entries", "GameId", "dbo.Games");
        }
        
        public override void Down()
        {
            AddForeignKey("dbo.Entries", "UserId", "dbo.Users","Id");
            AddForeignKey("dbo.Entries", "GameId", "dbo.Games", "Id");
        }
    }
}
