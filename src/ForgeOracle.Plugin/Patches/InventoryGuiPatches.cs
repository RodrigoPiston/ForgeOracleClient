using System;
using HarmonyLib;

namespace ForgeOracle.Plugin.Patches
{
    /// <summary>
    /// UI integration. Postfixes never change arguments, return values or exceptions of the game methods, and
    /// every exception of ours is caught and logged here so the forge keeps working if the plugin fails.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatches
    {
        // Last, so the prediction line goes above lines other mods prepend to the description (ImpactfulSkills).
        [HarmonyPostfix]
        [HarmonyPatch("UpdateRecipe")]
        [HarmonyPriority(Priority.Last)]
        private static void UpdateRecipePostfix(InventoryGui __instance, Player player)
        {
            try
            {
                ForgeOracleClientPlugin.Runtime?.Controller.OnUpdateRecipe(__instance, player);
            }
            catch (Exception ex)
            {
                ForgeOracleClientPlugin.ReportError("UpdateRecipe", ex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void HidePostfix()
        {
            try
            {
                ForgeOracleClientPlugin.Runtime?.Controller.Hide();
            }
            catch (Exception ex)
            {
                ForgeOracleClientPlugin.ReportError("Hide", ex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPostfix(InventoryGui __instance)
        {
            try
            {
                ForgeOracleClientPlugin.Runtime?.Controller.OnGuiDestroyed(__instance);
            }
            catch (Exception ex)
            {
                ForgeOracleClientPlugin.ReportError("OnDestroy", ex);
            }
        }

        /// <summary>
        /// Optional local precaution (ConfirmPredictedDestruction, off by default): the first click on an attempt
        /// predicted to destroy the item is held back with a warning; a second click within a few seconds goes
        /// through unchanged. Nothing is modified, and any error lets the click through.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("OnCraftPressed")]
        private static bool OnCraftPressedPrefix(InventoryGui __instance)
        {
            try
            {
                return ForgeOracleClientPlugin.Runtime?.Confirmation.ShouldBlock(__instance) != true;
            }
            catch (Exception ex)
            {
                ForgeOracleClientPlugin.ReportError("OnCraftPressed", ex);
                return true;
            }
        }
    }
}
