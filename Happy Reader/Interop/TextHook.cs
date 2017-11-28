﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Happy_Reader.StaticMethods;

//ReSharper disableAll
#pragma warning disable IDE1006 // Naming Styles
namespace Happy_Reader.Interop
{
    class TextHook
    {
        public const int OK = 0;
        public const int ERROR_ALREADY_RUNNING = 1001;

        private bool isInitialized = false;
        internal bool isCompat = false;

        public int init(bool isCompat)
        {
            lock (this)
            {
                if (isInitialized)
                {
                    if (this.isCompat == isCompat)
                    {
                        return TextHook.OK;
                    }
                    else
                    {
                        throw new Exception("Initialization error. Please restart Chiitrans Lite and try again.");
                    }
                }
                else
                {
                    this.isCompat = isCompat;
                    try
                    {
                        handleCreateThreadDelegate = new TextHookInterop.OnCreateThreadFunc(handleCreateThread);
                        handleRemoveThreadDelegate = new TextHookInterop.OnRemoveThreadFunc(handleRemoveThread);
                        handleConnectDelegate = new TextHookInterop.CallbackFunc(handleConnect);
                        handleDisconnectDelegate = new TextHookInterop.CallbackFunc(handleDisconnect);
                        handleInputDelegate = new TextHookInterop.OnInputFunc(handleInput);
                        int res;
                        if (isCompat)
                        {
                            TextHookInteropCompat.TextHookOnConnect(handleConnectDelegate);
                            TextHookInteropCompat.TextHookOnCreateThread(handleCreateThreadDelegate);
                            TextHookInteropCompat.TextHookOnRemoveThread(handleRemoveThreadDelegate);
                            TextHookInteropCompat.TextHookOnDisconnect(handleDisconnectDelegate);
                            TextHookInteropCompat.TextHookOnInput(handleInputDelegate);
                            res = TextHookInteropCompat.TextHookInit();
                            isInitialized = res == TextHook.OK;
                            return res;
                        }
                        else
                        {
                            TextHookInterop.TextHookOnConnect(handleConnectDelegate);
                            TextHookInterop.TextHookOnCreateThread(handleCreateThreadDelegate);
                            TextHookInterop.TextHookOnRemoveThread(handleRemoveThreadDelegate);
                            TextHookInterop.TextHookOnDisconnect(handleDisconnectDelegate);
                            TextHookInterop.TextHookOnInput(handleInputDelegate);
                            res = TextHookInterop.TextHookInit();
                            isInitialized = res == TextHook.OK;
                            return res;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        return 1;
                    }
                }
            }
        }
        private class Singleton
        {
            public static readonly TextHook _instance = new TextHook();
        }

        private TextHookInterop.OnCreateThreadFunc handleCreateThreadDelegate;
        private TextHookInterop.OnRemoveThreadFunc handleRemoveThreadDelegate;
        private TextHookInterop.CallbackFunc handleConnectDelegate;
        private TextHookInterop.CallbackFunc handleDisconnectDelegate;
        private TextHookInterop.OnInputFunc handleInputDelegate;

        private TextHook()
        {
        }

        public static TextHook instance
        {
            get
            {
                return Singleton._instance;
            }
        }

        private ContextFactory factory = new DefaultContextFactory();
        private IDictionary<int, TextHookContext> contexts = new ConcurrentDictionary<int, TextHookContext>();
        public String currentProcessTitle { get; private set; }
        public volatile bool isConnected = false;
        public Process currentProcess { get; private set; }

        private int handleCreateThread(int id, string name, int hook, int context, int subcontext, int status)
        {
            contexts[id] = factory.create(id, name, hook, context, subcontext, status);
            Task.Factory.StartNew(() => {
                if (onNewContext != null)
                {
                    onNewContext(contexts[id]);
                }
            });
            return 0;
        }

        private int handleRemoveThread(int id)
        {
            TextHookContext ctx;
            if (contexts.TryGetValue(id, out ctx))
            {
                Task.Factory.StartNew(() => {
                    if (onRemoveContext != null)
                    {
                        onRemoveContext(ctx);
                    }
                    contexts.Remove(id);
                });
            }
            return 0;
        }

        private int handleConnect()
        {
            installHooks();
            return 0;
        }

        private int handleDisconnect()
        {
            isConnected = false;
            contexts.Clear();
            Task.Factory.StartNew(() => {
                if (onDisconnect != null)
                {
                    onDisconnect();
                }
            });
            ResetSession();
            return 0;
        }

        private int handleInput(int id, IntPtr data, int len, int isNewline)
        {
            TextHookContext ctx;
            if (contexts.TryGetValue(id, out ctx))
            {
                ctx.handleInput(data, len, (isNewline & 2) != 0);
            }
            return 0;
        }

        public bool connect(int pid)
        {
            if (!isInitialized)
            {
                return false;
            }
            else
            {
                if (isCompat)
                {
                    TextHookInteropCompat.TextHookConnect(pid); // cannot determine success :(
                }
                else
                {
                    TextHookInterop.TextHookConnect(pid);
                }
                contexts.Clear();
                isConnected = true;
                return true;
            }
        }

        public void connect(int pid, string exeName)
        {
            /*bool needCompat = Program.arguments.Contains("--compat");
            //(Path.GetFileName(exeName).ToLower() == "fatefd.exe" && !Program.arguments.Contains("--nocompat"));
            if (needCompat && !isInitialized)
            {
                if (!Utils.confirm("Entering ITH compatible mode. Continue?"))
                {
                    throw new MyException("Aborted.");
                }
            }*/
            int err = init(false);
            if (err != TextHook.OK)
            {
                if (err == TextHook.ERROR_ALREADY_RUNNING)
                {
                    throw new Exception("Cannot initialize TextHook: already running!");
                }
                else
                {
                    throw new Exception("Failed to initialize TextHook, error code = " + err);
                }
            }
            else
            {
                if (pid == 0)
                {
                    pid = StaticMethods.StartProcess(exeName).Id;
                    if (pid == 0)
                    {
                        throw new Exception("Failed to start process.");
                    }
                }
                try
                {
                    currentProcess = Process.GetProcessById(pid);
                }
                catch
                {
                    throw new Exception("Failed to connect to process.");
                }
                if (currentProcess.HasExited && !string.IsNullOrEmpty(exeName))
                {
                    connect(0, exeName);
                    return;
                }
                currentProcess.WaitForInputIdle(1000);
                currentProcessTitle = currentProcess.MainWindowTitle;
                Session.RunningExecutable = exeName;
                factory.onConnected();
                connect(pid);
            }
        }

        public void setContextFactory(ContextFactory f)
        {
            this.factory = f;
        }

        public ContextFactory getContextFactory()
        {
            return factory;
        }

        public TextHookContext getContext(int ctxId)
        {
            TextHookContext res;
            if (contexts.TryGetValue(ctxId, out res))
            {
                return res;
            }
            else
            {
                return null;
            }
        }

        public delegate void TextHookContextHandler(TextHookContext ctx);
        public event TextHookContextHandler onNewContext;
        public event TextHookContextHandler onRemoveContext;
        public event Action onDisconnect;

        internal IEnumerable<TextHookContext> getContexts()
        {
            return contexts.Values;
        }

        internal bool addUserHook(UserHook userHook)
        {
            if (!isConnected)
            {
                return false;
            }
            if (Session.HookIsInstalled(userHook))
            {
                return false;
            }
            bool ok;
            if (isCompat)
            {
                ok = TextHookInteropCompat.TextHookAddHook(ref userHook.hookParam, userHook.getName()) == OK;
            }
            else
            {
                ok = TextHookInterop.TextHookAddHook(ref userHook.hookParam, userHook.getName()) == OK;
            }
            if (ok)
            {
                Session.AddUserHook(userHook);
            }
            return ok;
        }

        internal bool removeUserHook(UserHook hook)
        {
            if (!isConnected)
            {
                return false;
            }
            if (!Session.RemoveUserHook(hook))
            {
                return false;
            }
            bool ok = true;
            Thread removingThread = new Thread(new ThreadStart(() => {
                if (isCompat)
                {
                    ok = TextHookInteropCompat.TextHookRemoveHook(hook.addr) == OK;
                }
                else
                {
                    ok = TextHookInterop.TextHookRemoveHook(hook.addr) == OK;
                }
            }));
            removingThread.IsBackground = true;
            removingThread.Start();
            if (removingThread.Join(1000))
            {
                return ok;
            }
            else
            {
                removingThread.Abort();
                Debug.WriteLine("Freeze trying to remove hook: " + hook.code);
                return true;
            }
        }

        private void installHooks()
        {
            foreach (UserHook h in Session.GetHookList())
            {
                if (isCompat)
                {
                    TextHookInteropCompat.TextHookAddHook(ref h.hookParam, h.getName());
                }
                else
                {
                    TextHookInterop.TextHookAddHook(ref h.hookParam, h.getName());
                }
            }
        }

    }
}
