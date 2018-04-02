using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Happy_Apps_Core;
using JetBrains.Annotations;
using KakasiNET;
namespace Happy_Reader
{
	static class Kakasi
	{
		private static KakasiLib _kakasiJtk;
		private static KakasiLib _kakasiJtr;
		private static int _counter;

		private static readonly string KakasiAssembly;

		static Kakasi()
		{
			try
			{
				var path = Path.GetFullPath(@"Kakasi.NET.Interop.dll");
				var assembly = Assembly.LoadFile(path);
				KakasiAssembly = assembly.FullName;
				LoadKakasiJtk();
				LoadKakasiJtr();
			}
			catch (Exception ex)
			{
				StaticHelpers.LogToFile(ex);
			}
		}


		static void LoadKakasiJtk()
		{
			var kakasiAppDomainJtk = AppDomain.CreateDomain($"KakasiJapToKana AD1 ");
			_kakasiJtk = (KakasiLib)kakasiAppDomainJtk.CreateInstanceAndUnwrap(KakasiAssembly, typeof(KakasiLib).FullName);
			_kakasiJtk.Init();
			_kakasiJtk.SetParams(new string[] { "kakasi", "-ieuc", "-JH", "-s" });
		}

		static void LoadKakasiJtr()
		{
			var kakasiAppDomainJtr = AppDomain.CreateDomain($"KakasiJapToRomaji AD2 ");
			_kakasiJtr = (KakasiLib)kakasiAppDomainJtr.CreateInstanceAndUnwrap(KakasiAssembly, typeof(KakasiLib).FullName);
			_kakasiJtr.InitSpecific("libkakasi2.dll");
			_kakasiJtr.SetParams(new string[] { "kakasi", "-ieuc", "-Ha", "-Ja", "-Ka", "-s" });
		}

		public static string JapaneseToRomaji([NotNull]string text)
		{
			int tries = 0;
			while (tries < 5)
			{
				HandleCounter();
				try
				{
					tries++;
					return _kakasiJtr.DoKakasi(text);
				}
				catch (Exception ex)
				{
					StaticHelpers.LogToFile(ex);
					LoadKakasiJtr();
				}
			}
			return null;
		}

		public static string JapaneseToKana([NotNull]string text)
		{
			int tries = 0;
			while (tries < 5)
			{
				HandleCounter();
				try
				{
					tries++;
					return _kakasiJtk.DoKakasi(text);
				}
				catch (Exception ex)
				{
					StaticHelpers.LogToFile(ex);
					LoadKakasiJtk();
				}
			}
			return null;
		}

		[Conditional("LOGVERBOSE")]
		private static void HandleCounter()
		{
			_counter++;
			if (_counter % 50 == 0) Debug.WriteLine($"Kakasi counter at {_counter}");
		}


	}

}
