namespace Happy_Apps_Core.Database
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class CharacterStaff
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public int AliasId { get; set; }
        public int ListedVNId { get; set; }
        public string Note { get; set; }
        public virtual CharacterItem CharacterItem { get; set; }

        public static CharacterStaff From(CharacterItem.StaffItem cStaff)
        {
            var result = new CharacterStaff
            {
                StaffId = cStaff.ID,
                AliasId = cStaff.AID,
                ListedVNId = cStaff.VID,
                Note = cStaff.Note
            };
            return result;
        }
    }
}
