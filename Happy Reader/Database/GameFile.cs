using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Reader.Database
{
    public class GameFile
    {

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        // ReSharper disable once InconsistentNaming
        public string MD5 { get; set; }

        public long GameId { get; set; }
    }
}
