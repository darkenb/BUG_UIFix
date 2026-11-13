using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyVersion("1.1.3.0")]
[assembly: AssemblyFileVersion("1.1.3.0")]
[assembly: AssemblyTitle("BUG_UIFix")]

namespace BUG_UIFix
{
    // UI fixes for other mods (and one in the game itself: the replicator count), applied with Harmony. Each fix runs only when its mod is loaded and does not already
    // contain the fix; a missing mod or a changed member only skips that fix (logged), the others still apply.
    [BepInPlugin(Guid, "BUG_UIFix", "1.1.3")]
    [BepInDependency("bug.dsp.modspatch")]  // the logic half of several fixes here, and the BigInventory screen limit
    [BepInDependency(AllPlanetInfoFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(GalacticScaleFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(StarmapFilterFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(PlanetFinderFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MultifunctionFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LaserClearingFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(BigInventoryFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(UniverseGenTweaksFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MechaDronesTweaksFix.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "bug.dsp.uifix";
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            var harmony = new Harmony(Guid);
            Run("DSPAllPlanetInfo", () => AllPlanetInfoFix.Apply(harmony, Config));
            Run("DSPAllPlanetInfo rows", () => AllPlanetInfoRowsFix.Apply(harmony));
            Run("Galactic Scale", () => GalacticScaleFix.Apply(harmony));
            Run("UXAssist", () => StarmapFilterFix.Apply(harmony));
            Run("PlanetFinder", () => PlanetFinderFix.Apply(harmony));
            Run("Multifunction", () => MultifunctionFix.Apply(harmony));
            Run("LaserClearing", () => LaserClearingFix.Apply(harmony));
            Run("DSPBigInventory", () => BigInventoryFix.Apply(harmony));
            Run("UniverseGenTweaks", () => UniverseGenTweaksFix.Apply(harmony));
            Run("MechaDronesTweaks", () => MechaDronesTweaksFix.Apply(harmony));
            Run("New game screen", () => NewGameInputFix.Apply(harmony));
            Run("Replicator", () => ReplicatorCountFix.Apply(harmony));
            Run("Tech tree names", () => TechTitleFix.Apply(harmony));
        }

        private static void Run(string mod, Func<string> apply)
        {
            try { Log.LogInfo(mod + ": " + apply()); }
            catch (Exception error) { Log.LogError(mod + " fix skipped, the mod is left as it is: " + error); }
        }

        internal static bool Loaded(string guid) => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid);
    }
}
