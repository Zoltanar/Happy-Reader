using System.Collections.Generic;
using System.Linq;
using Happy_Reader.Database;

namespace Happy_Reader
{
	public class RoleProxy
	{
		public string Role { get; set; }
		public string MainRole { get; set; }
		public Entry Entry { get; set; }
        public int RepeatCount { get; set; } = 1;
		public string FullRoleString => string.Join("",Enumerable.Repeat($"[[{MainRole}#{Id}]]",RepeatCount));
		public int Id { get; set; }
		public List<Entry> ProxyMods { get; set; } = new List<Entry>();

		public override string ToString() => FullRoleString;

		public string WithId(int id) => $"[[{MainRole}#{id}]]";
	}
}
