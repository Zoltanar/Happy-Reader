namespace Happy_Reader.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveRoleProxyTable : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.RoleProxies");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.RoleProxies",
                c => new
                    {
                        RoleString = c.String(nullable: false, maxLength: 128),
                        In = c.String(nullable: false, maxLength: 128),
                        Out = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.RoleString, t.In, t.Out });
            
        }
    }
}
