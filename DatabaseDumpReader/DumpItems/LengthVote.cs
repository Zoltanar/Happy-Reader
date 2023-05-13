using System.Linq;
using Happy_Apps_Core.Database;


namespace DatabaseDumpReader.DumpItems
{
    internal class LengthVote : DumpItem
    {
        public override void LoadFromStringParts(string[] parts)
        {
            VNId = GetInteger(parts, "vid", 1);
            Length = GetInteger(parts, "length");
            //Speed = GetInteger(parts, "speed");
            var releaseIds = GetPartOrNull(parts, "rid");
            if (releaseIds != null)
            {
                releaseIds = releaseIds.Trim('{', '}');
                ReleaseIds = releaseIds.Split(',').Select(p=>int.Parse(p.Substring(1))).ToArray();
            }
        }

        public int VNId { get; set; }
        public int Length { get; set; }
        //public int Speed { get; set; }
        public int[] ReleaseIds { get; set; }
    }
}
