using System;
using System.Reflection;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Game;
using HarmonyLib;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>
    /// PotentialReforged (Dyls123) keeps the vanilla seeded roll but transpiles DoCrafting so the success and break
    /// thresholds and the failure quality come from <c>Plugin.GetSuccessThreshold</c>, <c>GetBreakThreshold</c> and
    /// <c>GetFailureQuality</c>. This resolver calls those same public static methods (server-synced config).
    /// </summary>
    internal sealed class PotentialReforgedRulesResolver : IRulesResolver
    {
        private Type? _plugin;

        public string Id => "potentialreforged";

        public string DisplayName => "Vanilla roll + PotentialReforged rules";

        public string PluginGuid => KnownMods.PotentialReforged;

        public string? ForecastNoteKey => null;

        private Type? PluginType => _plugin ??= AccessTools.TypeByName("PotentialReforged.Plugin");

        /// <summary>
        /// Active only while its transpiler is applied (the mod unpatches itself when it cannot apply).
        /// </summary>
        public bool IsActive(ForgeScan scan)
        {
            return scan.IsLoaded(PluginGuid) && scan.HasCraftPatch(PluginGuid, PatchKind.Transpiler);
        }

        public bool TryResolve(ForgeSelection selection, int targetQuality, out UpgradeRules? rules, out string problem)
        {
            rules = null;
            MethodInfo? success = ModReflection.StaticMethod(PluginType, "GetSuccessThreshold");
            MethodInfo? brk = ModReflection.StaticMethod(PluginType, "GetBreakThreshold");
            MethodInfo? quality = ModReflection.StaticMethod(PluginType, "GetFailureQuality");
            if (success == null || brk == null || quality == null || selection.Idol == null)
            {
                problem = "PotentialReforged threshold methods not found";
                return false;
            }

            ItemDrop.ItemData.SharedData idol = selection.Idol.m_itemData.m_shared;
            float successChance = (float)ModReflection.Invoke(success, idol.m_upgradeChance);
            float breakChance = (float)ModReflection.Invoke(brk, idol.m_breakChance);
            int failureQuality = (int)ModReflection.Invoke(quality, targetQuality, 2);
            var failure = FailureModel.WithLevelLoss(
                targetQuality - 1 - failureQuality, 1, "PotentialReforged failure quality " + failureQuality);
            rules = new UpgradeRules(successChance, breakChance, idol.m_breakReturnIngreientsAmount,
                selection.Idol.name + " (PotentialReforged)", failure);
            problem = string.Empty;
            return true;
        }
    }
}
