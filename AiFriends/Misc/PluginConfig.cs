using BepInEx.Configuration;

namespace AiFriends.Misc
{
    public class PluginConfig
    {
        readonly ConfigFile configFile;

        public bool HELPER { get; set; }

        public PluginConfig(ConfigFile cfg)
        {
            configFile = cfg;
        }

        private T ConfigEntry<T>(string section, string key, T defaultVal, string description)
        {
            return configFile.Bind(section, key, defaultVal, description).Value;
        }

        public void InitBindings()
        {
            HELPER = ConfigEntry("Hire Helper", "Hire an AI Helper.", true, "");
        }

    }
}
