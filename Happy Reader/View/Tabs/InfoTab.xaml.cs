using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

		private void DeleteOldCachedTranslations(object sender, RoutedEventArgs e) => (DataContext as InformationViewModel).DeletedCachedTranslations(false);

		private void DeleteAllCachedTranslations(object sender, RoutedEventArgs e) => (DataContext as InformationViewModel).DeletedCachedTranslations(true);

	}
}
