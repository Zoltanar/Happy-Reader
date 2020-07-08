using System.Collections.Generic;
using JetBrains.Annotations;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get release commands
    /// </summary>
    [UsedImplicitly]
    public class ReleaseItem
    {
        public List<ProducerItem> Producers { get; set; }
        public List<VNItem> VN { get; set; }
        public int ID { get; set; }
        public string Released { get; set; }
        public string Title { get; set; }
        public bool Patch { get; set; }
        public bool Freeware { get; set; }
        public string Type { get; set; }
        public string Original { get; set; }
        public string[] Languages { get; set; }
        public bool Doujin { get; set; }

        public override string ToString()
        {
            return $"{Title} \t({Released})";
        }
    }
}