using System.ComponentModel.DataAnnotations.Schema;

namespace Happy_Apps_Core.Database
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class CharacterVN
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int RId { get; set; }
        public int Spoiler { get; set; }
        public string Role { get; set; }
        public virtual CharacterItem CharacterItem { get; set; }
        public int ListedVNId { get; set; }
        //public virtual ListedVN ListedVN { get; set; }

        public static CharacterVN From(CharacterItem.VNItem cvn)
        {
            var result = new CharacterVN
            {
                ListedVNId = cvn.ID,
                RId = cvn.RID,
                Spoiler = cvn.Spoiler,
                Role = cvn.Role
            };
            return result;
        }

        public override string ToString() => $"[VNID: {ListedVNId}, CID: {CharacterItem.ID}] {CharacterItem.Name}";
    }
}