using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Happy_Apps_Core;
using Happy_Apps_Core.Translation;
using Happy_Reader.ViewModel;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Reader.View.Tabs
{
	public partial class SettingsTab : UserControl
	{
		private SettingsViewModel ViewModel => DataContext as SettingsViewModel ?? throw new ArgumentNullException($"Expected view model to be of type {nameof(SettingsViewModel)}");

		public SettingsTab() => InitializeComponent();

		private void SetMaxInputSize(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			ViewModel.TranslatorSettings.MaxOutputSize = (int)((Slider)e.Source).Value;
		}

		private void PasswordChanged(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var pwBox = (PasswordBox)sender;
			SavePassword(pwBox.Password.ToCharArray());
			LoginResponseBlock.Text = "Saved new password.";
		}

		private void LogInWithDetails(object sender, RoutedEventArgs e)
		{
			var password = LoadPassword();
			var response = Conn.Login(password != null
				? new VndbConnection.LoginCredentials(ClientName, ClientVersion, CSettings.Username, password)
				: new VndbConnection.LoginCredentials(ClientName, ClientVersion), false);
			LoginResponseBlock.Text = response;
		}

		private void OnNsfwToggle(object sender, RoutedEventArgs e)
		{
			//refresh images of active objects.
			StaticMethods.MainWindow.ViewModel.RefreshActiveObjectImages();
		}

		private void SettingsTab_OnLoaded(object sender, RoutedEventArgs e)
		{
			OriginalColorSelector.TrySetColor();
			RomajiColorSelector.TrySetColor();
			TranslatedColorSelector.TrySetColor();
		}

		public void LoadTranslationPlugins(IEnumerable<ITranslator> translators)
		{
			foreach (var translator in translators)
			{
				if (translator.Properties.Count == 0) continue;
				var settingsPanel = new StackPanel() { Orientation = Orientation.Vertical };
				foreach (var property in translator.Properties) settingsPanel.Children.Add(GetPropertyControl(translator.SetProperty, property.Key, property.Value, translator.GetProperty(property.Key)));
				var header = $"{translator.SourceName} ({translator.Version})";
				var groupBox = new GroupBox { Header = header, Content = settingsPanel };
				PluginSettingsPanel.Children.Add(groupBox);
			}
		}

		private UIElement GetPropertyControl(Action<string, object> action, string header, Type type, object value)
		{
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			var tb = new TextBlock(new Run(header));
			tb.SetValue(Grid.ColumnProperty, 0);
			grid.Children.Add(tb);
			var valueControl = CreateControlForType(action, header, type, value);
			valueControl.SetValue(Grid.ColumnProperty, 1);
			grid.Children.Add(valueControl);
			return grid;
		}


		private FrameworkElement CreateControlForType(Action<string, object> action, string header, Type type, object value)
		{
			FrameworkElement control;
			if (type.IsEnum)
			{
				var enumArray = StaticMethods.GetEnumValues(type);
				var selectedIndex = enumArray.Select(e => e.Tag).ToList().FindIndex(t => t == value);
				var cb = new ComboBox
				{
					ItemsSource = enumArray,
					SelectedIndex = Math.Max(selectedIndex, 0),
					SelectedValuePath = nameof(Tag)
				};
				cb.SelectionChanged += (_, e) => action(header, e.AddedItems[0]);
				control = cb;
			}
			else
			{
				if (type == typeof(bool))
				{
					var cb = new CheckBox { Content = header, IsChecked = value is true };
					cb.Checked += (_, _) => action(header, true);
					cb.Unchecked += (_, _) => action(header, false);
					control = cb;
				}
				else if (type == typeof(string))
				{
					var tb = new TextBox() { Text = value.ToString() };
					tb.LostFocus += (s, _) => action(header, ((TextBox)s).Text);
					tb.KeyUp += (s, ka) =>
					{
						if (ka.Key != Key.Enter) return;
						action(header, ((TextBox)s).Text);
					};
					control = tb;
				}
				else if (type == typeof(int))
				{
					var tb = new TextBox() { Text = value.ToString() };
					tb.LostFocus += (s, _) => action(header, int.Parse(((TextBox)s).Text));
					tb.KeyUp += (s, ka) =>
					{
						if (ka.Key != Key.Enter) return;
						action(header, int.Parse(((TextBox)s).Text));
					};
					tb.PreviewTextInput += NumberValidationTextBox;
					control = tb;
				}
				else control = new TextBlock(new Run($"Unsupported property type '{type.Name}'."));
			}
			return control;
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}


		private void OnDecimalVoteToggle(object sender, RoutedEventArgs e)
		{
			StaticMethods.MainWindow.ViewModel.RefreshActiveObjectUserVns();
		}
	}
}
