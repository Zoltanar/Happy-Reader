using System;
using System.Text;
using System.Windows.Input;
using IthVnrSharpLib;

namespace Happy_Reader.ViewModel
{
	[Serializable]
	public class IthViewModel : IthVnrViewModel
	{
		// ReSharper disable once NotAccessedField.Local
		private readonly MainWindowViewModel _mainViewModel;
		public override bool MergeByHookCode
		{
			get => HookManager?.MergeByHookCode ?? false;
			set
			{
				_mainViewModel.UserGame.MergeByHookCode = value;
				if(HookManager != null) HookManager.MergeByHookCode = value;
				OnPropertyChanged();
			}
		}

		public override Encoding PrefEncoding
		{
			get => SelectedTextThread?.PrefEncoding ?? Encoding.Unicode;
			set
			{
				if (SelectedTextThread != null) SelectedTextThread.PrefEncoding = value;
				if (_mainViewModel?.UserGame != null) _mainViewModel.UserGame.PrefEncoding = value;
				OnPropertyChanged();
			}
		}
		
		public ICommand SetHookCodeCommand { get; }
		public ICommand SetDefaultHookCommand { get; }

		public bool Paused
		{
			get => _mainViewModel.TranslatePaused;
			set => _mainViewModel.TranslatePaused = value;
		}

		public IthViewModel(MainWindowViewModel mainViewModel)
		{
			_mainViewModel = mainViewModel;
			SetHookCodeCommand = new IthCommandHandler(SetHookCode);
			SetDefaultHookCommand = new IthCommandHandler(SetDefaultHook);
		}

		public void SetHookCode() => _mainViewModel.UserGame?.SaveHookCode(SelectedTextThread.HookCode, SelectedTextThread.HookFull);
		public void SetDefaultHook() => _mainViewModel.UserGame?.SaveHookCode(null, SelectedTextThread.HookFull);

	}
}