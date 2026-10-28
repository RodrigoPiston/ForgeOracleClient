using System;
using System.Collections.Generic;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Prediction;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>
    /// Decides which forge implementation is live and how far a prediction can be trusted. A replaced roll, an
    /// unknown patch on the roll, or two forge overhauls at once block prediction; unknown DoCrafting prefixes or
    /// transpilers make it unverified.
    /// </summary>
    internal sealed class ForgeEnvironment
    {
        private static readonly IRulesResolver Vanilla = new VanillaRulesResolver();

        private static readonly IRulesResolver[] Overhauls =
        {
            new ImpactfulSkillsRulesResolver(),
            new PotentialReforgedRulesResolver(),
            new ForesightForgeRulesResolver(),
        };

        private ForgeEnvironment(IRulesResolver resolver, IReadOnlyList<CompatibilityIssue> issues, ForgeScan scan)
        {
            Resolver = resolver;
            Issues = issues;
            Scan = scan;
            Provider = HasBlocking(issues)
                ? new UnsupportedForgePredictionProvider(
                    "unsupported", "Modified forge", UnknownReason.ModifiedAlgorithm)
                : (IForgePredictionProvider)new SeededForgePredictionProvider(resolver.Id, resolver.DisplayName);
        }

        public IRulesResolver Resolver { get; }

        public IForgePredictionProvider Provider { get; }

        public IReadOnlyList<CompatibilityIssue> Issues { get; }

        public ForgeScan Scan { get; }

        public static IReadOnlyList<IRulesResolver> AllResolvers()
        {
            var all = new List<IRulesResolver> { Vanilla };
            all.AddRange(Overhauls);
            return all;
        }

        public static ForgeEnvironment Detect(string ownHarmonyId)
        {
            ForgeScan scan = ForgeScan.Capture(ownHarmonyId);
            var issues = new List<CompatibilityIssue>();
            ClassifyRollPatches(scan, issues);
            ClassifyCraftPatches(scan, issues);
            if (ModReflection.IsLoaded(KnownMods.ReforgedPotential))
            {
                issues.Add(new CompatibilityIssue("random-roll", IssueSeverity.Blocking, KnownMods.ReforgedPotential,
                    "ReforgedPotential rolls with the unseeded global Unity RNG; the outcome cannot be known."));
            }

            return new ForgeEnvironment(SelectResolver(scan, issues), issues, scan);
        }

        private static IRulesResolver SelectResolver(ForgeScan scan, List<CompatibilityIssue> issues)
        {
            var active = new List<IRulesResolver>();
            foreach (IRulesResolver resolver in Overhauls)
            {
                if (resolver.IsActive(scan))
                {
                    active.Add(resolver);
                }
            }

            if (active.Count > 1)
            {
                issues.Add(new CompatibilityIssue("multiple-forge-overhauls", IssueSeverity.Blocking,
                    string.Join(", ", active.ConvertAll(r => r.PluginGuid).ToArray()),
                    "Several mods change the forge decision; their combined effect is not modelled."));
                return Vanilla;
            }

            if (active.Count == 0)
            {
                return Vanilla;
            }

            AddVersionIssues(active[0], issues);
            return active[0];
        }

        private static void AddVersionIssues(IRulesResolver resolver, List<CompatibilityIssue> issues)
        {
            KnownMod? mod = KnownMods.Find(resolver.PluginGuid);
            string version = ModReflection.Version(resolver.PluginGuid);
            if (mod != null && version != mod.AuditedVersion)
            {
                issues.Add(new CompatibilityIssue("untested-version", IssueSeverity.Unverified, resolver.PluginGuid,
                    $"{mod.Name} {version} is loaded; the provider was audited against {mod.AuditedVersion}."));
            }

            if (!LiveValidation.IsValidated(resolver.PluginGuid))
            {
                issues.Add(new CompatibilityIssue("provider-not-live-tested", IssueSeverity.Unverified,
                    resolver.PluginGuid, $"The {resolver.DisplayName} provider has not been validated in game yet."));
            }
        }

        private static void ClassifyRollPatches(ForgeScan scan, List<CompatibilityIssue> issues)
        {
            foreach (ForeignPatch patch in scan.RollPatches)
            {
                issues.Add(new CompatibilityIssue("foreign-roll-patch", IssueSeverity.Blocking, patch.Owner,
                    patch + ": the roll itself may be changed."));
            }
        }

        private static void ClassifyCraftPatches(ForgeScan scan, List<CompatibilityIssue> issues)
        {
            foreach (ForeignPatch patch in scan.CraftPatches)
            {
                KnownMod? mod = KnownMods.Find(patch.Owner);
                if (mod != null)
                {
                    issues.Add(new CompatibilityIssue("known-forge-patch", IssueSeverity.Info, patch.Owner,
                        $"{patch} ({mod.Name}: {mod.Note})"));
                    continue;
                }

                IssueSeverity severity = patch.CanChangeDecision ? IssueSeverity.Unverified : IssueSeverity.Info;
                issues.Add(new CompatibilityIssue("unknown-forge-patch", severity, patch.Owner,
                    patch + (patch.CanChangeDecision
                        ? ": not audited, it could change the outcome."
                        : ": runs after the decision.")));
            }
        }

        private static bool HasBlocking(IReadOnlyList<CompatibilityIssue> issues)
        {
            foreach (CompatibilityIssue issue in issues)
            {
                if (issue.Severity == IssueSeverity.Blocking)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
