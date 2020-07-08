using JetBrains.Annotations;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get user commands
    /// </summary>
    [UsedImplicitly]
    public class UserItem
    {
        public int ID { get; set; }
        public string Username { get; set; }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => $"ID={ID} Username={Username}";
    }
}