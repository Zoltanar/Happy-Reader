using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using Happy_Apps_Core.Database;
using Happy_Reader.View.Converters;

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
			var titles = producer.Titles.OrderBy(c=>c.ReleaseDate).ToList();
			TimeActiveLabel.Content = titles.Any() ? $"{titles.First().ReleaseDate.Year:0000}-{titles.Last().ReleaseDate.Year:0000}" : "No Titles.";
			TitlesLabel.Content = titles.Any() ? $"{titles.Count} Titles." : string.Empty;
			var series = new List<KeyValuePair<DateTime, double>>();
			var scoreSeriesData = new List<KeyValuePair<DateTime, double>>();
			foreach (var vn in titles)
			{
				if(vn.UserVN?.Vote.HasValue ?? false) scoreSeriesData.Add(new KeyValuePair<DateTime, double>(vn.ReleaseDate, vn.UserVN.Vote.Value));
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
			if(scoreSeriesData.Any()) ReleaseChart.Series.Add(scoreSeries);
			ReleaseChart.DataContext = ReleaseSeries.ItemsSource;
			var averageRating = series.Any() ? ScoreConverter.Instance.Convert(series.Average(p => p.Value), typeof(string), null, CultureInfo.CurrentCulture) : "N/A";
			var years3Ago = DateTime.Now.AddYears(-3);
			var recentTitles = series.Where(p => p.Key >= years3Ago).ToList();
			var recentAverageRating = recentTitles.Any() ? ScoreConverter.Instance.Convert(recentTitles.Average(p => p.Value), typeof(string), null, CultureInfo.CurrentCulture) : "N/A";
			var yourScore = scoreSeriesData.Any() ? ScoreConverter.Instance.Convert(scoreSeriesData.Average(p=>p.Value), typeof(string), null, CultureInfo.CurrentCulture) : "N/A";
			RatingLabel.Content = $"Average Rating: {averageRating} (3Y = {recentAverageRating}); Your Score: {yourScore}";
		}
	}
}
