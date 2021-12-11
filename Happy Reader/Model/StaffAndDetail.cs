using Happy_Apps_Core.Database;

namespace Happy_Reader
{
	public readonly struct StaffAndDetail
	{
		public StaffAlias Staff { get; }
		public string Detail { get; }

		public StaffAndDetail(StaffAlias staff, string detail)
		{
			Staff = staff;
			Detail = detail;
		}
	}
}