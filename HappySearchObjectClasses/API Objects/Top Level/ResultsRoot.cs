using System.Collections.Generic;
using JetBrains.Annotations;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From all get commands
    /// </summary>
    /// <typeparam name="T">Type of object contained in Items</typeparam>
    [UsedImplicitly]
    public class ResultsRoot<T>
    {
        public List<T> Items { get; set; }
        public bool More { get; set; }
        public int Num { get; set; }
    }
}