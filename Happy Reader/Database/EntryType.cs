namespace Happy_Reader.Database
{
	public enum EntryType
	{
		// ReSharper disable All
		Proxy = -40,
		//stage zero
		Game = 0,
		//stage 1
		Input = 10,
		PreRomaji = 12,
		PostRomaji = 15,
		Yomi = 20,
		//stage 2
		Translation = 30,
		Name = 40,
		ProxyMod = 41,
		//stage 3
		Output = 50,
		// ReSharper restore All
	}
}