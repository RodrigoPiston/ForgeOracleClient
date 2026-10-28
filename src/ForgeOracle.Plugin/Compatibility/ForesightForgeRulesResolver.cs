using System;
using System.Reflection;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Game;
using HarmonyLib;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>
    /// ForesightForge (DrakosDJ) in TierScaled mode replaces DoCrafting but still rolls through the real
    /// <c>Player.GetItemUpgradeChance</c>. A roll above the idol's upgrade chance is classified by its public static
    /// <c>UpgradeSimulator.EvaluateFailure</c> using the idol tier: no change, downgrade (never below 1) or break.
    /// In Vanilla mode its prefix lets the vanilla code run, so the vanilla resolver applies.
    /// </summary>
    internal sealed class ForesightForgeRulesResolver : IRulesResolver
    {
        private Type? _plugin;
        private Type? _simulator;

        public string Id => "foresightforge";

        public string DisplayName => "Vanilla roll + ForesightForge TierScaled";

        public string PluginGuid => KnownMods.ForesightForge;

        public string? ForecastNoteKey => null;

        private Type? PluginType => _plugin ??= AccessTools.TypeByName("ForesightForge.ForesightForgePlugin");

        private Type? Simulator => _simulator ??= AccessTools.TypeByName("ForesightForge.UpgradeSimulator");

        public bool IsActive(ForgeScan scan)
        {
            return scan.IsLoaded(PluginGuid) && Mode()?.ToString() == "TierScaled";
        }

        public bool TryResolve(ForgeSelection selection, int targetQuality, out UpgradeRules? rules, out string problem)
        {
            rules = null;
            MethodInfo? tierOf = ModReflection.StaticMethod(Simulator, "GetIdolTier");
            MethodInfo? evaluate = ModReflection.StaticMethod(Simulator, "EvaluateFailure");
            object? mode = Mode();
            if (tierOf == null || evaluate == null || mode == null || selection.Idol == null)
            {
                problem = "ForesightForge simulator methods not found";
                return false;
            }

            ItemDrop.ItemData.SharedData idol = selection.Idol.m_itemData.m_shared;
            int tier = (int)ModReflection.Invoke(tierOf, selection.Idol);
            float up = idol.m_upgradeChance;
            float brk = idol.m_breakChance;
            FailureClassifier classifier = roll => Map(ModReflection.Invoke(evaluate, roll, up, brk, tier, mode));
            var failure = FailureModel.Custom(classifier, 1, 1, "ForesightForge TierScaled, idol tier " + tier);
            rules = new UpgradeRules(up, brk, idol.m_breakReturnIngreientsAmount,
                selection.Idol.name + " (ForesightForge tier " + tier + ")", failure);
            problem = string.Empty;
            return true;
        }

        private object? Mode()
        {
            PropertyInfo? property =
                PluginType == null ? null : AccessTools.Property(PluginType, "EffectiveOutcomeMode");
            return property?.GetValue(null, null);
        }

        private static FailureKind Map(object failureType)
        {
            switch (failureType.ToString())
            {
                case "NoChange":
                    return FailureKind.NoChange;
                case "Downgrade":
                    return FailureKind.LevelLoss;
                case "Break":
                    return FailureKind.Destroyed;
                default:
                    throw new InvalidOperationException("Unexpected ForesightForge failure type " + failureType);
            }
        }
    }
}
