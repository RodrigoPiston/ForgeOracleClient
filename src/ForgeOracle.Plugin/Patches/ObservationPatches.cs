using System;
using HarmonyLib;

namespace ForgeOracle.Plugin.Patches
{
    /// <summary>
    /// Observes real attempts for the validation log and to refresh the prediction. The DoCrafting prefix returns
    /// void (it can never skip the original) and runs before other prefixes so it sees the untouched state; it only
    /// reads. The roll is taken from the postfix of the game's own <c>GetItemUpgradeChance</c> call.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    internal static class DoCraftingObserver
    {
        [HarmonyPrefix]
        [HarmonyPriority(1000)]
        private static void Prefix(InventoryGui __instance, Player player)
        {
            try
            {
                ForgeOracleClientPlugin.Runtime?.BeginAttempt(__instance, player);
            }
            catch (Exception ex)
            {
                ForgeOracleClientPlugin.ReportError("DoCrafting prefix", ex);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(Player player)
        {
            try
            {
                ForgeOracleClientPlugin.Runtime?.EndAttempt(player);
            }
            catch (Exception ex)
            {
                ForgeOracleClientPlugin.ReportError("DoCrafting postfix", ex);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetItemUpgradeChance))]
    internal static class RollObserver
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, int hash, float __result)
        {
            try
            {
                ForgeOracleClientPlugin.Runtime?.ObserveRoll(__instance, hash, __result);
            }
            catch (Exception ex)
            {
                ForgeOracleClientPlugin.ReportError("GetItemUpgradeChance postfix", ex);
            }
        }
    }
}
