using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;

namespace Happy_Reader.View
{
	public partial class InputWindow : Window, INotifyPropertyChanged
	{

		public static readonly DependencyProperty InputLabelProperty =
			DependencyProperty.Register(nameof(InputLabel), typeof(string), typeof(InputWindow), new UIPropertyMetadata(null));

		public event PropertyChangedEventHandler PropertyChanged;

		public string InputLabel
		{
			get => (string)GetValue(InputLabelProperty);
			set => SetValue(InputLabelProperty, value);
		}

		public string InputText
		{
			get => InputTextBox.Text;
			set { InputTextBox.Text = value; OnPropertyChanged(nameof(OkEnabled)); }
		}
		
		public InputWindow() => InitializeComponent();

		/// <summary>
		/// Will toggle the OK button's enabled state based on the input text passing through the <see cref="Filter"/>.
		/// </summary>
		public bool OkEnabled => Filter(InputText);

		/// <summary>
		/// Filter to enable OK button.
		/// </summary>
		public Func<string, bool> Filter { get; set; } = _ => true;

		private void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void UpdateOkEnabled(object sender, RoutedEventArgs e) => OnPropertyChanged(nameof(OkEnabled));
		
		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
