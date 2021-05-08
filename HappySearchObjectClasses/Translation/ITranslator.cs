using System;
using System.Collections.Generic;

namespace Happy_Apps_Core.Translation
{
	public interface ITranslator
	{
		/// <summary>
		/// User-displayed version text.
		/// </summary>
		public string Version { get; }

		/// <summary>
		/// Name of translator, saved to database.
		/// Should be a valid file name if loading/saving properties to file.
		/// </summary>
		public string SourceName { get; }

		/// <summary>
		/// Dictionary of properties by name and type.
		/// These are used to present options in UI for user to change them.
		/// </summary>
		public IReadOnlyDictionary<string,Type> Properties { get; }

		/// <summary>
		/// Contains string for error, if this property is not null, it indicates it cannot be used.
		/// </summary>
		string Error { get; set; }

		/// <summary>
		/// Initialise translator, <see cref="LoadProperties"/> is called first.
		/// Should set <see cref="Error"/> to <c>null</c> at the start and should set it to the <c>exception message</c> if an error occurs. 
		/// </summary>
		public void Initialise();

		/// <summary>
		/// Load properties from a file for the translator.
		/// The implementation may choose to ignore this path and load properties in any other way.
		/// Called before <see cref="Initialise"/>.
		/// </summary>
		public void LoadProperties(string filePath);

		/// <summary>
		/// Saves properties for the translator to a file.
		/// The implementation may choose to ignore this path and save properties in any other way.
		/// </summary>
		public void SaveProperties(string filePath); //todo call this at some point (maybe on program exit)

		/// <summary>
		/// Called when a property is changed.
		/// </summary>
		public void SetProperty(string propertyName, object value);

		/// <summary>
		/// Called when loading, to get the value of a property.
		/// </summary>
		public object GetProperty(string propertyName);

		/// <summary>
		/// Returns whether translation was successful.
		/// </summary>
		public bool Translate(string input, out string output);
	}
}
