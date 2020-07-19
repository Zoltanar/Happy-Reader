using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Happy_Apps_Core
{
	public abstract class SettingsJsonFile : INotifyPropertyChanged
	{
		protected bool Loaded { get; private set; }

		protected string FilePath { get; private set; }

		public static T Load<T>(string jsonPath) where T : SettingsJsonFile, new()
		{
			System.Diagnostics.Debug.WriteLine($"Loading file...: {jsonPath}");
			T settings = null;
			if (!File.Exists(jsonPath))
			{
				var result = new T
				{
					FilePath = jsonPath,
					Loaded = true
				};
				result.Save();
				return result;
			}
			try
			{
				settings = JsonConvert.DeserializeObject<T>(File.ReadAllText(jsonPath));
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

			if (settings == null)
			{
				settings = new T
				{
					FilePath = jsonPath,
					Loaded = true
				};
				settings.Save();
			}
			return settings;
		}

		protected void Save([CallerMemberName]string source = null)
		{
			System.Diagnostics.Debug.WriteLine($"Saving file ({source})...: {FilePath}");
			try
			{
				File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
			}
			catch (JsonException exception)
			{
				StaticHelpers.Logger.ToFile(exception);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}