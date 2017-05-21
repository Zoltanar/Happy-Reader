namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateTimeNullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Entries", "UpdateTime", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Entries", "UpdateTime", c => c.DateTime(nullable: false));
        }
    }
}
