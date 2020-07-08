namespace Happy_Apps_Core.Database
{
	public interface IDumpItem
	{
		void LoadFromStringParts(string[] parts);

		void SetDumpHeaders(string[] parts);

		string GetPart(string[] parts, string columnName);
	}
}