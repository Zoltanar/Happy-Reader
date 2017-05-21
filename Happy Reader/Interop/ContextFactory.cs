// ReSharper disable All
namespace Happy_Reader.Interop
{
    interface ContextFactory {
        TextHookContext create(int id, string name, int hook, int context, int subcontext, int status);
        void onConnected();
    }
}
