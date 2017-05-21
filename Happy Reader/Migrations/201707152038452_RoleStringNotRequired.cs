namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RoleStringNotRequired : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Entries", "UserId", "dbo.Users");
            AlterColumn("dbo.Entries", "RoleString", c => c.String());
            AddForeignKey("dbo.Entries", "UserId", "dbo.Users", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Entries", "UserId", "dbo.Users");
            AlterColumn("dbo.Entries", "RoleString", c => c.String(nullable: false));
            AddForeignKey("dbo.Entries", "UserId", "dbo.Users", "Id");
        }
    }
}
