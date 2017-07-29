using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Reader.Database
{
    /// <summary>
    /// A per-user object
    /// </summary>
    public class GameHook
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long GameId { get; set; }

        public int Context { get; set; }

        public string Name { get; set; }

        public bool Allowed { get; set; }

        public virtual Game Game { get; set; }
    }
}
