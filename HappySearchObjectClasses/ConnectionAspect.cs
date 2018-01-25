using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PostSharp.Aspects;
using PostSharp.Serialization;

namespace Happy_Apps_Core
{
    [PSerializable]
    public class ConnectionFunctionAspect : OnMethodBoundaryAspect
    {
        public sealed override void OnEntry(MethodExecutionArgs args)
        {
            Debug.WriteLine(args.Arguments.Any()
                ? $"PS: Entered {args.Method.Name}, arguments: {string.Join(", ", args.Arguments.Select(x => $"[{x}]"))}"
                : $"PS: Entered {args.Method.Name}");
        }

        public sealed override void OnExit(MethodExecutionArgs args)
        {
            Debug.WriteLine(args.Arguments.Any()
                ? $"PS: Exited {args.Method.Name}, arguments: {string.Join(", ", args.Arguments.Select(x => $"[{x}]"))}"
                : $"PS: Exited {args.Method.Name}");
            VndbConnection conn = (VndbConnection)args.Instance;
            conn.EndQuery();
        }

        public sealed override void OnException(MethodExecutionArgs args)
        {
            Debug.WriteLine(args.Arguments.Any()
                ? $"PS: Exception in {args.Method.Name}, arguments: {string.Join(", ", args.Arguments.Select(x => $"[{x}]"))}"
                : $"PS: Exception in {args.Method.Name}");
            VndbConnection conn = (VndbConnection)args.Instance;
            conn.TextAction($"Exception in {args.Method.Name} - {args.Exception.Message}", VndbConnection.MessageSeverity.Error);
        }
    }

    [PSerializable]
    public class ConnectionInterceptAspect : MethodInterceptionAspect
    {
        public bool RefreshList { get; set; }
        public bool AdditionalMessage { get; set; }
        public bool IgnoreDateLimit { get; set; }

        public ConnectionInterceptAspect(bool refreshList, bool additionalMessage, bool ignoreDateLimit)
        {
            RefreshList = refreshList;
            AdditionalMessage = additionalMessage;
            IgnoreDateLimit = ignoreDateLimit;
        }

        public sealed override async Task OnInvokeAsync(MethodInterceptionArgs args)
        {
            VndbConnection conn = (VndbConnection)args.Instance;
            if (StaticHelpers.CSettings.UserID < 1) return;
            if (!conn.StartQuery(args.Method.Name, RefreshList, AdditionalMessage, IgnoreDateLimit)) return;
            await Task.Run(() => args.ProceedAsync());
        }
    }
}
