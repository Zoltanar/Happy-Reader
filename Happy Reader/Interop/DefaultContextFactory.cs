﻿namespace Happy_Reader.Interop {
    class DefaultContextFactory : ContextFactory {

        public TextHookContext create(int id, string name, int hook, int context, int subcontext, int status) {
            return new TextHookContext(id, name, hook, context, subcontext, status);
        }

        public void onConnected() {
        }
    }
}
