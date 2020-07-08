using JetBrains.Annotations;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get wishlist commands
    /// </summary>
    [UsedImplicitly]
    public class WishListItem
    {
        public WishListItem(int vn, int priority, int added)
        {
            VN = vn;
            Priority = priority;
            Added = added;
        }

        public int VN { get; set; }
        public int Priority { get; set; }
        public int Added { get; set; }
    }

}
