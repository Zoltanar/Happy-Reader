namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGameItem : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Games",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        Timestamp = c.DateTime(nullable: false),
                        Title = c.String(),
                        RomajiTitle = c.String(),
                        Brand = c.String(),
                        Image = c.String(),
                        Wiki = c.String(),
                        Date = c.String(),
                        Artists = c.String(),
                        Musicians = c.String(),
                        Otome = c.Boolean(nullable: false),
                        Ecchi = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Games");
        }
    }
}
