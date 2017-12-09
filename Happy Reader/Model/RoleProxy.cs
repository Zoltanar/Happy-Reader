using System.Collections.Generic;
using Happy_Reader.Database;

namespace Happy_Reader
{
    public class RoleProxy
    {
        public string Role { get; set; }
        public Entry Entry { get; set; }
        public string FullRoleString { get; set; }
        public int Id { get; set; }
        public List<Entry> ProxyMods { get; set; } = new List<Entry>();
    }
}
