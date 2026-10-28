using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Game;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>
    /// Produces the effective <see cref="UpgradeRules"/> for one forge implementation. Resolvers only read game
    /// and mod state (fields, config values, pure static methods); they never call gameplay methods.
    /// </summary>
    internal interface IRulesResolver
    {
        /// <summary>Provider id shown in diagnostics.</summary>
        string Id { get; }

        string DisplayName { get; }

        /// <summary>BepInEx GUID of the mod this resolver models, or empty for vanilla.</summary>
        string PluginGuid { get; }

        /// <summary>True when the mod is loaded and its forge behaviour is switched on.</summary>
        bool IsActive(ForgeScan scan);

        /// <summary>
        /// Note shown under the forecast (for example a skill that grows after each attempt), or null.
        /// </summary>
        string? ForecastNoteKey { get; }

        bool TryResolve(ForgeSelection selection, int targetQuality, out UpgradeRules? rules, out string problem);
    }

    /// <summary>The vanilla rules: the idol's own chances, unchanged.</summary>
    internal sealed class VanillaRulesResolver : IRulesResolver
    {
        public string Id => "vanilla";

        public string DisplayName => "Vanilla";

        public string PluginGuid => string.Empty;

        public string? ForecastNoteKey => null;

        public bool IsActive(ForgeScan scan) => true;

        public bool TryResolve(ForgeSelection selection, int targetQuality, out UpgradeRules? rules, out string problem)
        {
            problem = string.Empty;
            rules = null;
            if (selection.Idol == null)
            {
                problem = "no upgrader resource";
                return false;
            }

            ItemDrop.ItemData.SharedData idol = selection.Idol.m_itemData.m_shared;
            rules = new UpgradeRules(
                idol.m_upgradeChance, idol.m_breakChance, idol.m_breakReturnIngreientsAmount, selection.Idol.name);
            return true;
        }
    }
}
