﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Apps_Core;
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

	}
}
