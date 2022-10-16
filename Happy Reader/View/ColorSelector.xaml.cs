using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Happy_Reader.View
{
	public partial class ColorSelector : UserControl
	{
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(nameof(Text), typeof(string), typeof(ColorSelector), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set
			{
				SetValue(TextProperty, value);
				TrySetColor(value);
			}
		}


		public ColorSelector()
		{
			InitializeComponent();
		}

		private void UIElement_OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Return) return;
			TrySetColor(EntryBox.Text);
		}

		public void TrySetColor(string input = null)
		{
			if ((input ?? Text) == null)
			{
				SetError();
				return;
			}
			try
			{
				// ReSharper disable once PossibleNullReferenceException
				var color = (Color)ColorConverter.ConvertFromString(input ?? Text);
				ReplyBox.Text = string.Empty;
				ReplyBox.ToolTip = null;
				ColorBorder.Background = new SolidColorBrush(color);
			}
			catch
			{
				SetError();
			}

			void SetError()
			{
				// ReSharper disable StringLiteralTypo
				const string invalidText = "Input must be of format '#AARRGGBB' or '#RRGGBB' or known color";
				// ReSharper restore StringLiteralTypo

				ReplyBox.Text = "Invalid";
				ReplyBox.ToolTip = invalidText;
				ColorBorder.Background = Theme.InvalidColorBoxBackground;
			}
		}
	}
}
