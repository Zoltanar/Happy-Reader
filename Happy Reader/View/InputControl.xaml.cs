using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;

namespace Happy_Reader.View
{
	public partial class InputControl : UserControl, INotifyPropertyChanged
	{
		public static readonly DependencyProperty InputLabelProperty =
			DependencyProperty.Register(nameof(InputLabel), typeof(string), typeof(InputControl), new UIPropertyMetadata(null));

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
		
		private Func<bool, string, Task> Callback { get; }

		public InputControl(string description, Func<bool, string, Task> callback)
		{
			InitializeComponent();
			DescriptionLabel.Content = description;
			Callback = callback;
		}

		/// <summary>
		/// Will toggle the OK button's enabled state based on the input text passing through the <see cref="Filter"/>.
		/// </summary>
		public bool OkEnabled => Filter(InputText);

		/// <summary>
		/// Filter to enable OK button.
		/// </summary>
		public Func<string, bool> Filter { get; set; } = _ => true;

		private async void OkClick(object sender, RoutedEventArgs e)
		{
			await Callback(true, InputText);
		}

		private async void CancelClick(object sender, RoutedEventArgs e)
		{
			await Callback(false, null);
		}

		private void UpdateOkEnabled(object sender, RoutedEventArgs e) => OnPropertyChanged(nameof(OkEnabled));
		
		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
