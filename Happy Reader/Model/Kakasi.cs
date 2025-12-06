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

		static Kakasi()
        {
            _kakasiJtr = new KakasiLib();
            _kakasiJtr.Init();
            _kakasiJtr.SetParams(["kakasi", "-ieuc", "-Ha", "-Ja", "-Ka", "-s"]);
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
					throw;
				}
			}
			return null;
		}
	}

}
