namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class GameItemReady : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Games", "Tags", c => c.String());
            AddColumn("dbo.Games", "Series", c => c.String());
            AddColumn("dbo.Games", "Writers", c => c.String());
            AddColumn("dbo.Games", "Banner", c => c.String());
            AddColumn("dbo.Games", "Okazu", c => c.Boolean(nullable: false));
            AddColumn("dbo.Games", "SDArtists", c => c.String());
            AlterColumn("dbo.Games", "Title", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Games", "Title", c => c.String());
            DropColumn("dbo.Games", "SDArtists");
            DropColumn("dbo.Games", "Okazu");
            DropColumn("dbo.Games", "Banner");
            DropColumn("dbo.Games", "Writers");
            DropColumn("dbo.Games", "Series");
            DropColumn("dbo.Games", "Tags");
        }
    }
}
