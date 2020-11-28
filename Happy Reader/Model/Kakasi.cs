using System;
using System.IO;
using System.Reflection;
using System.Text;
using Happy_Apps_Core;
using JetBrains.Annotations;
using KakasiNET;
namespace Happy_Reader
{
	static class Kakasi
	{
		private static KakasiLib _kakasiJtk;
		private static KakasiLib _kakasiJtr;
		private static AppDomain _kakasiAppDomainJtr;
		private static AppDomain _kakasiAppDomainJtk;

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
				StaticHelpers.Logger.ToFile(ex);
			}
		}


		static void LoadKakasiJtk()
		{
			if (_kakasiAppDomainJtk != null) AppDomain.Unload(_kakasiAppDomainJtk);
			_kakasiAppDomainJtk = AppDomain.CreateDomain($"KakasiJapToKana AD1 ");
			_kakasiJtk = (KakasiLib)_kakasiAppDomainJtk.CreateInstanceAndUnwrap(KakasiAssembly, typeof(KakasiLib).FullName);
			_kakasiJtk.Init();
			_kakasiJtk.SetParams(new string[] { "kakasi", "-ieuc", "-JH", "-s" });
		}

		static void LoadKakasiJtr()
		{
			if(_kakasiAppDomainJtr != null) AppDomain.Unload(_kakasiAppDomainJtr);
			_kakasiAppDomainJtr = AppDomain.CreateDomain($"KakasiJapToRomaji AD2 ");
			_kakasiJtr = (KakasiLib)_kakasiAppDomainJtr.CreateInstanceAndUnwrap(KakasiAssembly, typeof(KakasiLib).FullName);
			_kakasiJtr.InitSpecific("libkakasi2.dll");
			_kakasiJtr.SetParams(new string[] { "kakasi", "-ieuc", "-Ha", "-Ja", "-Ka", "-s" });
		}

		public static string JapaneseToRomaji([NotNull]string text)
		{
			int tries = 0;
			while (tries < 5)
			{
				try
				{
					tries++;
					return _kakasiJtr.DoKakasi(text);
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile(ex);
					LoadKakasiJtr();
				}
			}
			return null;
		}

		public static void JapaneseToRomaji([NotNull] StringBuilder sb)
		{
			var result = JapaneseToRomaji(sb.ToString());
			sb.Clear();
			if(result != null) sb.Append(result);
		}

		public static string JapaneseToKana([NotNull]string text)
		{
			int tries = 0;
			while (tries < 5)
			{
				try
				{
					tries++;
					return _kakasiJtk.DoKakasi(text);
				}
				catch (Exception ex)
				{
					StaticHelpers.Logger.ToFile(ex);
					LoadKakasiJtk();
				}
			}
			return null;
		}

		public static void JapaneseToKana([NotNull] StringBuilder sb)
		{
			var result = JapaneseToKana(sb.ToString());
			sb.Clear();
			if (result != null) sb.Append(result);
		}
	}

}
