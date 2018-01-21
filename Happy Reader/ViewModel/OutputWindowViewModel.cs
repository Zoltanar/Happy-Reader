using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Happy_Reader.View;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
    public sealed class OutputWindowViewModel : INotifyPropertyChanged
    {
        public RichTextBox TextArea { get; set; }

        public MainWindow MainWindow { get; set; }

        public MainWindowViewModel MainViewModel { get; set; }

        public OutputWindowViewModel()
        {
            AddEntryForText = new CommandHandler(AddEntry, true);
        }

        public void Initialize(MainWindow mainWindow, RichTextBox debugTextbox)
        {
            TextArea = debugTextbox;
            MainWindow = mainWindow;
            MainViewModel = (MainWindowViewModel)mainWindow.DataContext;
            OnPropertyChanged(nameof(TranslateOn));
        }

        private void AddEntry()
        {
            MainWindow.AddEntryFromOutputWindow(TextArea.Selection.Text);
        }

        public ICommand AddEntryForText { get; set; }

        public bool TranslateOn
        {
            get => MainViewModel?.TranslateOn ?? true;
            set => MainViewModel.TranslateOn = value;
        }

        private class CommandHandler : ICommand
        {
            private readonly Action _action;
            private readonly bool _canExecute;
            public CommandHandler(Action action, bool canExecute)
            {
                _action = action;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                _action();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}