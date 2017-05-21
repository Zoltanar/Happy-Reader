using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Reader.Database
{

    public class UserGame
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public string UserDefinedName { get; set; }

        [Required]
        public string Language { get; set; }

        public string FileName { get; set; }

        public string FolderName { get; set; }

        public string WindowName { get; set; }

        public bool IgnoresRepeat { get; set; }
    }
}
