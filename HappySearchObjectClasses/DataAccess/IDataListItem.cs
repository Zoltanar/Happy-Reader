namespace Happy_Apps_Core.DataAccess
{
	public interface IDataListItem<out TListKey>
	{
		TListKey ListKey { get; }
	}
}