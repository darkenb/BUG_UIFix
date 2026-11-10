using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyTitle("BUG_UIFix")]

namespace BUG_UIFix
{
    // UI fixes for other mods, applied with Harmony. Each fix runs only when its mod is loaded and does not already
    // contain the fix; a missing mod or a changed member only skips that fix (logged), the others still apply.
    [BepInPlugin(Guid, "BUG_UIFix", "1.0.0")]
    [BepInDependency(AllPlanetInfoFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(GalacticScaleFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(StarmapFilterFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "bug.dsp.uifix";
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            var harmony = new Harmony(Guid);
            Run("DSPAllPlanetInfo", () => AllPlanetInfoFix.Apply(harmony, Config));
            Run("Galactic Scale", () => GalacticScaleFix.Apply(harmony));
            Run("UXAssist", () => StarmapFilterFix.Apply(harmony));
        }

        private static void Run(string mod, Func<string> apply)
        {
            try { Log.LogInfo(mod + ": " + apply()); }
            catch (Exception error) { Log.LogError(mod + " fix skipped, the mod is left as it is: " + error); }
        }

        internal static bool Loaded(string guid) => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid);
    }
}
