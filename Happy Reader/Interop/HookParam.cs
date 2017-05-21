using System;
using System.Runtime.InteropServices;
// ReSharper disable All

namespace Happy_Reader.Interop
{
    //copied as is from chiitrans.texthook folder (https://github.com/alexbft/chiitrans)

    [Flags()]
    public enum HookParamType : uint
    {
        HOOK_NULL_TYPE = 0
        , USING_STRING = 0x1
        , USING_UNICODE = 0x2
        , BIG_ENDIAN = 0x4
        , DATA_INDIRECT = 0x8
        , USING_SPLIT = 0x10
        , SPLIT_INDIRECT = 0x20
        , MODULE_OFFSET = 0x40
        , FUNCTION_OFFSET = 0x80
        , PRINT_DWORD = 0x100
        , STRING_LAST_CHAR = 0x200
        , NO_CONTEXT = 0x400
        , EXTERN_HOOK = 0x800
        , HOOK_AUXILIARY = 0x2000
        , HOOK_ENGINE = 0x4000
        , HOOK_ADDITIONAL = 0x8000
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct HookParam
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void extern_fun_t(Int32 p0, HookParam p1, IntPtr p2, IntPtr p3, IntPtr p4);

        public int addr;
        public int off, ind, split, split_ind;
        public int module, function;
        public extern_fun_t extern_fun;
        public HookParamType type;
        public short length_offset;
        public byte hook_len, recover_len;
    }
}
