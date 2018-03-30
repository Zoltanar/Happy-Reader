using System;
using System.Text;
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
			    HookManager.MergeByHookCode = value;
			    OnPropertyChanged();
		    }
	    }

	    public override Encoding PrefEncoding
	    {
		    get => SelectedTextThread?.PrefEncoding ?? Encoding.Unicode;
		    set
		    {
			    SelectedTextThread.PrefEncoding = value;
			    if(_mainViewModel?.UserGame != null) _mainViewModel.UserGame.PrefEncoding = value;
			    OnPropertyChanged();
		    }
	    }

		public IthViewModel(MainWindowViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }
		
	    public void SetHookCode()
	    {
		    _mainViewModel.UserGame.SaveHookCode(SelectedTextThread.HookCode);
		}

    }
}