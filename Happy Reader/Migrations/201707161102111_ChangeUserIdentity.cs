namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeUserIdentity : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Entries", "UserId", "dbo.Users");
            DropPrimaryKey("dbo.Users");
            AlterColumn("dbo.Users", "Id", c => c.Long(nullable: false));
            AddPrimaryKey("dbo.Users", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Entries", "UserId", "dbo.Users");
            DropPrimaryKey("dbo.Users");
            AlterColumn("dbo.Users", "Id", c => c.Long(nullable: false, identity: true));
            AddPrimaryKey("dbo.Users", "Id");
            AddForeignKey("dbo.Entries", "UserId", "dbo.Users", "Id", cascadeDelete: true);
        }
    }
}
