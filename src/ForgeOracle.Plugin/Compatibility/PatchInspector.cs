using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ForgeOracle.Plugin.Compatibility
{
    internal enum PatchKind
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer,
    }

    internal sealed class ForeignPatch
    {
        public ForeignPatch(string owner, PatchKind kind, string method)
        {
            Owner = owner;
            Kind = kind;
            Method = method;
        }

        public string Owner { get; }

        public PatchKind Kind { get; }

        public string Method { get; }

        /// <summary>A prefix or transpiler can change the decision; postfixes and finalizers run after it.</summary>
        public bool CanChangeDecision => Kind == PatchKind.Prefix || Kind == PatchKind.Transpiler;

        public override string ToString() => $"{Owner} {Kind} on {Method}";
    }

    /// <summary>Lists the Harmony patches other mods applied to the forge methods.</summary>
    internal static class PatchInspector
    {
        public static readonly MethodBase Roll =
            AccessTools.Method(typeof(Player), nameof(Player.GetItemUpgradeChance));
        public static readonly MethodBase Craft = AccessTools.Method(typeof(InventoryGui), "DoCrafting");

        public static List<ForeignPatch> Foreign(MethodBase method, string ownHarmonyId)
        {
            var result = new List<ForeignPatch>();
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(method);
            if (info == null)
            {
                return result;
            }

            string name = method.DeclaringType?.Name + "." + method.Name;
            Add(result, info.Prefixes, PatchKind.Prefix, name, ownHarmonyId);
            Add(result, info.Transpilers, PatchKind.Transpiler, name, ownHarmonyId);
            Add(result, info.Postfixes, PatchKind.Postfix, name, ownHarmonyId);
            Add(result, info.Finalizers, PatchKind.Finalizer, name, ownHarmonyId);
            return result;
        }

        private static void Add(List<ForeignPatch> result, IEnumerable<Patch> patches, PatchKind kind, string method,
            string ownHarmonyId)
        {
            foreach (Patch patch in patches)
            {
                if (patch.owner != ownHarmonyId)
                {
                    result.Add(new ForeignPatch(patch.owner, kind, method));
                }
            }
        }
    }
}
