using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Converters;
using Happy_Reader.ViewModel;

namespace Happy_Reader.View.Tabs
{
	/// <summary>
	/// Interaction logic for ProducerTab.xaml
	/// </summary>
	public partial class ProducerTab : UserControl
	{
		public readonly ListedProducer ViewModel;
		public ProducerTab(ListedProducer producer)
		{
			ViewModel = producer;
			DataContext = producer;
			InitializeComponent();
			LoadData(producer);
		}

		private void LoadData(ListedProducer producer)
		{
			var titles = producer.Titles.OrderBy(c => c.ReleaseDate).ToList();
			TimeActiveLabel.Content = titles.Any()
				? $"{titles.First().ReleaseDate.Year:0000}-{titles.Last().ReleaseDate.Year:0000}"
				: "No Titles.";
			TitlesLabel.Content = titles.Any() ? $"{titles.Count} Titles." : string.Empty;
			var series = new List<KeyValuePair<DateTime, double>>();
			var scoreSeriesData = new List<KeyValuePair<DateTime, double>>();
			foreach (var vn in titles)
			{
				if (vn.UserVN?.Vote.HasValue ?? false)
					scoreSeriesData.Add(new KeyValuePair<DateTime, double>(vn.ReleaseDate, vn.UserVN.Vote.Value));
				if (vn.Rating == 0) continue;
				series.Add(new KeyValuePair<DateTime, double>(vn.ReleaseDate, vn.Rating));
			}
			ReleaseSeries.Title = "Release Rating";
			ReleaseSeries.ItemsSource = series.ToArray();
			var scoreSeries = new System.Windows.Controls.DataVisualization.Charting.Compatible.LineSeries
			{
				Title = "My Release Score", ItemsSource = scoreSeriesData.ToArray(),
				IndependentValuePath = "Key", DependentValuePath = "Value"
			};
			if (scoreSeriesData.Any()) ReleaseChart.Series.Add(scoreSeries);
			ReleaseChart.DataContext = ReleaseSeries.ItemsSource;
			var averageRating = series.Any()
				? ScoreConverter.Instance.Convert(series.Average(p => p.Value), typeof(string), null, CultureInfo.CurrentCulture)
				: "N/A";
			var years3Ago = DateTime.Now.AddYears(-3);
			var recentTitles = series.Where(p => p.Key >= years3Ago).ToList();
			var recentAverageRating = recentTitles.Any()
				? ScoreConverter.Instance.Convert(recentTitles.Average(p => p.Value), typeof(string), null,
					CultureInfo.CurrentCulture)
				: "N/A";
			var yourScore = scoreSeriesData.Any()
				? ScoreConverter.Instance.Convert(scoreSeriesData.Average(p => p.Value), typeof(string), null,
					CultureInfo.CurrentCulture)
				: "N/A";
			RatingLabel.Content = $"Average Rating: {averageRating} (3Y = {recentAverageRating}); Your Score: {yourScore}";
			try
			{
				LoadStaff(producer);
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
		}

		private void LoadStaff(ListedProducer producer)
		{
			//get list of staff for titles from producer, grouped by role, ordered by most contributions first.
			var orderedStaff = producer.Titles.SelectMany(t => t.Staff).GroupBy(t => t.AliasID)
				.ToDictionary(g => StaticHelpers.LocalDatabase.StaffAliases[g.Key], g => g.ToList()).Where(p => p.Value.Count > 1)
				.OrderByDescending(p => p.Value.Count).ThenBy(p => p.Key.AliasID);
			foreach (var item in orderedStaff)
			{
				int countForType = 0;
				string role = "Unknown";
				var creditRoles = item.Value.GroupBy(s => s.RoleDetail).OrderByDescending(g => g.Count());
				foreach (var creditRole in creditRoles)
				{
					var count = creditRole.Count();
					if (countForType == 0)
					{
						countForType = count;
						role = creditRole.Key;
					}
					else
					{
						if (count == countForType) role = "Various";
						break;
					}
				}
				//detail shows either most prominent role, or various if multiple roles have same number of contributions.
				StaffListBox.Items.Add(new StaffAndDetail(item.Key, $"{item.Key} ({role}) ({item.Value.Count} titles)"));
			}
		}


		private void ShowVNsForStaff(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			if (element?.DataContext is not StaffAndDetail staffDetail) return;

			StaticMethods.MainWindow.ViewModel.DatabaseViewModel.ShowForStaffWithAlias(staffDetail.Staff.AliasID);
			StaticMethods.MainWindow.SelectTab(typeof(VNTabViewModel));
		}

		private void ShowCharactersForStaff(object sender, RoutedEventArgs e)
		{
			var element = sender as FrameworkElement;
			if (element?.DataContext is not StaffAndDetail staffDetail) return;
			StaticMethods.MainWindow.ViewModel.CharactersViewModel.ShowForStaffWithAlias(staffDetail.Staff.AliasID);
			StaticMethods.MainWindow.SelectTab(typeof(CharactersTabViewModel));
		}
	}
}
