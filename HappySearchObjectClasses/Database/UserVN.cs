using System.ComponentModel.DataAnnotations;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Happy_Apps_Core.Database
{
    public class UserVN
    {
        [Key]
        public int Id { get; set; }
        
        public int VNID { get; set; }
        
        public int UserId { get; set; }
        
        public virtual User User { get; set; }

        public UserlistStatus? ULStatus { get; set; }

        public int? ULAdded { get; set; }

        public string ULNote { get; set; }

        public WishlistStatus? WLStatus { get; set; }

        public int? WLAdded { get; set; }

        public int? Vote { get; set; }

        public int? VoteAdded { get; set; }
    }
}