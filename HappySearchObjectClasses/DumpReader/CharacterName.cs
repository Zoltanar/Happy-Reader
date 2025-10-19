using Happy_Apps_Core.Database;

namespace Happy_Apps_Core.DumpReader
{
    public class CharacterName : DumpItem
    {
        public int ID { get; private set; }
        public string Latin { get; private set; }
        public string Name { get; private set; }
        public string Language { get; private set; }

        public override void LoadFromStringParts(string[] parts)
        {
            ID = GetInteger(parts, "id", 1);
            Latin = GetPartOrNull(parts, "latin");
            Name = GetPartOrNull(parts, "name");
            Language = GetPartOrNull(parts, "lang");
        }
    }
}
