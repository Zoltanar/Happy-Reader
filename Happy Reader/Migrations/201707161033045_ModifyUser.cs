namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModifyUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "Language", c => c.String());
            AddColumn("dbo.Users", "Gender", c => c.String());
            AddColumn("dbo.Users", "Homepage", c => c.String());
            AddColumn("dbo.Users", "Avatar", c => c.String());
            AddColumn("dbo.Users", "Color", c => c.String());
            AddColumn("dbo.Users", "TermLevel", c => c.Int());
            AddColumn("dbo.Users", "CommentLevel", c => c.Int());
            AlterColumn("dbo.Users", "Username", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Users", "Username", c => c.String(nullable: false, maxLength: 50));
            DropColumn("dbo.Users", "CommentLevel");
            DropColumn("dbo.Users", "TermLevel");
            DropColumn("dbo.Users", "Color");
            DropColumn("dbo.Users", "Avatar");
            DropColumn("dbo.Users", "Homepage");
            DropColumn("dbo.Users", "Gender");
            DropColumn("dbo.Users", "Language");
        }
    }
}
