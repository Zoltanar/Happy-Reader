﻿using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Apps_Core
{
	public abstract class SettingsJsonFile : INotifyPropertyChanged
	{
		[JsonIgnore]
		public virtual bool Loaded { get; set; }

		[JsonIgnore]
		public virtual string FilePath { get; set; }

		[JsonIgnore]
		public object ObjectToSerialise { get; set; }

		public static T Load<T>(string jsonPath, JsonSerializerSettings serialiserSettings = null) where T : SettingsJsonFile, new()
		{
			StaticHelpers.Logger.ToDebug($"Loading file...: {jsonPath}");
			T settings = null;
			if (File.Exists(jsonPath))
			{
				try
				{
					settings = JsonConvert.DeserializeObject<T>(File.ReadAllText(jsonPath), serialiserSettings);
					if (settings != null)
					{
						settings.FilePath = jsonPath;
						settings.Loaded = true;
					}
				}
				catch (JsonException exception)
				{
					StaticHelpers.Logger.ToFile(exception);
				}
			}
			if (settings != null) return settings;
			settings = new T
			{
				FilePath = jsonPath,
				Loaded = true
			};
			settings.Save();
			return settings;
		}

		protected void Save([CallerMemberName] string source = null)
		{
			StaticHelpers.Logger.ToDebug($"Saving file ({source})...: {FilePath}");
			try
			{
				File.WriteAllText(FilePath, JsonConvert.SerializeObject(ObjectToSerialise ?? this, Formatting.Indented));
			}
			catch (JsonException exception)
			{
				StaticHelpers.Logger.ToFile(exception);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}