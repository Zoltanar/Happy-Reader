namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AdjustedGameForXml : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Games", "UserDefinedName", c => c.String());
            AddColumn("dbo.Games", "Language", c => c.String(nullable: false));
            AddColumn("dbo.Games", "FileName", c => c.String());
            AddColumn("dbo.Games", "FolderName", c => c.String());
            AddColumn("dbo.Games", "WindowName", c => c.String());
            AddColumn("dbo.Games", "IgnoresRepeat", c => c.Boolean(nullable: false));
            DropColumn("dbo.Entries", "Language");
            DropColumn("dbo.Games", "Name");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Games", "Name", c => c.String(nullable: false));
            AddColumn("dbo.Entries", "Language", c => c.String(maxLength: 10));
            DropColumn("dbo.Games", "IgnoresRepeat");
            DropColumn("dbo.Games", "WindowName");
            DropColumn("dbo.Games", "FolderName");
            DropColumn("dbo.Games", "FileName");
            DropColumn("dbo.Games", "Language");
            DropColumn("dbo.Games", "UserDefinedName");
        }
    }
}
