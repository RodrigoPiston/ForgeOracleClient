using System;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>
    /// Mod providers whose predictions were compared with real in-game attempts (see docs/VALIDATION.md).
    /// A provider that is only audited from decompiled code stays UNVERIFIED until it is listed here.
    /// </summary>
    internal static class LiveValidation
    {
        // ImpactfulSkills 0.23.0: 5 of 5 real attempts matched roll, next seed and outcome on 2026-10-28
        // (2 on a dedicated server, 3 in a local world; success, name-derived seed and destruction cases).
        private static readonly string[] Validated = { KnownMods.ImpactfulSkills };

        public static bool IsValidated(string pluginGuid)
        {
            return Array.Exists(Validated, id => string.Equals(id, pluginGuid, StringComparison.OrdinalIgnoreCase));
        }
    }
}
