using JetBrains.Annotations;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get vnlist commands
    /// </summary>
    [UsedImplicitly]
    public class UserListItem
    {
        public UserListItem(int vn, int status, int added, string notes)
        {
            VN = vn;
            Status = status;
            Added = added;
            Notes = notes;
        }

        public int VN { get; set; }
        public int Status { get; set; }
        public int Added { get; set; }
        public string Notes { get; set; }
    }
}