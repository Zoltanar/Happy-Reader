namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModifyUsers2 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Users", "Language", c => c.String(nullable: false));
            AlterColumn("dbo.Users", "Color", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Users", "Color", c => c.String());
            AlterColumn("dbo.Users", "Language", c => c.String());
        }
    }
}
