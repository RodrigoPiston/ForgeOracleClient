using System.Collections.Generic;
using System.Globalization;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Configuration;
using ForgeOracle.Plugin.Diagnostics;
using ForgeOracle.Plugin.Game;
using ForgeOracle.Plugin.Localization;

namespace ForgeOracle.Plugin.UI
{
    /// <summary>Formats a prediction for the compact line and the detailed panel.</summary>
    internal sealed class ViewModelBuilder
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly PluginConfig _cfg;
        private readonly Localizer _loc;

        public ViewModelBuilder(PluginConfig cfg, Localizer loc)
        {
            _cfg = cfg;
            _loc = loc;
        }

        public ViewModel Build(PredictionResult result, ForgeSelection? selection, string? forecastNoteKey,
            RngSelfTestResult rng)
        {
            bool hidden = _cfg.HideWhenUnverified.Value && result.Confidence != PredictionConfidence.Verified;
            if (!result.IsKnown || hidden || result.Next == null)
            {
                string reason = hidden ? _loc.Format("warn.unverified", ConfidenceText(result.Confidence))
                    : _loc.Get("reason." + result.Reason);
                return Unavailable(result, selection, reason);
            }

            AttemptForecast next = result.Next;
            string compact = CompactKey(next);
            string color = Palette.For(next.Status, _cfg.ColorblindPalette.Value);
            string body = string.Join("\n", KnownLines(result, next, selection, rng).ToArray());
            return new ViewModel(next.Status, compact, color, body, Forecast(result, forecastNoteKey))
            {
                Icon = selection?.Item.GetIcon(),
                CompactNote = _cfg.ShowConfidence.Value ? ConfidenceText(result.Confidence) : string.Empty,
            };
        }

        /// <summary>Shown after an attempt when automatic refresh is off.</summary>
        public ViewModel Stale(ForgeSelection? selection)
        {
            string text = _loc.Get("compact.unavailable");
            string body = Header(selection) + "\n\n" + _loc.Get("reason.Stale");
            string gray = Palette.For(PredictionStatus.Unknown, false);
            return new ViewModel(PredictionStatus.Unknown, text, gray, body, string.Empty);
        }

        private ViewModel Unavailable(PredictionResult result, ForgeSelection? selection, string reason)
        {
            string key = result.Reason == UnknownReason.ModifiedAlgorithm ? "compact.modified" : "compact.unavailable";
            var lines = new List<string> { Header(selection), string.Empty, reason };
            lines.Add(_loc.Format("panel.provider", result.ProviderName));
            AddIssues(lines, result.Issues, IssueSeverity.Unverified);
            string color = Palette.For(PredictionStatus.Unknown, false);
            return new ViewModel(PredictionStatus.Unknown, _loc.Get(key), color, string.Join("\n", lines.ToArray()), "")
            {
                Icon = selection?.Item.GetIcon(),
            };
        }

        private List<string> KnownLines(PredictionResult result, AttemptForecast next, ForgeSelection? selection,
            RngSelfTestResult rng)
        {
            var lines = new List<string> { Header(selection), string.Empty, $"<b>{_loc.Get("panel.next")}</b>" };
            string color = Palette.For(next.Status, _cfg.ColorblindPalette.Value);
            lines.Add($"<color={color}><b>{Outcome(next)}</b></color>");
            if (_cfg.ShowExactRoll.Value && result.Rules != null)
            {
                lines.Add(_loc.Format("panel.roll", next.Roll.Roll.ToString("0.000000", Invariant)));
                lines.Add(_loc.Format("panel.threshold", result.Rules.SuccessChance.ToString("0.000000", Invariant)));
                lines.Add(BreakLine(result.Rules));
            }

            if (_cfg.ShowConfidence.Value)
            {
                lines.Add(_loc.Format("panel.confidence", ConfidenceText(result.Confidence)));
            }

            lines.Add(_loc.Format("panel.provider", result.ProviderName));
            lines.Add($"<color={Palette.Muted}>{_loc.Get("panel.seed." + next.SeedOrigin)}</color>");
            if (!rng.Passed)
            {
                lines.Add($"<color={Palette.Warning}>! {_loc.Format("warn.unverified", _loc.Get("warn.rng"))}</color>");
            }

            AddIssues(lines, result.Issues, IssueSeverity.Unverified);
            return lines;
        }

