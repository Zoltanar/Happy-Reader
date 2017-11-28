﻿using System;
using System.Runtime.InteropServices;
// ReSharper disable All


namespace Happy_Reader.Interop
{

    //copied as is from chiitrans.texthook folder (https://github.com/alexbft/chiitrans)

    class TextHookInteropCompat
    {
        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookInit();

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookConnect(Int32 pid);

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookDisconnect();

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookCleanup();

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookOnConnect(TextHookInterop.CallbackFunc callback);

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookOnDisconnect(TextHookInterop.CallbackFunc callback);

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookOnCreateThread(TextHookInterop.OnCreateThreadFunc callback);

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookOnRemoveThread(TextHookInterop.OnRemoveThreadFunc callback);

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookOnInput(TextHookInterop.OnInputFunc callback);

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookAddHook(ref HookParam p, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport("ithwrapper_compat.dll")]
        public static extern Int32 TextHookRemoveHook(Int32 addr);
    }
}
