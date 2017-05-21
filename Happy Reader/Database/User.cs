using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Reader.Database
{


    public class User
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public User()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Entries = new HashSet<Entry>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Language { get; set; }

        public string Gender { get; set; }

        public string Homepage { get; set; }

        public string Avatar { get; set; }

        public string Color { get; set; }

        public int? TermLevel { get; set; }

        public int? CommentLevel { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Entry> Entries { get; set; }
    }
}
