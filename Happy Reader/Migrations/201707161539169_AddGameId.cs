namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGameId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Entries", "GameId", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Entries", "GameId");
        }
    }
}
