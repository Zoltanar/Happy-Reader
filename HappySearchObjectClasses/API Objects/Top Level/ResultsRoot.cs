using System.Collections.Generic;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From all get commands
    /// </summary>
    /// <typeparam name="T">Type of object contained in Items</typeparam>
    public class ResultsRoot<T>
    {
        public List<T> Items { get; set; }
        public bool More { get; set; }
        public int Num { get; set; }
    }
}