namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovedIdentities : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Entries", "GameId", "dbo.Games");
            DropPrimaryKey("dbo.Entries");
            DropPrimaryKey("dbo.Games");
            AlterColumn("dbo.Entries", "Id", c => c.Long(nullable: false));
            AlterColumn("dbo.Games", "Id", c => c.Long(nullable: false));
            AddPrimaryKey("dbo.Entries", "Id");
            AddPrimaryKey("dbo.Games", "Id");
            AddForeignKey("dbo.Entries", "GameId", "dbo.Games", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Entries", "GameId", "dbo.Games");
            DropPrimaryKey("dbo.Games");
            DropPrimaryKey("dbo.Entries");
            AlterColumn("dbo.Games", "Id", c => c.Long(nullable: false, identity: true));
            AlterColumn("dbo.Entries", "Id", c => c.Long(nullable: false, identity: true));
            AddPrimaryKey("dbo.Games", "Id");
            AddPrimaryKey("dbo.Entries", "Id");
            AddForeignKey("dbo.Entries", "GameId", "dbo.Games", "Id");
        }
    }
}
