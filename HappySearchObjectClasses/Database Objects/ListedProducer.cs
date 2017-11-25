using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Happy_Apps_Core.Database;

namespace Happy_Apps_Core
{
    /// <summary>
    /// Object for Favorite Producers in Object List View.
    /// </summary>
    [Table("producerlist")]
    public class ListedProducer
    {
        /// <summary>
        /// Constructor for ListedProducer, not favorite producers.
        /// </summary>
        /// <param name="name">Producer Name</param>
        /// <param name="numberOfTitles">Number of Producer's titles</param>
        /// <param name="updated">Date of last update to Producer</param>
        /// <param name="id">Producer ID</param>
        /// <param name="language">Language of producer</param>
        public ListedProducer(string name, int numberOfTitles, DateTime updated, int id, string language)
        {
            Name = name;
            NumberOfTitles = numberOfTitles;
            Updated = StaticHelpers.DaysSince(updated);
            ID = id;
            Language = language;
        }

        /// <summary>
        /// Constructor for ListedProducer for favorite producers.
        /// </summary>
        /// <param name="name">Producer Name</param>
        /// <param name="numberOfTitles">Number of Producer's titles</param>
        /// <param name="updated">Date of last update to Producer</param>
        /// <param name="id">Producer ID</param>
        /// <param name="language">Language of producer</param>
        /// <param name="userAverageVote">User's average vote on Producer titles. (Only titles with votes)</param>
        /// <param name="userDropRate">User's average drop rate on Producer titles. (Dropped / (Finished+Dropped)</param>
        public ListedProducer(string name, int numberOfTitles, DateTime updated, int id, string language,
            double userAverageVote, int userDropRate)
        {
            Name = name;
            NumberOfTitles = numberOfTitles;
            Updated = StaticHelpers.DaysSince(updated);
            ID = id;
            Language = language;
            UserAverageVote = Math.Round(userAverageVote, 2);
            UserDropRate = userDropRate;
        }

        /// <summary>
        /// Number of Producer's titles
        /// </summary>
        [NotMapped]
        public int NumberOfTitles { get; set; }
        /// <summary>
        /// Date of last update to Producer
        /// </summary>
        [NotMapped]
        public int Updated { get; set; }
        /// <summary>
        /// User's average vote on Producer titles. (Only titles with votes)
        /// </summary>
        [NotMapped]
        public double UserAverageVote { get; set; }
        /// <summary>
        /// User's average drop rate on Producer titles. (Dropped / (Finished+Dropped)
        /// </summary>
        [NotMapped]
        public int UserDropRate { get; set; }
        /// <summary>
        /// Bayesian average score of votes by all users.
        /// </summary>
        [NotMapped]
        public double GeneralRating { get; set; }

        // ReSharper disable once VirtualMemberCallInConstructor
	    public ListedProducer() { Users = new HashSet<User>();}
        
        public virtual ICollection<User> Users { get; set; }
        #region Columns

        /// <summary>
        /// Producer Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Producer ID
        /// </summary>
        [Column("ProducerID")]
        public int ID { get; set; }

        /// <summary>
        /// Language of Producer
        /// </summary>
        public string Language { get; set; }

        public int Titles { get; set; }

        public string Loaded { get; set; }

        [Column("Updated")]
        public DateTime? UpdatedDt { get; set; }

        #endregion
        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => $"ID={ID} Name={Name}";

    }
}