using System;
using System.Collections.Generic;
using System.Text;
using Happy_Apps_Core.DataAccess;
using JetBrains.Annotations;

namespace Happy_Apps_Core.Translation
{
	public interface ITranslator
	{
		/// <summary>
		/// Name of translator, saved to database.
		/// </summary>
		public string SourceName { get; }

		/// <summary>
		/// Dictionary of properties by name and type.
		/// These are used to present options in UI for user to change them.
		/// </summary>
		public Dictionary<string,Type> Properties { get; }

		/// <summary>
		/// Contains string for error, if this property is not null, it indicates it cannot be used.
		/// Should be set to null at start of Initialise.
		/// </summary>
		string Error { get; set; }

		public void Initialise([NotNull] Dictionary<string, object> properties);

		/// <summary>
		/// Returns whether translation was successful.
		/// </summary>
		public bool Translate(string input, out string output);
	}
}
