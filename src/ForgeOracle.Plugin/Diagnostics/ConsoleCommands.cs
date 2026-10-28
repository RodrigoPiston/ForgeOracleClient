using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace ForgeOracle.Plugin.Diagnostics
{
    /// <summary>
    /// <c>forgeoracle &lt;subcommand&gt;</c> in the F5 console. Subcommands read game state only; the few that act
    /// (refresh, debug, reload, resetpanel, selftest) touch this plugin's own settings or the restored RNG state.
    /// </summary>
    internal static class ConsoleCommands
    {
        private static readonly string[] Subcommands =
        {
            "status", "item", "predict", "providers", "history", "selftest", "refresh", "debug", "reload", "resetpanel",
        };

        public static void Register(PluginRuntime runtime, ManualLogSource log)
        {
            new Terminal.ConsoleCommand("forgeoracle",
                "[" + string.Join("|", Subcommands) + "] Forge Oracle diagnostics",
                (Terminal.ConsoleEvent)(args => Run(runtime, args, log)),
                optionsFetcher: () => new List<string>(Subcommands));
        }

        private static void Run(PluginRuntime runtime, Terminal.ConsoleEventArgs args, ManualLogSource log)
        {
            string sub = args.Args.Length > 1 ? args.Args[1].ToLowerInvariant() : "status";
            try
            {
                foreach (string line in Execute(runtime, sub, args.Args))
                {
                    args.Context.AddString(line);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"forgeoracle {sub}: {ex}");
                args.Context.AddString("forgeoracle: " + ex.Message);
            }
        }

        private static IEnumerable<string> Execute(PluginRuntime runtime, string sub, string[] args)
        {
            var report = new StatusReport(runtime);
            switch (sub)
            {
                case "status": return report.Status();
                case "item": return report.Item();
                case "predict": return report.Predict(args.Length > 2 && int.TryParse(args[2], out int n) ? n : 5);
                case "providers": return report.Providers();
                case "history": return report.History();
                case "selftest": return SelfTest(runtime);
                case "refresh": runtime.Controller.Refresh(); return new[] { "forgeoracle: prediction refreshed" };
                case "debug": return Toggle(runtime);
                case "reload": runtime.Config.Reload(); return new[] { "forgeoracle: configuration reloaded" };
                case "resetpanel": runtime.Config.ResetPanel(); return new[] { "forgeoracle: panel position reset" };
                default: return new[] { "forgeoracle: unknown subcommand. Try: " + string.Join(", ", Subcommands) };
            }
        }

        private static IEnumerable<string> SelfTest(PluginRuntime runtime)
        {
            runtime.Rng = RngSelfTest.Run();
            runtime.Controller.Refresh();
            return new[] { "forgeoracle: RNG self-test " + runtime.Rng };
        }

        private static IEnumerable<string> Toggle(PluginRuntime runtime)
        {
            runtime.Config.DebugLogging.Value = !runtime.Config.DebugLogging.Value;
            return new[] { "forgeoracle: debug logging " + (runtime.Config.DebugLogging.Value ? "on" : "off") };
        }
    }
}
