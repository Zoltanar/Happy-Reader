using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;

// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Happy_Apps_Core.Database
{
    /// <summary>
    /// Object for Favorite Producers in Object List View.
    /// </summary>
    public class ListedProducer : IComparable<ListedProducer>,IComparable, INotifyPropertyChanged
    {
        /// <summary>
        /// Constructor for ListedProducer, not favorite producers.
        /// </summary>
        /// <param name="name">Producer Name</param>
        /// <param name="updated">Date of last update to Producer</param>
        /// <param name="id">Producer ID</param>
        /// <param name="language">Language of producer</param>
        public ListedProducer(string name, DateTime? updated, int id, string language)
        {
            Name = name;
            UpdatedDt = updated;
            ID = id;
            Language = language;
        }

        /// <summary>
        /// Constructor for ListedProducer for favorite producers.
        /// </summary>
        /// <param name="name">Producer Name</param>
        /// <param name="updated">Date of last update to Producer</param>
        /// <param name="id">Producer ID</param>
        /// <param name="language">Language of producer</param>
        /// <param name="userAverageVote">User's average vote on Producer titles. (Only titles with votes)</param>
        /// <param name="userDropRate">User's average drop rate on Producer titles. (Dropped / (Finished+Dropped)</param>
        public ListedProducer(string name, DateTime updated, int id, string language,
            double userAverageVote, int userDropRate)
        {
            Name = name;
            Updated = updated.DaysSince();
            ID = id;
            Language = language;
            UserAverageVote = Math.Round(userAverageVote, 2);
            UserDropRate = userDropRate;
        }

        /// <summary>
        /// Number of Producer's titles
        /// </summary>
        [NotMapped]
        public int NumberOfTitles => Titles.Count;

        /// <summary>
        /// Days since last update (if any)
        /// </summary>
        [NotMapped]
        public int? Updated { get; set; }

        /// <summary>
        /// User's average vote on Producer titles. (Only titles with votes)
        /// </summary>
        [NotMapped]
        public double? UserAverageVote { get; set; }

        /// <summary>
        /// User's average drop rate on Producer titles. (Dropped / (Finished+Dropped)
        /// </summary>
        [NotMapped]
        public double? UserDropRate { get; set; }
		
        [NotMapped]
        public bool IsFavorited { get; set; }


		/// <summary>
		/// Bayesian average score of votes by all users.
		/// </summary>
		[NotMapped]
        public double? GeneralRating { get; set; }
        
        public ListedProducer()
        {
            FavoritedUsers = new HashSet<User>();
            Titles = new HashSet<ListedVN>();

        }

        public virtual ICollection<User> FavoritedUsers { get; set; }

        public virtual ICollection<ListedVN> Titles { get; set; }

        #region Columns

        /// <summary>
        /// Producer ID
        /// </summary>
        [Key]
        [Column("ProducerID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }

        /// <summary>
        /// Producer Name
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Language of Producer
        /// </summary>
        public string Language { get; set; }

        public string Loaded { get; set; }

				/// <summary>
				/// Time of last update (if any) in UTC
				/// </summary>
        [Column("Updated")]
        public DateTime? UpdatedDt { get; set; }

        public object FlagSource => Language != null ? Path.GetFullPath($"{StaticHelpers.FlagsFolder}{Language}.png")
                                        : DependencyProperty.UnsetValue;

        #endregion
        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => $"ID={ID} Name={Name}";

        public int CompareTo(object other)
        {
            if (ReferenceEquals(this, other)) return 0;
            switch (other)
            {
                case null:
                    return 1;
                case ListedProducer otherProducer:
                    return ID.CompareTo(otherProducer.ID);
                default:
                    return 1;
            }
        }

        public int CompareTo(ListedProducer other)
        {
            if (ReferenceEquals(this, other)) return 0;
            return other is null ? 1 : ID.CompareTo(other.ID);
        }

	    public event PropertyChangedEventHandler PropertyChanged;

	    [NotifyPropertyChangedInvocator]
	    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
		    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }

	    private bool _userDataSet;
	    public void SetFavoriteProducerData(VisualNovelDatabase database, DateTime now)
	    {
		    if (_userDataSet) return;
		    _userDataSet = true;
		    IsFavorited = database.CurrentUser.FavoriteProducers.Contains(this);
			if (!IsFavorited) { }
		    var titleIds = Titles.Select(x => x.VNID).ToList();
		    var userTitles = database.UserVisualNovels.Where(x=> titleIds.Contains(x.VNID) && x.UserId == database.CurrentUser.Id).ToList();
		    var userTitleVotes = userTitles.Where(x=> x.Vote != null).Select(x=> x.Vote).ToList();
			  UserAverageVote = userTitleVotes.Any() ? userTitleVotes.Average() / 10 : null;
		    var titlesDropped = userTitles.Count(x => x.ULStatus != null && x.ULStatus == UserlistStatus.Dropped);
		    var titlesFinished = userTitles.Count(x => x.ULStatus != null && x.ULStatus == UserlistStatus.Finished);
		    var titlesDroppedOrFinished = titlesDropped + titlesFinished;
				UserDropRate = titlesDroppedOrFinished > 0 ? (double?)(titlesDropped / (double)(titlesFinished + titlesDropped)) : null;
				GeneralRating = Titles.Any() ? (double?)Titles.Average(x => x.Rating) : null;
				Updated = UpdatedDt != null ? (int?)(now - UpdatedDt.Value).TotalDays : null;

	    }
    }
}