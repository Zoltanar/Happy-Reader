using System.Windows;
using System.Windows.Controls;

namespace Happy_Reader.View
{
	public partial class LabeledTextBox : UserControl
	{
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(nameof(Text), typeof(string), typeof(LabeledTextBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		public static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register(nameof(Label), typeof(string), typeof(LabeledTextBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty LabelWidthProperty =
			DependencyProperty.Register(nameof(LabelWidth), typeof(string), typeof(LabeledTextBox), new UIPropertyMetadata(null));

		public static readonly DependencyProperty LabelHorizontalAlignmentProperty =
			DependencyProperty.Register(nameof(LabelHorizontalAlignmentProperty), typeof(HorizontalAlignment), typeof(LabeledTextBox), new UIPropertyMetadata(null));


		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public string Label
		{
			get => (string)GetValue(LabelProperty);
			set => SetValue(LabelProperty, value);
		}

		public string LabelWidth
		{
			get => (string)GetValue(LabelWidthProperty) ?? "100";
			set => SetValue(LabelWidthProperty, value);
		}

		public HorizontalAlignment LabelHorizontalAlignment
		{
			get => (HorizontalAlignment)GetValue(LabelHorizontalAlignmentProperty);
			set => SetValue(LabelHorizontalAlignmentProperty, value);
		}



		public LabeledTextBox() => InitializeComponent();
	}
}
