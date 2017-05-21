using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using Happy_Reader.Properties;

namespace Happy_Reader
{
    public class HookInfo : INotifyPropertyChanged
    {
        public HookInfo(int contextId, string name, ElapsedEventHandler timerAction)
        {
            ContextId = contextId;
            Timer = new NamedTimer
            {
                Context = contextId,
                Interval = 20
            };
            Timer.Elapsed += timerAction;
            Text = new StringBuilder(100);
            Name = name;
            Parts = new List<string>();
        }

        public List<string> Parts { get; }

        public int ContextId { get; set; }

        public NamedTimer Timer { get; }
        private StringBuilder _text;

        public StringBuilder Text
        {
            get => _text;
            private set => _text = value;
        }

        private string _displayText;

        public string DisplayText
        {
            get => _displayText;
            set
            {
                _displayText = value;
                OnPropertyChanged();
            }
        }
        public string Name { get; }
        private bool _allowed;
        public bool Allowed
        {
            get => _allowed;
            set
            {
                _allowed = value;
                OnPropertyChanged();
            }
        }

        internal void AddText(string text)
        {
            Parts.Add(text);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void ClearText()
        {
            Text.Clear();
            Parts.Clear();
        }

        public override string ToString() => $"{ContextId} - {Name}";
    }

    public class NamedTimer : Timer
    {
        public int Context;
    }
}