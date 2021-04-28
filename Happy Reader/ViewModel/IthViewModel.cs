using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Happy_Reader.Database;
using Happy_Reader.View;
using IthVnrSharpLib;

namespace Happy_Reader.ViewModel
{
	[Serializable]
	public class IthViewModel : IthVnrViewModel
	{
		// ReSharper disable once NotAccessedField.Local
		private readonly MainWindowViewModel _mainViewModel;
		public override bool IsPaused => StaticMethods.CtrlKeyIsHeld();

		public override TextThread SelectedTextThread
		{
			get
			{
				var result = Application.Current.Dispatcher.Invoke(() =>
				{
					var selectedItem = Selector.SelectedItem as FrameworkElement;
					var selectedItemTag = selectedItem?.Tag;
					return selectedItemTag as TextThread;
				});
				return result;
			}
			set
			{
				if (value == null) return;
				Application.Current.Dispatcher.Invoke(() => Selector.SelectedItem = DisplayThreads.First(t => t.Tag == value));
			}
		}

		public override bool MergeByHookCode
		{
			get => HookManager?.MergeByHookCode ?? false;
			set
			{
				if (_mainViewModel?.UserGame != null) _mainViewModel.UserGame.MergeByHookCode = value;
				if (HookManager != null) HookManager.MergeByHookCode = value;
				OnPropertyChanged();
			}
		}

		public ICommand SetHookCodeCommand { get; }

		public bool Paused
		{
			get => _mainViewModel.TranslatePaused;
			set => _mainViewModel.TranslatePaused = value;
		}

		public Selector Selector { get; set; }

		public IthViewModel(MainWindowViewModel mainViewModel, Action initializeUserGameAction)
		{
			_mainViewModel = mainViewModel;
			InitializeUserGameAction = initializeUserGameAction;
			SetHookCodeCommand = new IthCommandHandler(SetHookCode);
		}

		public override void AddNewThreadToDisplayCollection(TextThread textThread)
		{
			if (Application.Current == null) return;
			Application.Current.Dispatcher.Invoke(() =>
			{
				var displayThread = new TextThreadPanel(textThread, this) { Tag = textThread };
				DisplayThreads.Add(displayThread);
			});
			OnPropertyChanged(nameof(DisplayThreads));
		}

		public void SetHookCode() => _mainViewModel.UserGame?.SaveHookCode(SelectedTextThread.PersistentIdentifier);

		public override void AddGameThread(GameTextThread gameTextThread)
		{
			if (_mainViewModel.UserGame == null) return;
			gameTextThread.GameId = _mainViewModel.UserGame.Id;
			StaticMethods.Data.SqliteGameThreads.UpsertLater(new GameThread(gameTextThread));
		}
		
		protected override void SaveGameTextThreads()
		{
			var threads = StaticMethods.Data.SqliteGameThreads.WithKeyIn(GameTextThreads.Select(x => (x.GameId, x.Identifier)).ToArray()).ToArray();
			foreach (var gameThread in threads)
			{
				StaticMethods.Data.SqliteGameThreads.UpsertLater(gameThread);
			}
			StaticMethods.Data.SaveChanges();
		}
	}
}