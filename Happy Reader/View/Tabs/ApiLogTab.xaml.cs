using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core;
using Happy_Reader.ViewModel;
using Newtonsoft.Json;

namespace Happy_Reader.View.Tabs
{
	public partial class ApiLogTab : UserControl
	{
		public ApiLogTab()
		{
			InitializeComponent();
		}

		private void SendQueryButton(object sender, RoutedEventArgs e)
		{
			StaticHelpers.Conn.SendQuery(QueryTextBox.Text);
		}

		private void ClearQueryButton(object sender, RoutedEventArgs e)
		{
			QueryTextBox.Clear();
		}
	}

	public class ApiLogViewModel
	{
		public bool AdvancedMode
		{
			get => StaticMethods.GSettings.AdvancedMode;
			set => StaticMethods.GSettings.AdvancedMode = value;
		}

		[JsonIgnore]
		public BindingList<string> VndbQueries { get; set; }
		[JsonIgnore]
		public BindingList<string> VndbResponses { get; set; }
	}
}
