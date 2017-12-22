using System;
using System.Linq;
using System.Reflection;
using Happy_Apps_Core;
using KakasiNET;
namespace Happy_Reader
{
    static class Kakasi
    {

        private static KakasiInner IsolatedKakasi;
        private static AppDomain KakasiAppDomain;
        static Kakasi()
        {
            ReloadAppDomain(true);
        }

        static void ReloadAppDomain(bool firstTime = false)
        {
            _counter = 0;
            if(!firstTime) AppDomain.Unload(KakasiAppDomain);
            // Get and display the full name of the EXE assembly.
            string exeAssembly = Assembly.GetEntryAssembly().FullName;

            // Construct and initialize settings for a second AppDomain.
            AppDomainSetup ads = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };

            // Create the second AppDomain.
            KakasiAppDomain = AppDomain.CreateDomain("AD #2", null, ads);

            // Create an instance of MarshalbyRefType in the second AppDomain. 
            // A proxy to the object is returned.
            try
            {
                IsolatedKakasi =
                    (KakasiInner) KakasiAppDomain.CreateInstanceAndUnwrap(exeAssembly, typeof(KakasiInner).FullName);
            }
            catch (Exception ex)
            {
                StaticHelpers.LogToFile(ex);
            }
        }

        private class KakasiInner : MarshalByRefObject
        {
            private static bool _initialized;

            public void Init()
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

            private void SetParams(params string[] parameters)
            {
                try
                {
                    //always need these parameters
                    KakasiLib.SetParams(new[] { "kakasi", "-ieuc" }.Concat(parameters).ToArray()); //kana with kanji+furi
                }
                catch (Exception ex)
                {/*LogToFile(ex);*/}
            }

            public string AddFuriToJapanese(string text, bool spaces = false)
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

            public string JapaneseToKana(string text, bool spaces = false)
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

            public string JapaneseToRomaji(string text, bool spaces = false)
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

            public void Deinit()
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

        public static void Init()
        {
            int tries = 0;
            while (tries < 5)
            {
                try
                {
                    tries++;
                    IsolatedKakasi.Init();
                    break;
                }
                catch (Exception ex)
                {
                    StaticHelpers.LogToFile(ex);
                    AppDomain.Unload(KakasiAppDomain);
                    ReloadAppDomain();
                }
            }

        }

        public static void Deinit()
        {
            int tries = 0;
            while (tries < 5)
            {
                try
                {
                    tries++;
                    IsolatedKakasi.Deinit();
                    break;
                }
                catch (Exception ex)
                {
                    StaticHelpers.LogToFile(ex);
                    AppDomain.Unload(KakasiAppDomain);
                    ReloadAppDomain();
                }
            }
        }

        private static int _counter;
        public static string JapaneseToRomaji(string text, bool spaces = false)
        {
            _counter++;
            if (_counter > 500) ReloadAppDomain();
            int tries = 0;
            while (tries < 5)
            {
                try
                {
                    tries++;
                    return IsolatedKakasi.JapaneseToRomaji(text,spaces);
                }
                catch (Exception ex)
                {
                    StaticHelpers.LogToFile(ex);
                    ReloadAppDomain();
                }
            }
            return null;
        }

        public static string JapaneseToKana(string text, bool spaces = false)
        {
            int tries = 0;
            while (tries < 5)
            {
                try
                {
                    tries++;
                    return IsolatedKakasi.JapaneseToKana(text, spaces);
                }
                catch (Exception ex)
                {
                    StaticHelpers.LogToFile(ex);
                    AppDomain.Unload(KakasiAppDomain);
                    ReloadAppDomain();
                }
            }
            return null;
        }
    }

}
