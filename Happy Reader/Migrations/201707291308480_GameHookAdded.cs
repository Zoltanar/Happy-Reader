namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class GameHookAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GameHooks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GameId = c.Long(nullable: false),
                        Context = c.Int(nullable: false),
                        Name = c.String(),
                        Allowed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Games", t => t.GameId, cascadeDelete: true)
                .Index(t => t.GameId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.GameHooks", "GameId", "dbo.Games");
            DropIndex("dbo.GameHooks", new[] { "GameId" });
            DropTable("dbo.GameHooks");
        }
    }
}
