using System.Collections.Generic;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>Raw facts about the running game: foreign patches on the forge methods and loaded plugins.</summary>
    internal sealed class ForgeScan
    {
        public ForgeScan(IReadOnlyList<ForeignPatch> rollPatches, IReadOnlyList<ForeignPatch> craftPatches)
        {
            RollPatches = rollPatches;
            CraftPatches = craftPatches;
        }

        /// <summary>Patches by other mods on <c>Player.GetItemUpgradeChance</c>.</summary>
        public IReadOnlyList<ForeignPatch> RollPatches { get; }

        /// <summary>Patches by other mods on <c>InventoryGui.DoCrafting</c>.</summary>
        public IReadOnlyList<ForeignPatch> CraftPatches { get; }

        public static ForgeScan Capture(string ownHarmonyId)
        {
            return new ForgeScan(
                PatchInspector.Foreign(PatchInspector.Roll, ownHarmonyId),
                PatchInspector.Foreign(PatchInspector.Craft, ownHarmonyId));
        }

        public bool IsLoaded(string guid) => ModReflection.IsLoaded(guid);

        public bool HasCraftPatch(string owner, PatchKind kind)
        {
            foreach (ForeignPatch patch in CraftPatches)
            {
                if (patch.Kind == kind && string.Equals(patch.Owner, owner, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