        private string Forecast(PredictionResult result, string? noteKey)
        {
            if (!_cfg.ExpandedLayout.Value || result.Next == null)
            {
                return string.Empty;
            }

            ForecastMode mode = _cfg.Forecast.Value;
            IReadOnlyList<AttemptForecast> rows =
                mode == ForecastMode.Progression ? result.Progression : result.RepeatedAttempts;
            string title = _loc.Format("panel.forecast." + mode, result.Next.TargetQuality);
            var lines = new List<string> { $"<b>{title}</b>" };
            foreach (AttemptForecast row in rows)
            {
                lines.Add(Row(row));
            }

            if (noteKey != null)
            {
                lines.Add($"<color={Palette.Muted}><size=85%>{_loc.Get(noteKey)}</size></color>");
            }

            return string.Join("\n", lines.ToArray());
        }

        private string Row(AttemptForecast row)
        {
            string color = Palette.For(row.Status, _cfg.ColorblindPalette.Value);
            string text = $"{row.Index + 1,2}. +{row.TargetQuality}  <color={color}>{Outcome(row)}</color>";
            if (_cfg.ShowExactRoll.Value)
            {
                text += "  " + row.Roll.Roll.ToString("0.0000", Invariant);
            }

            if (_cfg.ShowSeedTransitions.Value)
            {
                text += $"  <color={Palette.Muted}>{row.Roll.Seed} → {row.Roll.NextSeed}</color>";
            }

            return row.Conditional
                ? $"<color={Palette.Muted}><i>{text} ({_loc.Get("panel.forecast.conditional")})</i></color>"
                : text;
        }

        private string Header(ForgeSelection? selection)
        {
            if (selection == null)
            {
                return $"<b>{_loc.Get("panel.title")}</b>";
            }

            string name = global::Localization.instance.Localize(selection.Item.m_shared.m_name);
            string target = _loc.Format("panel.target", selection.TargetQuality);
            string header = $"<b>{name} +{selection.CurrentQuality}</b>\n{target}";
            if (_cfg.ShowPrefabId.Value)
            {
                string prefab = selection.Recipe.m_item.gameObject.name;
                header += $"\n<color={Palette.Muted}><size=85%>{prefab}</size></color>";
            }

            return header;
        }

        private string BreakLine(UpgradeRules rules)
        {
            return rules.Failure.Classifier == null
                ? _loc.Format("panel.break", rules.BreakChance.ToString("0.000000", Invariant))
                : $"<color={Palette.Muted}>{rules.Failure.Description}</color>";
        }

        private void AddIssues(List<string> lines, IReadOnlyList<CompatibilityIssue> issues, IssueSeverity minimum)
        {
            foreach (CompatibilityIssue issue in issues)
            {
                if (issue.Severity >= minimum)
                {
                    lines.Add($"<color={Palette.Warning}><size=85%>! {issue.Detail}</size></color>");
                }
            }
        }

        private string CompactKey(AttemptForecast next)
        {
            switch (next.Status)
            {
                case PredictionStatus.Success:
                    return _loc.Get("compact.success");
                case PredictionStatus.Destruction:
                    return _loc.Get("compact.destroyed");
                case PredictionStatus.LevelLoss:
                    return _loc.Format("compact.levelloss", next.LevelsLost);
                default:
                    return _loc.Get("compact.nochange");
            }
        }

        private string Outcome(AttemptForecast row)
        {
            switch (row.Status)
            {
                case PredictionStatus.Success:
                    return _loc.Get("outcome.success");
                case PredictionStatus.Destruction:
                    return _loc.Get("outcome.destroyed");
                case PredictionStatus.LevelLoss:
                    return _loc.Format("outcome.levelloss", row.LevelsLost);
                case PredictionStatus.NoChange:
                    return _loc.Get("outcome.nochange");
                default:
                    return _loc.Get("outcome.unknown");
            }
        }

        private string ConfidenceText(PredictionConfidence confidence) => _loc.Get("confidence." + confidence);
    }
}
