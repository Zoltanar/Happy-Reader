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
        public ApiQuery(string actionName, bool refreshList, bool additionalMessage, bool ignoreDateLimit)
        {
            ActionName = actionName;
            RefreshList = refreshList;
            AdditionalMessage = additionalMessage;
            IgnoreDateLimit = ignoreDateLimit;
            Completed = false;
        }

        /// <summary>
        /// Count of titles added in last query.
        /// </summary>
        public uint TitlesAdded { get; private set; }
        /// <summary>
        /// Count of titles skipped in last query.
        /// </summary>
        public uint TitlesSkipped { get; private set; }
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

        public uint TotalTitles => TitlesAdded + TitlesSkipped;


        public void AddTitlesSkipped(uint count = 1) => TitlesSkipped += count;

        public void AddTitlesAdded(uint count = 1) => TitlesAdded += count;

        public void AddCharactersAdded(uint count = 1) => CharactersAdded += count;

        public void AddCharactersUpdated(uint count = 1) => CharactersUpdated += count;
    }

}
