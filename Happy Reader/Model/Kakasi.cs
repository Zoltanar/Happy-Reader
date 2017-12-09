using System;
using System.Diagnostics;
using System.Linq;
using Kakasi.NET.Interop;

namespace Happy_Reader
{
    static class Kakasi
    {
        private static bool _initialized;
        static Kakasi()
        {
            KakasiLib.Init();
            _initialized = true;
        }

        public static void Init()
        {
            if (_initialized) return;
            KakasiLib.Init();
            _initialized = true;
        }

        private static void SetParams(params string[] parameters)
        {
            //always need these parameters
            KakasiLib.SetParams(new[] { "kakasi", "-ieuc" }.Concat(parameters).ToArray()); //kana with kanji+furi
        }

        public static string AddFuriToJapanese(string text, bool spaces = false)
        {
            if (spaces) SetParams("-f", "-JH", "-w");
            else SetParams("-f", "-JH");
            return KakasiLib.DoKakasi(text);
        }
        
        public static string JapaneseToKana(string text, bool spaces = false)
        {
            // Set params to get Furigana
            // NOTE: Use EUC-JP encoding as the wrapper will encode/decode using it
            if (spaces) SetParams("-JH", "-w");
            else SetParams("-JH");
            return KakasiLib.DoKakasi(text);
        }

        public static string JapaneseToRomaji(string text, bool spaces = false)
        {
            // Set params to get Furigana
            // NOTE: Use EUC-JP encoding as the wrapper will encode/decode using it
            if (spaces) SetParams("-Ha", "-Ja", "-s");
            else SetParams("-Ha", "-Ja");
            return KakasiLib.DoKakasi(text);
        }

        public static void Deinit()
        {
            if (!_initialized) return;
            KakasiLib.Dispose();
            _initialized = false;
        }
    }
}
