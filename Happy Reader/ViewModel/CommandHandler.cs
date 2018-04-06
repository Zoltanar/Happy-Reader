using System;
using System.Windows.Input;

namespace Happy_Reader.ViewModel
{
	public class CommandHandler : ICommand
	{
		private readonly Action _action;
		private readonly bool _canExecute;
		public CommandHandler(Action action, bool canExecute)
		{
			_action = action;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter) => _canExecute;

		public void Execute(object parameter) => _action();

#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
	}
}