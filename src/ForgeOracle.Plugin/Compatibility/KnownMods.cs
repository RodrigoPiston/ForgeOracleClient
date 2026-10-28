using System;

namespace ForgeOracle.Plugin.Compatibility
{
    internal enum ModRole
    {
        /// <summary>Patches forge methods but was audited not to change the decision at the forge.</summary>
        Benign,

        /// <summary>Changes the success / break odds; handled by a rules resolver.</summary>
        ThresholdModifier,

        /// <summary>Replaces the roll itself; no exact prediction is possible.</summary>
        RollReplacer,
    }

    /// <summary>
    /// A mod whose forge patches were inspected (decompiled source, summary in docs/COMPATIBILITY.md).
    /// </summary>
    internal sealed class KnownMod
    {
        public KnownMod(string id, string name, ModRole role, string auditedVersion, string note)
        {
            Id = id;
            Name = name;
            Role = role;
            AuditedVersion = auditedVersion;
            Note = note;
        }

        /// <summary>BepInEx GUID; for these mods it is also the Harmony id.</summary>
        public string Id { get; }

        public string Name { get; }

        public ModRole Role { get; }

        public string AuditedVersion { get; }

        public string Note { get; }
    }

    internal static class KnownMods
    {
        public const string ImpactfulSkills = "MidnightsFX.ImpactfulSkills";
        public const string PotentialReforged = "Dyls123.PotentialReforged";
        public const string ForesightForge = "com.drakosdj.foresightforge";
        public const string ReforgedPotential = "akuichi.ReforgedPotential";

        public static readonly KnownMod[] All =
        {
            new KnownMod(ImpactfulSkills, "ImpactfulSkills", ModRole.ThresholdModifier, "0.23.0",
                "Forging skill and level decay adjust the idol odds during DoCrafting."),
            new KnownMod(PotentialReforged, "PotentialReforged", ModRole.ThresholdModifier, "1.1.1",
                "Transpiler replaces the success / break thresholds and the failure quality; the roll is vanilla."),
            new KnownMod(ForesightForge, "ForesightForge", ModRole.ThresholdModifier, "1.0.0",
                "TierScaled mode replaces DoCrafting but keeps the seeded roll; failures depend on the idol tier."),
            new KnownMod(ReforgedPotential, "ReforgedPotential", ModRole.RollReplacer, "2.0.8",
                "Rolls with the unseeded global Unity RNG instead of the character seed table."),
            new KnownMod("dezzycode.valheim.betrvalheim", "BetrValheim", ModRole.Benign, "5.1.5",
                "Salvage, batch crafting and extra inventory; none of it runs at the forge."),
            new KnownMod("vapok.mods.adventurebackpacks", "AdventureBackpacks", ModRole.Benign, "2.3.4",
                "Preserves backpack contents across upgrades."),
            new KnownMod("vapok.common.ItemDataManager", "AdventureBackpacks item data", ModRole.Benign, "2.3.4",
                "Carries custom item data across RemoveItem."),
            new KnownMod("com.morda.workbenchesplus", "WorkbenchesPlus", ModRole.Benign, "1.0.16",
                "Dismantle mode, which is unavailable at the forge."),
            new KnownMod("Therzie.Armory", "Armory", ModRole.Benign, "1.4.3",
                "Socket preservation hook, inactive without Jewelcrafting; never touches the roll."),
        };

        public static KnownMod? Find(string id)
        {
            foreach (KnownMod mod in All)
            {
                if (string.Equals(mod.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return mod;
                }
            }

            return null;
        }
    }
}
