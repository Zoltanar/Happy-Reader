using System;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace HRGoogleTranslate
{

	public class GoogleTranslation
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }
		public string Input { get; set; }
		public string Output { get; set; }
		/// <summary>
		/// Timestamp of creation
		/// </summary>
		public DateTime CreatedAt { get; set; }
		/// <summary>
		/// Timestamp of last used
		/// </summary>
		public DateTime Timestamp { get; set; }
		/// <summary>
		/// Count of times used
		/// </summary>
		public int Count { get; set; }

		public GoogleTranslation(string input, string output)
		{
			Input = input;
			Output = output;
			CreatedAt = DateTime.UtcNow;
			Timestamp = DateTime.UtcNow;
			Count = 1;
		}

		/// <summary>
		/// Increase count by 1 and update timestamp.
		/// </summary>
		public void Update()
		{
			Count++;
			Timestamp = DateTime.UtcNow;
		}

		public GoogleTranslation() { }
	}
}