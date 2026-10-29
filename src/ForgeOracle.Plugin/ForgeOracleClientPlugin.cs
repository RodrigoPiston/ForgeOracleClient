using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using ForgeOracle.Plugin.Configuration;
using ForgeOracle.Plugin.Diagnostics;
using ForgeOracle.Plugin.Patches;
using HarmonyLib;
using UnityEngine;

namespace ForgeOracle.Plugin
{
    /// <summary>
    /// Forge Oracle Client: shows the deterministic outcome of the next Forge of Potential attempt. Client-side
    /// and read-only: no RPCs, no save, inventory, seed or probability changes, nothing required on the server.
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public sealed class ForgeOracleClientPlugin : BaseUnityPlugin
    {
        public const string Guid = "heido.valheim.forgeoracleclient";
        public const string Name = "ForgeOracleClient";
        public const string Version = "1.0.0";

        private static readonly HashSet<string> Reported = new HashSet<string>();
        private static ManualLogSource? _log;
        private Harmony? _harmony;

        internal static PluginRuntime? Runtime { get; private set; }

        /// <summary>
        /// Logs a plugin error once per site and exception type, so a broken frame cannot flood the log.
        /// </summary>
        internal static void ReportError(string site, Exception ex)
        {
            if (Reported.Add(site + ex.GetType().FullName))
            {
                _log?.LogError($"{site}: {ex}");
            }
        }

        private void Awake()
        {
            _log = Logger;
            if (Application.isBatchMode)
            {
                Logger.LogInfo("Headless (server) process: Forge Oracle is client-only and stays idle.");
                return;
            }

            var config = new PluginConfig(Config);
            RngSelfTestResult rng = config.RngSelfTest.Value ? RngSelfTest.Run() : RngSelfTestResult.Skipped;
            LogSelfTest(rng);
            Runtime = new PluginRuntime(config, rng, Guid, Logger);
            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(InventoryGuiPatches));
            _harmony.PatchAll(typeof(DoCraftingObserver));
            _harmony.PatchAll(typeof(RollObserver));
            ConsoleCommands.Register(Runtime, Logger);
            Logger.LogInfo($"{Name} {Version} loaded (game {(global::Version.CurrentVersion)}).");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Runtime?.Controller.Unload();
            Runtime = null;
        }

        private void LogSelfTest(RngSelfTestResult rng)
        {
            if (rng.Passed || !rng.Ran)
            {
                Logger.LogInfo("RNG self-test: " + rng);
            }
            else
            {
                Logger.LogError("RNG self-test FAILED, predictions are disabled: " + rng);
            }
        }
    }
}
