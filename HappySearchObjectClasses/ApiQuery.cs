using System;
using System.Collections.Generic;

namespace Happy_Apps_Core
{
	/// <summary>
	/// Contains settings for API Query
	/// </summary>
	public class ApiQuery
	{
		private readonly Action<List<int>> _actionOnAdd;

		/// <summary>
		/// Name of action
		/// </summary>
		public readonly string ActionName;

		/// <summary>
		/// Refresh OLV on throttled connection
		/// </summary>
		public readonly bool RefreshList;

		/// <summary>
		/// Print Added/Skipped message on throttled connection
		/// </summary>
		public readonly bool AdditionalMessage;
		
		/// <summary>
		/// Query has been completed
		/// </summary>
		public bool Completed;

		/// <summary>
		/// Set API query settings.
		/// </summary>
		/// <param name="actionName">Name of action</param>
		/// <param name="refreshList">Refresh OLV on throttled connection</param>
		/// <param name="additionalMessage">Print Added/Skipped message on throttled connection</param>
		/// <param name="actionOnAdd">Action to perform when item is added to <see cref="TitlesAdded"/></param>
		public ApiQuery(string actionName, bool refreshList, bool additionalMessage, Action<List<int>> actionOnAdd)
		{
			ActionName = actionName;
			RefreshList = refreshList;
			AdditionalMessage = additionalMessage;
			_actionOnAdd = actionOnAdd;
			Completed = false;
		}

		/// <summary>
		/// List of title ids added in last query.
		/// </summary>
		public List<int> TitlesAdded { get; } = new List<int>();

		/// <summary>
		/// List of title ids skipped in last query.
		/// </summary>
		public List<int> TitlesSkipped { get; } = new List<int>();
		/// <summary>
		/// Count of characters added in last query.
		/// </summary>
		public uint CharactersAdded { get; private set; }
		/// <summary>
		/// Count of characters updated in last query.
		/// </summary>
		public uint CharactersUpdated { get; private set; }

		public string CompletedMessage { get; set; } = "";

		public VndbConnection.MessageSeverity CompletedMessageSeverity { get; set; } = VndbConnection.MessageSeverity.Normal;

		public int TotalTitles => TitlesAdded.Count + TitlesSkipped.Count;

		public void SetException(Exception exception)
		{
			if (exception == null) throw new Exception("Setting Exception to null is not expected.");
			CompletedMessage = $"Exception in {ActionName} - {exception.Message}";
			CompletedMessageSeverity = VndbConnection.MessageSeverity.Error;
		}

		public void AddTitleSkipped(int id) => TitlesSkipped.Add(id);

		public void AddTitlesSkipped(IEnumerable<int> ids)
		{
			foreach (var id in ids)
			{
				TitlesSkipped.Add(id);
			}
		}

		public void AddTitleAdded(int id)
		{
			TitlesAdded.Add(id);
		}

		public void AddCharactersAdded(uint count = 1) => CharactersAdded += count;

		public void AddCharactersUpdated(uint count = 1) => CharactersUpdated += count;

		private int _itemCountDuringLastActionOnAdd;

		public void RunActionOnAdd()
		{
			if (_actionOnAdd == null || TitlesAdded.Count == _itemCountDuringLastActionOnAdd) return;
			if(RefreshList) _actionOnAdd.Invoke(TitlesAdded);
			_itemCountDuringLastActionOnAdd = TitlesAdded.Count;
		}

		public string GetAdditionalWarning()
		{
			if (!AdditionalMessage) return string.Empty;
			string additionalWarning = "";
			if (TitlesAdded.Count > 0) additionalWarning += $" Added {TitlesAdded.Count}.";
			if (TitlesSkipped.Count > 0) additionalWarning += $" Skipped {TitlesSkipped.Count}.";
			return additionalWarning;
		}
	}

}
