using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Apps_Core.Database
{
    [Table("userlist")]
    public class UserVN
    {
        [Key,Column(Order = 0)]
        public int VNID { get; set; }

        [Key, Column(Order = 1)]
        public int UserId { get; set; }

        public virtual ListedVN ListedVN { get; set; }

        public virtual User User { get; set; }

        public UserlistStatus? ULStatus { get; set; }

        public int ULAdded { get; set; }

        public string ULNote { get; set; }

        public WishlistStatus? WLStatus { get; set; }

        public int WLAdded { get; set; }

        public int Vote { get; set; }

        public int VoteAdded { get; set; }
    }
}