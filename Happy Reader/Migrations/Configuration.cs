namespace Happy_Reader.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<Happy_Reader.Database.HappyReaderDatabase>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }
    }
}
