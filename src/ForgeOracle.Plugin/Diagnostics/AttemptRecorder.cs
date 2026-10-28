using System.Collections.Generic;
using BepInEx.Logging;
using ForgeOracle.Core.Abstractions;
using ForgeOracle.Core.Prediction;
using ForgeOracle.Plugin.Game;
using ForgeOracle.Plugin.Prediction;

namespace ForgeOracle.Plugin.Diagnostics
{
    /// <summary>
    /// Compares each real attempt with the prediction made just before it. Everything here is read-only: the item
    /// counts come from <c>Inventory.CountItems</c>, the roll from the postfix of the game's own call.
    /// </summary>
    internal sealed class AttemptRecorder
    {
        private const int Capacity = 50;
        private readonly LinkedList<AttemptObservation> _history = new LinkedList<AttemptObservation>();
        private readonly ManualLogSource _log;
        private PendingAttempt? _pending;

        public AttemptRecorder(ManualLogSource log)
        {
            _log = log;
        }

        public IEnumerable<AttemptObservation> History => _history;

        public int Total { get; private set; }

        public int Matched { get; private set; }

        /// <summary>DoCrafting prefix (runs first): snapshot the prediction and the item counts.</summary>
        public void Begin(InventoryGui gui, Player player, PredictionPipeline pipeline)
        {
            _pending = null;
            if (ForgeSelection.FromCraft(gui, player, out ForgeSelection? selection) != SelectionState.Ready
                || selection == null)
            {
                return;
            }

            PredictionResult predicted = pipeline.Predict(selection, 1);
            var observation = new AttemptObservation(selection.Recipe.m_item.gameObject.name,
                selection.CurrentQuality, predicted);
            _pending = new PendingAttempt(selection, observation, CountByQuality(player, selection));
        }

        /// <summary>GetItemUpgradeChance postfix: the actual roll and the seed the game stored.</summary>
        public void OnRoll(Player player, int hash, float roll)
        {
            if (_pending == null || !ReferenceEquals(player, _pending.Selection.Player))
            {
                return;
            }

            AttemptObservation observation = _pending.Observation;
            observation.ActualRoll = roll;
            observation.ActualNextSeed = LiveSeeds.TryGet(player, hash, out int next) ? next : (int?)null;
            if (_pending.Selection.Idol != null)
            {
                observation.SuccessAtRoll = _pending.Selection.Idol.m_itemData.m_shared.m_upgradeChance;
                observation.BreakAtRoll = _pending.Selection.Idol.m_itemData.m_shared.m_breakChance;
            }
        }

        /// <summary>DoCrafting postfix: classify what happened to the item and log the comparison.</summary>
        public void End(Player player)
        {
            PendingAttempt? pending = _pending;
            _pending = null;
            if (pending == null || pending.Observation.ActualRoll == null)
            {
                return;
            }

            Classify(pending, CountByQuality(player, pending.Selection));
            Store(pending.Observation);
        }

        private static void Classify(PendingAttempt pending, int[] after)
        {
            int current = pending.Selection.CurrentQuality;
            for (int quality = after.Length - 1; quality >= 0; quality--)
            {
                int gained = after[quality] - pending.CountsBefore[quality] + (quality == current ? 1 : 0);
                if (gained > 0)
                {
                    pending.Observation.ObservedQuality = quality;
                    pending.Observation.Observed = quality > current ? PredictionStatus.Success
                        : quality == current ? PredictionStatus.NoChange : PredictionStatus.LevelLoss;
                    return;
                }
            }

            pending.Observation.Observed = PredictionStatus.Destruction;
        }

        private void Store(AttemptObservation observation)
        {
            Total++;
            Matched += observation.Matches ? 1 : 0;
            _history.AddFirst(observation);
            if (_history.Count > Capacity)
            {
                _history.RemoveLast();
            }

            string line = "[Validation] " + observation;
            if (observation.Predicted.IsKnown && !observation.Matches)
            {
                _log.LogWarning(line);
            }
            else
            {
                _log.LogInfo(line);
            }
        }

        /// <summary>How many items share the upgraded item's name, per quality 0..target.</summary>
        private static int[] CountByQuality(Player player, ForgeSelection selection)
        {
            var counts = new int[selection.TargetQuality + 1];
            string name = selection.Item.m_shared.m_name;
            for (int quality = 0; quality < counts.Length; quality++)
            {
                counts[quality] = player.GetInventory().CountItems(name, quality, matchWorldLevel: false);
            }

            return counts;
        }

        private sealed class PendingAttempt
        {
            public PendingAttempt(ForgeSelection selection, AttemptObservation observation, int[] countsBefore)
            {
                Selection = selection;
                Observation = observation;
                CountsBefore = countsBefore;
            }

            public ForgeSelection Selection { get; }

            public AttemptObservation Observation { get; }

            public int[] CountsBefore { get; }
        }
    }
}
