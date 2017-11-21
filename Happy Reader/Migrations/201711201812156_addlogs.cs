namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addlogs : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Logs",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Kind = c.Int(nullable: false),
                        AssociatedId = c.Long(nullable: false),
                        Data = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Logs");
        }
    }
}
