using System;

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
		/// Query has been completed
		/// </summary>
		public bool Completed;

		/// <summary>
		/// Set API query settings.
		/// </summary>
		/// <param name="actionName">Name of action</param>
		public ApiQuery(string actionName)
		{
			ActionName = actionName;
			Completed = false;
		}
		
		public string CompletedMessage { get; set; } = "";

		public VndbConnection.MessageSeverity CompletedMessageSeverity { get; set; } = VndbConnection.MessageSeverity.Normal;

		public void SetException(Exception exception)
		{
			if (exception == null) throw new Exception("Setting Exception to null is not expected.");
			StaticHelpers.Logger.ToFile(exception,ActionName);
			CompletedMessage = $"Exception in {ActionName} - {exception.Message}";
			CompletedMessageSeverity = VndbConnection.MessageSeverity.Error;
		}
	}

}
