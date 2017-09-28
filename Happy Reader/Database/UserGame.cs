using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using Happy_Apps_Core;

namespace Happy_Reader.Database
{

    public class UserGame
    {
        public UserGame(string file, ListedVN vn)
        {
            FilePath = file;
            FileName = Path.GetFileName(file);
            FolderName = Path.GetDirectoryName(file);
            Language = vn.Languages.Originals.FirstOrDefault();
            VNID = vn.VNID;
            VN = vn;
        }

        public UserGame() { }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public string UserDefinedName { get; set; }

        [Required]
        public string Language { get; set; }

        public string FileName { get; set; }

        public string FolderName { get; set; }

        public string WindowName { get; set; }

        public bool IgnoresRepeat { get; set; }

        public int? VNID { get; set; }

        public string FilePath { get; set; }

        [NotMapped]
        public ListedVN VN { get; set; }
    }
}
