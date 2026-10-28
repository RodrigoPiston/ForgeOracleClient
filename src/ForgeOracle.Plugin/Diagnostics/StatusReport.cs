using System.Collections.Generic;
using System.Globalization;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Items;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Compatibility;
using ForgeOracle.Plugin.Game;

namespace ForgeOracle.Plugin.Diagnostics
{
    /// <summary>
    /// Text for the console commands. It never prints file paths, the character file or the whole seed table;
    /// only the entries for the selected item's next levels.
    /// </summary>
    internal sealed class StatusReport
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly PluginRuntime _runtime;

        public StatusReport(PluginRuntime runtime)
        {
            _runtime = runtime;
        }

        public IEnumerable<string> Status()
        {
            ForgeEnvironment env = ForgeEnvironment.Detect(ForgeOracleClientPlugin.Guid);
            yield return $"Forge Oracle {ForgeOracleClientPlugin.Version}, game {(global::Version.CurrentVersion)}";
            yield return "Enabled: " + _runtime.Config.Enabled.Value;
            yield return "RNG self-test: " + _runtime.Rng;
            yield return $"Forge implementation: {env.Provider.DisplayName} ({env.Provider.Id})";
            foreach (string line in IssueLines(env.Issues))
            {
                yield return line;
            }

            foreach (string line in SelectionSummary())
            {
                yield return line;
            }

            AttemptRecorder rec = _runtime.Recorder;
            yield return $"Validated attempts this session: {rec.Matched}/{rec.Total} matched";
        }

        public IEnumerable<string> Item()
        {
            ForgeSelection? sel = Selection(out string why);
            if (sel == null)
            {
                yield return why;
                yield break;
            }

            ItemIdentity id = sel.Identity();
            yield return $"Prefab: {id.PrefabName} (hash {id.PrefabHash}), token {id.NameToken}, " +
                $"name {(global::Localization.instance.Localize(id.NameToken))}";
            yield return $"Quality {sel.CurrentQuality} -> {sel.TargetQuality} (declared max {id.MaxQuality})";
            int key = UpgradeKeys.ComputeKey(id, sel.TargetQuality);
            int? seed = LiveSeeds.EffectiveSeed(sel.Player, key, out bool stored);
            yield return $"Key {key}: seed {(seed?.ToString(Invariant) ?? "unavailable")} " +
                $"({(stored ? "stored" : "derived from name")}); table has {LiveSeeds.Count(sel.Player)} entries";
            if (sel.Idol != null)
            {
                ItemDrop.ItemData.SharedData idol = sel.Idol.m_itemData.m_shared;
                yield return $"Idol {sel.Idol.name}: upgrade {idol.m_upgradeChance:R}, break {idol.m_breakChance:R}, " +
                    $"refund {idol.m_breakReturnIngreientsAmount:R}";
            }
        }

        public IEnumerable<string> Predict(int count)
        {
            ForgeSelection? sel = Selection(out string why);
            if (sel == null)
            {
                return new[] { why };
            }

            PredictionResult result = _runtime.Pipeline.Predict(sel, System.Math.Max(1, System.Math.Min(20, count)));
            var lines = new List<string>
            {
                $"{result.Status} ({result.Confidence}, {result.ProviderName}) {result.Reason}",
            };
            lines.Add("Repeated attempts at +" + sel.TargetQuality + ":");
            lines.AddRange(Rows(result.RepeatedAttempts));
            lines.Add("Level by level (hypothetical after a predicted failure):");
            lines.AddRange(Rows(result.Progression));
            return lines;
        }

        public IEnumerable<string> Providers()
        {
            ForgeEnvironment env = ForgeEnvironment.Detect(ForgeOracleClientPlugin.Guid);
            foreach (IRulesResolver r in ForgeEnvironment.AllResolvers())
            {
                KnownMod? mod = KnownMods.Find(r.PluginGuid);
                string loaded = r.PluginGuid.Length == 0 ? "built in" : ModReflection.Version(r.PluginGuid);
                yield return $"{r.Id}: active={r.IsActive(env.Scan)}, loaded={(loaded.Length > 0 ? loaded : "no")}, " +
                    $"audited={mod?.AuditedVersion ?? "-"}, live-validated={LiveValidation.IsValidated(r.PluginGuid)}";
            }

            foreach (ForeignPatch patch in env.Scan.RollPatches)
            {
                yield return "Foreign patch: " + patch;
            }

            foreach (ForeignPatch patch in env.Scan.CraftPatches)
            {
                yield return "Foreign patch: " + patch;
            }

            yield return "Selected: " + env.Provider.DisplayName;
        }

        public IEnumerable<string> History()
        {
            yield return $"{_runtime.Recorder.Matched}/{_runtime.Recorder.Total} attempts matched this session";
            foreach (AttemptObservation observation in _runtime.Recorder.History)
            {
                yield return observation.ToString();
            }
        }

        private IEnumerable<string> SelectionSummary()
        {
            ForgeSelection? sel = Selection(out string why);
            if (sel == null)
            {
                yield return why;
                yield break;
            }

            PredictionResult result = _runtime.Pipeline.Predict(sel, 1);
            yield return $"Selected: {sel.Recipe.m_item.gameObject.name} +{sel.CurrentQuality} -> +{sel.TargetQuality}";
            yield return $"Seed: {result.Next?.SeedOrigin.ToString() ?? "unavailable"}";
            yield return $"Next attempt: {result.Status}, confidence {result.Confidence} {result.Reason}";
        }

        private static IEnumerable<string> IssueLines(IReadOnlyList<CompatibilityIssue> issues)
        {
            foreach (CompatibilityIssue issue in issues)
            {
                yield return "  " + issue;
            }
        }

        private static IEnumerable<string> Rows(IReadOnlyList<AttemptForecast> rows)
        {
            foreach (AttemptForecast row in rows)
            {
                string roll = row.Roll.Roll.ToString("0.000000", Invariant);
                yield return $"  {row.Index + 1}. +{row.TargetQuality} {row.Status} roll {roll} " +
                    $"seed {row.Roll.Seed} -> {row.Roll.NextSeed} {(row.Conditional ? "(conditional)" : string.Empty)}";
            }
        }

        private static ForgeSelection? Selection(out string why)
        {
            Player? player = Player.m_localPlayer;
            InventoryGui? gui = InventoryGui.instance;
            why = "No forge selection: open a Forge of Potential and select an item.";
            if (player == null || gui == null)
            {
                return null;
            }

            SelectionState state = ForgeSelection.FromGui(gui, player, out ForgeSelection? selection);
            return state == SelectionState.Ready ? selection : null;
        }
    }
}
