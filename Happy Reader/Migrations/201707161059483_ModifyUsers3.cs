namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModifyUsers3 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Users", "Color", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Users", "Color", c => c.String(nullable: false));
        }
    }
}
