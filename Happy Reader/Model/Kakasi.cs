using System;
using System.IO;
using System.Reflection;
using Happy_Apps_Core;
using JetBrains.Annotations;
using KakasiNET;
namespace Happy_Reader
{
	static class Kakasi
	{
		private static KakasiLib _kakasiJtr;
		private static AppDomain _kakasiAppDomainJtr;

		private static readonly string KakasiAssembly;

		static Kakasi()
		{
			try
			{
				var path = Path.GetFullPath(@"Kakasi.NET.Interop.dll");
				var assembly = Assembly.LoadFile(path);
				KakasiAssembly = assembly.FullName;
				LoadKakasiJtr();
			}
			catch (Exception ex)
			{
				StaticHelpers.Logger.ToFile(ex);
			}
		}
		
		static void LoadKakasiJtr()
		{
			if(_kakasiAppDomainJtr != null) AppDomain.Unload(_kakasiAppDomainJtr);
			_kakasiAppDomainJtr = AppDomain.CreateDomain($"KakasiJapToRomaji");
			_kakasiJtr = (KakasiLib)_kakasiAppDomainJtr.CreateInstanceAndUnwrap(KakasiAssembly, typeof(KakasiLib).FullName);
			_kakasiJtr.Init();
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
	}

}
