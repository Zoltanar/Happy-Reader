using System;

namespace Happy_Apps_Core
{
    /// <summary>
    /// From get producer commands
    /// </summary>
    public class ProducerItem
    {
        public int ID { get; set; }
        public bool Developer { get; set; }
        public bool Publisher { get; set; }
        public string Name { get; set; }
        public string Original { get; set; }
        public string Type { get; set; }
        public string Language { get; set; }


        /// <summary>
        /// Convert ProducerItem to ListedProducer.
        /// </summary>
        /// <param name="producer">Producer to be converted</param>
        public static explicit operator ListedProducer(ProducerItem producer)
        {
            return new ListedProducer(producer.Name, -1, DateTime.MinValue, producer.ID, producer.Language);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => $"ID={ID} Name={Name}";
    }
}