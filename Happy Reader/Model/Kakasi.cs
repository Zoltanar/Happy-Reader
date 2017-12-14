using System;
using System.Linq;
using KakasiNET;
namespace Happy_Reader
{
    static class Kakasi
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            try
            {
                KakasiLib.Init();
                _initialized = true;
            }
            catch (Exception ex)
            {/*LogToFile(ex);*/}
        }

        private static void SetParams(params string[] parameters)
        {
            try
            {
                //always need these parameters
                KakasiLib.SetParams(new[] { "kakasi", "-ieuc" }.Concat(parameters).ToArray()); //kana with kanji+furi
            }
            catch (Exception ex)
            {/*LogToFile(ex);*/}
        }

        public static string AddFuriToJapanese(string text, bool spaces = false)
        {
            if (spaces) SetParams("-f", "-JH", "-w");
            else SetParams("-f", "-JH"); try
            {
                return KakasiLib.DoKakasi(text);
            }
            catch (Exception ex)
            {/*LogToFile(ex);*/}
            return null;
        }

        public static string JapaneseToKana(string text, bool spaces = false)
        {
            // Set params to get Furigana
            // NOTE: Use EUC-JP encoding as the wrapper will encode/decode using it
            if (spaces) SetParams("-JH", "-w");
            else SetParams("-JH");
            try
            {
                return KakasiLib.DoKakasi(text);
            }
            catch (Exception ex)
            {/*LogToFile(ex);*/}
            return null;
        }

        public static string JapaneseToRomaji(string text, bool spaces = false)
        {
            // Set params to get Furigana
            // NOTE: Use EUC-JP encoding as the wrapper will encode/decode using it
            if (spaces) SetParams("-Ha", "-Ja", "-Ka", "-s");
            else SetParams("-Ha", "-Ja"); try
            {
                return KakasiLib.DoKakasi(text);
            }
            catch (Exception ex)
            {/*LogToFile(ex);*/}
            return null;
        }

        public static void Deinit()
        {
            if (!_initialized) return;
            try
            {
                KakasiLib.Dispose();
                _initialized = false;
            }
            catch (Exception ex)
            {/*LogToFile(ex);*/}
        }
    }
}
