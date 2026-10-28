using System;
using System.Reflection;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Game;
using HarmonyLib;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>
    /// ImpactfulSkills (MidnightsFX) overwrites the idol odds in a DoCrafting prefix from the forging skill and a
    /// per-level decay, then restores them. Its own <c>ForgingRefinement.TryGetChances</c> computes the same
    /// values outside DoCrafting, so this resolver calls it instead of re-implementing the formula.
    /// It also turns a downgrade to quality 0 into quality 1, and grants forging XP after every attempt.
    /// With ReforgedPotential loaded the environment is blocking anyway, so its RP mode is not modelled.
    /// </summary>
    internal sealed class ImpactfulSkillsRulesResolver : IRulesResolver
    {
        private Type? _refinement;
        private Type? _config;

        public string Id => "impactfulskills";

        public string DisplayName => "Vanilla roll + ImpactfulSkills odds";

        public string PluginGuid => KnownMods.ImpactfulSkills;

        public string? ForecastNoteKey => "panel.forecast.skilldrift";

        // Looked up only once the mod is known to be loaded; AccessTools logs a warning for missing types.
        private Type? Refinement => _refinement ??= AccessTools.TypeByName("ImpactfulSkills.patches.ForgingRefinement");

        private Type? Config => _config ??= AccessTools.TypeByName("ImpactfulSkills.ValConfig");

        public bool IsActive(ForgeScan scan)
        {
            return scan.IsLoaded(PluginGuid) && ModReflection.StaticConfigValue(Config, "EnableForging", false);
        }

        public bool TryResolve(ForgeSelection selection, int targetQuality, out UpgradeRules? rules, out string problem)
        {
            rules = null;
            MethodInfo? tryGet = ModReflection.StaticMethod(Refinement, "TryGetChances");
            if (tryGet == null || selection.Idol == null)
            {
                problem = tryGet == null ? "ForgingRefinement.TryGetChances not found" : "no upgrader resource";
                return false;
            }

            ItemDrop.ItemData.SharedData idol = selection.Idol.m_itemData.m_shared;
            object?[] args = { selection.Player, idol, targetQuality, null };
            if (!(bool)ModReflection.Invoke(tryGet, args) || args[3] == null)
            {
                problem = "ImpactfulSkills returned no chances";
                return false;
            }

            rules = ToRules(args[3]!, selection.Idol.name);
            problem = string.Empty;
            return true;
        }

        private static UpgradeRules ToRules(object chances, string idolName)
        {
            Type type = chances.GetType();
            float success = (float)AccessTools.Field(type, "Success").GetValue(chances);
            float brk = (float)AccessTools.Field(type, "Break").GetValue(chances);
            float refund = (float)AccessTools.Field(type, "Refund").GetValue(chances);
            var failure = FailureModel.WithLevelLoss(1, 1, "ImpactfulSkills: a downgrade never goes below quality 1");
            return new UpgradeRules(success, brk, refund, idolName + " (ImpactfulSkills)", failure);
        }
    }
}
