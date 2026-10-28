using System.Collections.Generic;
using System.Globalization;
using ForgeOracle.Plugin.Configuration;

namespace ForgeOracle.Plugin.Localization
{
    /// <summary>Picks the UI language from the config or the game's selected language.</summary>
    internal sealed class Localizer
    {
        private readonly PluginConfig _config;
        private Dictionary<string, string> _active = Texts.English;

        public Localizer(PluginConfig config)
        {
            _config = config;
        }

        /// <summary>Changes whenever the effective language changes, so cached UI text can be rebuilt.</summary>
        public int Version { get; private set; }

        /// <summary>Re-reads the game language; cheap enough to call every frame.</summary>
        public void Refresh()
        {
            string game = global::Localization.instance?.GetSelectedLanguage() ?? string.Empty;
            Dictionary<string, string> next = Resolve(_config.Language.Value, game);
            if (!ReferenceEquals(next, _active))
            {
                _active = next;
                Version++;
            }
        }

        public string Get(string key)
        {
            if (_active.TryGetValue(key, out string value) || Texts.English.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        public string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), args);
        }

        private static Dictionary<string, string> Resolve(LanguageOption option, string gameLanguage)
        {
            switch (option)
            {
                case LanguageOption.English:
                    return Texts.English;
                case LanguageOption.Spanish:
                    return Texts.Spanish;
                default:
                    return gameLanguage == "Spanish" ? Texts.Spanish : Texts.English;
            }
        }
    }
}
