using System.Collections.Generic;
using System.Linq;

namespace Happy_Apps_Core.Database
{
    public abstract class DumpItem
    {
        protected static Dictionary<string, int> Headers = new();

        public string GetPart(string[] parts, string columnName) => parts[Headers[columnName]];

        public abstract void LoadFromStringParts(string[] parts);

        public virtual void SetDumpHeaders(string[] parts)
        {
            int colIndex = 0;
            Headers = parts.ToDictionary(c => c, _ => colIndex++);
        }
    }
}