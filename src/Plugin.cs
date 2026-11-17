using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyVersion("1.1.5.0")]
[assembly: AssemblyFileVersion("1.1.5.0")]
[assembly: AssemblyTitle("BUG_UIFix")]

namespace BUG_UIFix
{
    // UI fixes for other mods (and one in the game itself: the replicator count), applied with Harmony. Each fix runs only when its mod is loaded and does not already
    // contain the fix; a missing mod or a changed member only skips that fix (logged), the others still apply.
    [BepInPlugin(Guid, "BUG_UIFix", "1.1.5")]
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
            // One switch per fix, in the config file (Gale's config editor shows them under "Fixes").
            ReplicatorCountFix.CountItems = Fix("ReplicatorCountItems", "Replicator: the count above the craft button is in items for a one-product recipe (285 batches of 8 show 2280), as the queue counts.");
            ReplicatorCountFix.NoLimit = Fix("ReplicatorCountNoLimit", "Replicator: the count above the craft button shows the real most instead of stopping at >999 (BUG_ModsPatch's MAX buttons add this number either way).");
            ReplicatorCountFix.InventoryCounts = Fix("ReplicatorInventoryCounts", "Replicator: each input in the recipe tree shows how many the inventory holds, under its icon; red below one batch.");
            if (ReplicatorCountFix.CountItems || ReplicatorCountFix.NoLimit || ReplicatorCountFix.InventoryCounts) Run("Replicator", () => ReplicatorCountFix.Apply(harmony));
            if (Fix("WindowOrder", "A clicked building's window opens in front of the inventory that opens with it.")) Run("Window order", () => WindowOrderFix.Apply(harmony));
            if (Fix("TipNames", "Item and recipe tips: a name too long for its line gets a smaller font instead of running over the line below.")) Run("Tip names", () => TipTitleFix.Apply(harmony));
            if (Fix("TechTreeNames", "Tech tree: a name wider than the gap to the next node gets a smaller font.")) Run("Tech tree names", () => TechTitleFix.Apply(harmony));
            if (Fix("NewGameTypedValues", "New game screen: a text box at the end of the star count slider (and UniverseGenTweaks' sliders) to type the value.")) Run("New game screen", () => NewGameInputFix.Apply(harmony));
            if (Fix("DSPAllPlanetInfo", "DSPAllPlanetInfo: the Interstellar Logistics window stays on screen, can be dragged and folded, fits its rows and lists big stations over several pages."))
            {
                Run("DSPAllPlanetInfo", () => AllPlanetInfoFix.Apply(harmony, Config));
                Run("DSPAllPlanetInfo rows", () => AllPlanetInfoRowsFix.Apply(harmony));
            }
            if (Fix("GalacticScale", "Galactic Scale: planet panel spacing and background, the new galaxy shown after a setting change, the star count slider keeps its value.")) Run("Galactic Scale", () => GalacticScaleFix.Apply(harmony));
            if (Fix("UXAssistStarmapFilter", "UXAssist: the open starmap filter is drawn above the other starmap panels.")) Run("UXAssist", () => StarmapFilterFix.Apply(harmony));
            if (Fix("PlanetFinderButtons", "PlanetFinder: filter buttons for every vein, liquid and gas in the loaded game, wrapping onto more rows.")) Run("PlanetFinder", () => PlanetFinderFix.Apply(harmony));
            if (Fix("MultifunctionInventoryButton", "Multifunction: the inventory button shows the inventory size and the screen limit.")) Run("Multifunction", () => MultifunctionFix.Apply(harmony));
            if (Fix("LaserClearingButton", "LaserClearing: the toggle button keeps its tooltip and colour after the UI is rebuilt.")) Run("LaserClearing", () => LaserClearingFix.Apply(harmony));
            if (Fix("BigInventoryWindowsFit", "DSPBigInventory: inventory, assembler and miner windows are scaled to fit the screen.")) Run("DSPBigInventory", () => BigInventoryFix.Apply(harmony));
            if (Fix("UniverseGenTweaksRows", "UniverseGenTweaks: new game setting rows are stacked without overlapping.")) Run("UniverseGenTweaks", () => UniverseGenTweaksFix.Apply(harmony));
            if (Fix("MechaDronesTweaksSpeed", "MechaDronesTweaks: the mecha window and tech tree show the modded drone speed.")) Run("MechaDronesTweaks", () => MechaDronesTweaksFix.Apply(harmony));
        }

        // The fixes patch the game while it loads, so a changed switch takes effect after restarting the game.
        private bool Fix(string key, string description)
        {
            bool on = Config.Bind("Fixes", key, true, description + " Takes effect after restarting the game.").Value;
            if (!on) Log.LogInfo(key + ": off in the config");
            return on;
        }

        private static void Run(string mod, Func<string> apply)
        {
            try { Log.LogInfo(mod + ": " + apply()); }
            catch (Exception error) { Log.LogError(mod + " fix skipped, the mod is left as it is: " + error); }
        }

        internal static bool Loaded(string guid) => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid);

        // "Already fixed?" lookups: unlike AccessTools these write no "Could not find" warning when the member is absent,
        // which is the normal case.
        internal static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }

        internal static bool HasMember(Type type, string name) => type != null && type.GetMember(name, AccessTools.all).Length > 0;
    }
}
