using System;

namespace Happy_Apps_Core
{
    /// <summary>
    /// Object for displaying producer search results in OLV.
    /// </summary>
    public class ListedSearchedProducer
    {
	    public ListedSearchedProducer()
	    {
		    
	    }

        /// <summary>
        /// Constructor for Searched Producer.
        /// </summary>
        /// <param name="name">Name of producer</param>
        /// <param name="inList">Is producer already in favorite producers list? (Yes/No)</param>
        /// <param name="id">ID of producer</param>
        /// <param name="language">Language of producer</param>
        /// <param name="finishedTitles">Number of producer's titles finished by user</param>
        /// <param name="urtTitles">Number of producer's titles related to user</param>
        public ListedSearchedProducer(string name, string inList, int id, string language, int finishedTitles, int urtTitles)
        {
            Name = name;
            InList = inList;
            ID = id;
            Language = language;
            FinishedTitles = finishedTitles;
            URTTitles = urtTitles;

        }

        /// <summary>
        /// Convert ListedSearchedProducer to ListedProducer.
        /// </summary>
        /// <param name="searchedProducer">Producer to be converted</param>
        /// <returns>ListedProducer with name and ID of ListedSearchedProducer</returns>
        public static explicit operator ListedProducer(ListedSearchedProducer searchedProducer)
        {
            return new ListedProducer(searchedProducer.Name, -1, DateTime.MinValue, searchedProducer.ID, searchedProducer.Language);
        }
        
        /// <summary>
        /// Is producer already in favorite producers list? (Yes/No)
        /// </summary>
        public string InList { get; set; }

        /// <summary>
        /// Number of producer's titles finished by user
        /// </summary>
        public int FinishedTitles { get; set; }

        /// <summary>
        /// Number of producer's titles related to user
        /// </summary>
        public int URTTitles { get; set; }


	    /// <summary>
	    /// Producer Name
	    /// </summary>
	    public string Name { get; set; }

	    /// <summary>
	    /// Producer ID
	    /// </summary>
	    public int ID { get; set; }

	    /// <summary>
	    /// Language of Producer
	    /// </summary>
	    public string Language { get; set; }

	    /// <filterpriority>2</filterpriority>
	    public override string ToString() => $"ID={ID} Name={Name}";
	}
}