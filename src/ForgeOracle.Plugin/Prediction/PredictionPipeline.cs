using System;
using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Items;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Compatibility;
using ForgeOracle.Plugin.Diagnostics;
using ForgeOracle.Plugin.Game;

namespace ForgeOracle.Plugin.Prediction
{
    /// <summary>
    /// Turns a live forge selection into a prediction: environment scan, rules, seed snapshot, provider.
    /// </summary>
    internal sealed class PredictionPipeline
    {
        private readonly string _harmonyId;
        private readonly Func<RngSelfTestResult> _rng;

        public PredictionPipeline(string harmonyId, Func<RngSelfTestResult> rng)
        {
            _harmonyId = harmonyId;
            _rng = rng;
        }

        /// <summary>The environment found by the latest prediction, for diagnostics.</summary>
        public ForgeEnvironment? LastEnvironment { get; private set; }

        public PredictionResult Unknown(UnknownReason reason, string error = "")
        {
            ForgeEnvironment env = LastEnvironment ?? ForgeEnvironment.Detect(_harmonyId);
            return PredictionResult.Unknown(reason, env.Provider.Id, env.Provider.DisplayName, env.Issues, error);
        }

        public PredictionResult Predict(ForgeSelection selection, int count)
        {
            ForgeEnvironment env = ForgeEnvironment.Detect(_harmonyId);
            LastEnvironment = env;
            RngSelfTestResult rng = _rng();
            if (rng.Ran && !rng.Passed)
            {
                return PredictionResult.Unknown(UnknownReason.RngMismatch, env.Provider.Id, env.Provider.DisplayName,
                    env.Issues, rng.ToString());
            }

            Dictionary<int, UpgradeRules> rules = ResolveRules(env.Resolver, selection, count, out string problem);
            if (!rules.ContainsKey(selection.TargetQuality))
            {
                return PredictionResult.Unknown(UnknownReason.MissingThresholds, env.Provider.Id,
                    env.Provider.DisplayName, env.Issues, problem);
            }

            ForgeContext context = BuildContext(selection, rules, rng, count, Issues(env, selection));
            return new ForgePredictionService(new[] { env.Provider }).Predict(context);
        }

        private static ForgeContext BuildContext(ForgeSelection selection, Dictionary<int, UpgradeRules> rules,
            RngSelfTestResult rng, int count, IReadOnlyList<CompatibilityIssue> issues)
        {
            ItemIdentity item = selection.Identity();
            SeedSnapshot seeds = LiveSeeds.Snapshot(selection.Player, item, selection.CurrentQuality, count);
            return new ForgeContext(item, selection.CurrentQuality, selection.Player.GetPlayerName(), seeds, rules,
                rng.ToProfile(), count, issues);
        }

        /// <summary>Rules for the target level and each forecast level; stops at the first level that fails.</summary>
        private static Dictionary<int, UpgradeRules> ResolveRules(IRulesResolver resolver, ForgeSelection selection,
            int count, out string problem)
        {
            var rules = new Dictionary<int, UpgradeRules>();
            problem = string.Empty;
            for (int level = selection.TargetQuality; level < selection.TargetQuality + count; level++)
            {
                if (!resolver.TryResolve(selection, level, out UpgradeRules? resolved, out problem) || resolved == null)
                {
                    break;
                }

                rules[level] = resolved;
            }

            return rules;
        }

        private static IReadOnlyList<CompatibilityIssue> Issues(ForgeEnvironment env, ForgeSelection selection)
        {
            var issues = new List<CompatibilityIssue>(env.Issues);
            bool qualityIdol = selection.Idol != null && selection.Idol.m_itemData.m_shared.m_maxQuality > 1;
            if (qualityIdol && env.Resolver.PluginGuid == KnownMods.ImpactfulSkills)
            {
                issues.Add(new CompatibilityIssue("idol-quality-scaling", IssueSeverity.Unverified,
                    KnownMods.ImpactfulSkills, "This idol has quality levels; ImpactfulSkills may raise the result."));
            }

            return issues;
        }
    }
}
