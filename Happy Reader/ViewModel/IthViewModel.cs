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
				if (_mainViewModel?.UserGame != null) _mainViewModel.UserGame.GameHookSettings.MergeByHookCode = value;
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
			Notify = new Action<object, string, string>(_mainViewModel.NotificationEvent);
			SetHookCodeCommand = new IthCommandHandler(SetHookCode);
		}

		public override void AddNewThreadToDisplayCollection(TextThread textThread)
		{
			if (Application.Current == null) return;
			Application.Current.Dispatcher.Invoke(() =>
			{
				var displayThread = new TextThreadPanel(textThread);
				DisplayThreads.Add(displayThread);
			});
			OnPropertyChanged(nameof(DisplayThreads));
		}

		public void SetHookCode()
		{
			if (SelectedTextThread is not HookTextThread hookTextThread) return;
			_mainViewModel.UserGame?.GameHookSettings.SaveHookCode(hookTextThread.HookCode);
		}

		public override void AddGameThread(GameTextThread gameTextThread)
		{
			if (_mainViewModel.UserGame == null) return;
			gameTextThread.GameId = _mainViewModel.UserGame.Id;
			StaticMethods.Data.GameThreads.UpsertLater(new GameThread(gameTextThread));
		}

		public override void SaveGameTextThreads()
		{
			if (GameHookCodes.Length != 0)
			{
				GameHookCodes = Array.Empty<string>();
				return;
			}
			if (GameTextThreads == null || GameTextThreads.Count == 0) return;
			var threads = StaticMethods.Data.GameThreads.WithKeyIn(GameTextThreads.SelectToList(x => (x.GameId, x.Identifier))).ToList();
			foreach (var gameThread in threads)
			{
				StaticMethods.Data.GameThreads.UpsertLater(gameThread);
			}
			//this data will be persisted on HookedProcessOnExited callback.
			GameTextThreads = null;
		}
	}
}