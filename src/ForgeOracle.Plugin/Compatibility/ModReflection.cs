using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;

namespace ForgeOracle.Plugin.Compatibility
{
    /// <summary>Small helpers for reading other mods' state without a compile-time dependency.</summary>
    internal static class ModReflection
    {
        public static bool IsLoaded(string guid) => Chainloader.PluginInfos.ContainsKey(guid);

        public static string Version(string guid)
        {
            return Chainloader.PluginInfos.TryGetValue(guid, out BepInEx.PluginInfo info)
                ? info.Metadata.Version.ToString()
                : string.Empty;
        }

        /// <summary>Reads <c>ConfigEntry&lt;T&gt;.Value</c> from a static field, or returns the fallback.</summary>
        public static T StaticConfigValue<T>(Type? owner, string field, T fallback)
        {
            if (owner == null)
            {
                return fallback;
            }

            FieldInfo? info = AccessTools.Field(owner, field);
            return info?.GetValue(null) is ConfigEntry<T> entry ? entry.Value : fallback;
        }

        public static MethodInfo? StaticMethod(Type? owner, string name)
        {
            return owner == null ? null : AccessTools.Method(owner, name);
        }

        /// <summary>Invokes a static method and unwraps reflection exceptions so logs show the real cause.</summary>
        public static object Invoke(MethodInfo method, params object?[] args)
        {
            try
            {
                return method.Invoke(null, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                string name = $"{method.DeclaringType?.Name}.{method.Name}";
                throw new InvalidOperationException(name + " failed", ex.InnerException);
            }
        }
    }
}
