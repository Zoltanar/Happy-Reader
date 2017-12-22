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
        /// Refresh OLV on throttled connection
        /// </summary>
        public readonly bool RefreshList;

        /// <summary>
        /// Print Added/Skipped message on throttled connection
        /// </summary>
        public readonly bool AdditionalMessage;

        /// <summary>
        /// Ignore 10 year limit (if applicable)
        /// </summary>
        public readonly bool IgnoreDateLimit;

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
        /// <param name="ignoreDateLimit">Ignore 10 year limit (if applicable)</param>
        public ApiQuery(string actionName,bool refreshList, bool additionalMessage, bool ignoreDateLimit)
        {
            ActionName = actionName;
            RefreshList = refreshList;
            AdditionalMessage = additionalMessage;
            IgnoreDateLimit = ignoreDateLimit;
            Completed = false;
        }

        /// <summary>
        /// Constructor for initializing ActiveQuery.
        /// </summary>
        /// <param name="isStartup">Necessary parameter</param>
        public ApiQuery(bool isStartup)
        {
            if (!isStartup) throw new ArgumentException("This method should only be used for startup.");
            ActionName = "Startup";
            RefreshList = false;
            AdditionalMessage = false;
            IgnoreDateLimit = false;
            Completed = true;
        }
    }

}
