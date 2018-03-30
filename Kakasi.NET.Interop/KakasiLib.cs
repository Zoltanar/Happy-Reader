using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace KakasiNET
{
    /// <summary>
    /// Kakasi library wrapper, taken as is from https://github.com/linguanostra/Kakasi.NET/tree/cca815915c7f616252c40eac7d16e2e247d4b429
    /// </summary>
    public class KakasiLib : MarshalByRefObject
    {

		public override object InitializeLifetimeService() => null;

        #region Externs

        /// <summary>
        /// Set DLL search directory
        /// </summary>
        /// <param name="lpPathName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// Get procedure address
        /// </summary>
        /// <param name="hModule"></param>
        /// <param name="procName"></param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        /// <summary>
        /// Load library
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// Free library
        /// </summary>
        /// <param name="hModule"></param>
        /// <returns></returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeLibrary(IntPtr hModule);

        #endregion

        #region Delegates

        /// <summary>
        /// Kakasi get options arguments delegate
        /// </summary>
        /// <param name="size"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int KakasiGetoptArgv([In] int size, [In] string[] param);

        /// <summary>
        /// Kakasi main execution method delegate
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr KakasiDo(byte[] str);

        /// <summary>
        /// Kakasi get options arguments
        /// </summary>
        private static KakasiGetoptArgv _kakasiGetoptArgv;

        /// <summary>
        /// Kakasi main execution method
        /// </summary>
        private static KakasiDo _kakasiDo;

        #endregion

        #region Properties

        /// <summary>
        /// Kakasi library instance pointer
        /// </summary>
        private IntPtr KakasiLibPtr = IntPtr.Zero;

        #endregion

        #region Static methods

        /// <summary>
        /// Init Kakasi library
        /// </summary>
        public void Init()
        {
            // Get executing assembly location
            var executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            var executingAssemblyPath = Path.GetDirectoryName(executingAssemblyLocation);

            // Init using this path
            Init(executingAssemblyPath);

        }

        /// <summary>
        /// Init Kakasi library
        /// </summary>
        public void InitSpecific(string kakasiDll)
        {

            // Get executing assembly location
            var executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            var executingAssemblyPath = Path.GetDirectoryName(executingAssemblyLocation);

            // Init using this path
            Init(executingAssemblyPath, kakasiDll);

        }

        /// <summary>
        /// Init Kakasi library
        /// </summary>
        /// <param name="executionPath">Execution path (to search for x86/x64 DLL)</param>
        public void Init(string executionPath, string kakasiDll = "libkakasi.dll")
        {

            // Lib path
            var kakasiLibPath = Path.Combine(executionPath,
                Environment.Is64BitProcess ? @"x64\" : @"x86\");

            // Set search path
            SetDllDirectory(kakasiLibPath);
            var path = Path.Combine(kakasiLibPath, kakasiDll);
            // Load library
            KakasiLibPtr = LoadLibrary(path);

            // Check for errors
            if (KakasiLibPtr != IntPtr.Zero)
            {

                // Loaded correctly
                _kakasiGetoptArgv =
                    (KakasiGetoptArgv)
                    Marshal.GetDelegateForFunctionPointer(GetProcAddress(KakasiLibPtr, "kakasi_getopt_argv"),
                        typeof(KakasiGetoptArgv));

                _kakasiDo =
                    (KakasiDo)
                    Marshal.GetDelegateForFunctionPointer(GetProcAddress(KakasiLibPtr, "kakasi_do"), typeof(KakasiDo));

            }
            else
            {

                // Get last Win32 error                
                var win32Error = Marshal.GetLastWin32Error();

                // Check if defined
                if (win32Error != 0)
                {

                    // Throw it
                    throw new Win32Exception(win32Error);

                }

                // Unknown error
                throw new Exception("Unable to load Kakasi library");

            }

        }

        /// <summary>
        /// Dispose of the library instance
        /// </summary>
        public void Dispose()
        {
            if (KakasiLibPtr != IntPtr.Zero)
            {
                FreeLibrary(KakasiLibPtr);
            }
        }

        /// <summary>
        /// Set Kakasi library params
        /// </summary>
        /// <param name="params"></param>
        public void SetParams(string[] @params)
        {

            // Init, if required
            if (KakasiLibPtr == IntPtr.Zero) Init();

            // Invoke
            _kakasiGetoptArgv.Invoke(@params.Length, @params);

        }
        
        /// <summary>
        /// Execute Kakasi action
        /// </summary>
        /// <param name="japanese"></param>
        /// <returns></returns>
        public string DoKakasi(string japanese)
        {
            // Init, if required
            if (KakasiLibPtr == IntPtr.Zero) Init();

            // Get EUC-JP encoding
            var encoding = Encoding.GetEncoding("euc-jp");

            // Get bytes
            var japaneseBytes = encoding.GetBytes(japanese);
            var callBytes = new byte[japaneseBytes.Length + 1];
            Buffer.BlockCopy(japaneseBytes, 0, callBytes, 0, japaneseBytes.Length);

            // Invoke to get result pointer
            var resultPtr = _kakasiDo.Invoke(callBytes);

            // Extract result bytes
            var resultBytes = new List<byte>();
            var currentByteIndex = 0;
            for (;;)
            {
                var currentByte = Marshal.ReadByte(resultPtr, currentByteIndex);
                if (currentByte != 0)
                {
                    resultBytes.Add(currentByte);
                    currentByteIndex++;
                }
                else
                {
                    break;
                }
            }

            // Get result string
            var decodedResult = encoding.GetString(resultBytes.ToArray());

            // Return it
            return decodedResult;

        }

        #endregion

    }
}