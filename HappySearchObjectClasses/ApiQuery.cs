using System;
using System.Collections.Generic;

namespace Happy_Apps_Core
{
	/// <summary>
	/// Contains settings for API Query
	/// </summary>
	public class ApiQuery
	{
		/// <summary>
		/// Name of action
		/// </summary>
		public readonly string ActionName;

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
		/// <param name="additionalMessage">Print Added/Skipped message on throttled connection</param>
		public ApiQuery(string actionName, bool additionalMessage)
		{
			ActionName = actionName;
			AdditionalMessage = additionalMessage;
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

		public string CompletedMessage { get; set; } = "";

		public VndbConnection.MessageSeverity CompletedMessageSeverity { get; set; } = VndbConnection.MessageSeverity.Normal;

		public void SetException(Exception exception)
		{
			if (exception == null) throw new Exception("Setting Exception to null is not expected.");
			StaticHelpers.Logger.ToFile(exception,ActionName);
			CompletedMessage = $"Exception in {ActionName} - {exception.Message}";
			CompletedMessageSeverity = VndbConnection.MessageSeverity.Error;
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
