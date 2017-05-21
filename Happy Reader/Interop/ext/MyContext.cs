using System.Collections.Concurrent;
using System.Collections.Generic;

// ReSharper disable All
namespace Happy_Reader.Interop
{
    class MyContext : TextHookContext {
        const int MAX_LOG = 20;

#pragma warning disable IDE1006 // Naming Styles
        private bool _enabled;
        public bool enabled {
            get {
                return _enabled;
            }
            set {
                _enabled = value;
                //Session.SetContextEnabled(this.context, this.subcontext, value);
            }
        }

        public ConcurrentQueue<string> log = new ConcurrentQueue<string>();

        public MyContext(int id, string name, int hook, int context, int subcontext, int status, bool enabled):
        base(id, name, hook, context, subcontext, status) {
            this.enabled = enabled;
            this.onSentence += MyContext_onSentence;
        }

        void MyContext_onSentence(TextHookContext sender, string text) {
            log.Enqueue(text);
            if (log.Count > MAX_LOG) {
                log.TryDequeue(out string unused);
            }
        }

        public IEnumerable<string> getLog()
        {
#pragma warning restore IDE1006 // Naming Styles
            return log;
        }
    }
}
