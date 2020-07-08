using JetBrains.Annotations;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get votelist commands
    /// </summary>
    [UsedImplicitly]
    public class VoteListItem
    {
        public VoteListItem(int vn, int vote, int added)
        {
            VN = vn;
            Vote = vote;
            Added = added;
        }

        public int VN { get; set; }
        public int Vote { get; set; }
        public int Added { get; set; }
    }
}