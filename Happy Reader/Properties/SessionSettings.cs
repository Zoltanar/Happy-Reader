using System;
using System.Collections.Generic;
using System.Linq;
using Happy_Reader.Interop;
// ReSharper disable All
#pragma warning disable IDE1006 // Naming Styles

namespace Happy_Reader.Properties
{
    public class SessionSettings
    {
        public string RunningExecutable;
        private SynchronizedCollection<UserHook> _userHooks = new SynchronizedCollection<UserHook>();
        private bool isDirty;
        internal MyContextFactory.NewContextsBehavior newContextsBehavior;

        //
        private static object classLock = new object();
        private static Dictionary<string, SessionSettings> cache = new Dictionary<string, SessionSettings>();
        public readonly string processExe;
        private string _po;
        public string po
        {
            get
            {
                return _po;
            }
            set
            {
                if (_po != value)
                {
                    _po = value;
                    isDirty = true;
                }
            }
        }
        private TimeSpan _sentenceDelay;
        public TimeSpan sentenceDelay
        {
            get
            {
                return _sentenceDelay;
            }
            set
            {
                if (_sentenceDelay != value)
                {
                    _sentenceDelay = value;
                    isDirty = true;
                }
            }
        }
        //

        internal bool HookIsInstalled(UserHook userHook) => _userHooks.Any(h => h.addr == userHook.addr);

        internal void AddUserHook(UserHook userHook)
        {
            _userHooks.Add(userHook);
            isDirty = true;
        }

        public bool RemoveUserHook(UserHook hook)
        {
            bool ok = _userHooks.Remove(hook);
            if (ok)
            {
                isDirty = true;
            }
            return isDirty;
        }

        public IEnumerable<UserHook> GetHookList() => _userHooks;
        
        private long contextKey(int addr, int sub)
        {
            return (((long)sub) << 32) + addr;
        }
#pragma warning restore IDE1006 // Naming Styles


    }
}
