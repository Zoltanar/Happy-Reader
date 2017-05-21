namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveFKGame : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Games", "SeriesID", "dbo.Series");
        }
        
        public override void Down()
        {
            AddForeignKey("dbo.Entries", "SeriesID", "dbo.Series", "Id");
        }
    }
}
