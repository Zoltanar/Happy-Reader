using System.IO;
using Newtonsoft.Json;

namespace Happy_Apps_Core
{
    public class SettingsJsonFile
    {
        protected bool Loaded { get; private set; }

        public string FilePath { get; private set; }
        
        public static T Load<T>(string jsonPath) where T : SettingsJsonFile, new()
        {
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
                StaticHelpers.LogToFile(exception);
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

        protected void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (JsonException exception)
            {
                StaticHelpers.LogToFile(exception);
            }
        }
    }
}