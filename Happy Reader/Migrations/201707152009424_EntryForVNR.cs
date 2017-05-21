namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EntryForVNR : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Entries",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.Long(nullable: false),
                        Input = c.String(nullable: false),
                        Output = c.String(),
                        Language = c.String(maxLength: 10),
                        GameId = c.Long(),
                        SeriesSpecific = c.Boolean(nullable: false),
                        Private = c.Boolean(nullable: false),
                        Priority = c.Double(nullable: false),
                        Type = c.Int(nullable: false),
                        RoleString = c.String(nullable: false),
                        Disabled = c.Boolean(nullable: false),
                        UserHash = c.Int(),
                        Host = c.String(),
                        FromLanguage = c.String(),
                        ToLanguage = c.String(),
                        Time = c.DateTime(nullable: false),
                        UpdateTime = c.DateTime(nullable: false),
                        UpdateUserId = c.Long(nullable: false),
                        CaseInsensitive = c.Boolean(nullable: false),
                        PhraseBoundary = c.Boolean(nullable: false),
                        Regex = c.Boolean(nullable: false),
                        Hentai = c.Boolean(nullable: false),
                        Context = c.String(),
                        Ruby = c.String(),
                        Comment = c.String(),
                        UpdateComment = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Games", t => t.GameId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.GameId);
            
            CreateTable(
                "dbo.Games",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        SeriesID = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Series", t => t.SeriesID)
                .Index(t => t.SeriesID);
            
            CreateTable(
                "dbo.Series",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.RoleProxies",
                c => new
                    {
                        RoleString = c.String(nullable: false, maxLength: 128),
                        In = c.String(nullable: false, maxLength: 128),
                        Out = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.RoleString, t.In, t.Out });
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Entries", "UserId", "dbo.Users");
            DropForeignKey("dbo.Games", "SeriesID", "dbo.Series");
            DropForeignKey("dbo.Entries", "GameId", "dbo.Games");
            DropIndex("dbo.Games", new[] { "SeriesID" });
            DropIndex("dbo.Entries", new[] { "GameId" });
            DropIndex("dbo.Entries", new[] { "UserId" });
            DropTable("dbo.RoleProxies");
            DropTable("dbo.Users");
            DropTable("dbo.Series");
            DropTable("dbo.Games");
            DropTable("dbo.Entries");
        }
    }
}
