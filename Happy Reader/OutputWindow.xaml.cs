namespace Happy_Reader
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow
    {

        public OutputWindow()
        {
            InitializeComponent();
        }

        public void SetText(string text)
        {
                CharacterLabel.Content = "<>";
                OutputTextBlock.Text = text;
        }

        internal void SetText(string character, string text)
        {
            CharacterLabel.Content = character;
            OutputTextBlock.Text = text;
        }

        internal void SetLocation(int left, int bottom, int width)
        {
                Left = left;
                Top = bottom;
                Width = width;
        }
    }
}
