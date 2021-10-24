using System.Windows;
using System.Windows.Controls;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	public partial class InfoTab : UserControl
	{
		public InfoTab()
		{
			InitializeComponent();
		}

		private void DeleteOldCachedTranslations(object sender, RoutedEventArgs e) => ((InformationViewModel) DataContext).DeletedCachedTranslations(false);

		private void DeleteAllCachedTranslations(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show($"Are you sure you want to delete all cached translations?", "Happy Reader - Confirm", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
			{
				((InformationViewModel) DataContext).DeletedCachedTranslations(true);
			}
		}
	}
}
