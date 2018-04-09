using System.Windows.Controls;
using Happy_Apps_Core.Database;

namespace Happy_Reader.View.Tiles
{
    /// <summary>
    /// Interaction logic for VNTile.xaml
    /// </summary>
    public partial class VNTile : UserControl
    {
        //private static readonly List<TimeSpan> ConstructorTimes = new List<TimeSpan>();
        //public static double AverageConstructorTime => ConstructorTimes.Average(x=> x.TotalMilliseconds);
        public VNTile(ListedVN vn)
        {
            //var watch = Stopwatch.StartNew();
            InitializeComponent();
            DataContext = vn;
            //ConstructorTimes.Add(watch.Elapsed);
        }

        public static VNTile FromListedVN(ListedVN vn)
        {
            return new VNTile(vn);
        }
    }
}
