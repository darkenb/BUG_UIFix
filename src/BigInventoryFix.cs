using HarmonyLib;

namespace BUG_UIFix
{
    // DSPBigInventory: a large inventory (with the delivery panel) made the inventory, assembler and miner windows taller or
    // wider than the screen. Those windows are scaled down and kept on screen (ViewportFit); the inventory size itself
    // (fitted to the screen first) is in BUG-ModsPatch.
    public static class BigInventoryFix
    {
        public const string Guid = "Appun.DSP.plugin.BigInventory";

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            if (AccessTools.TypeByName("DSPBigInventory.ViewportFit") != null) return "already contains this fix, skipped";
            harmony.Patch(AccessTools.Method(typeof(UIInventoryWindow), "_OnOpen"), postfix: new HarmonyMethod(typeof(BigInventoryFix), nameof(Inventory)));
            harmony.Patch(AccessTools.Method(typeof(UIAssemblerWindow), "_OnOpen"), postfix: new HarmonyMethod(typeof(BigInventoryFix), nameof(Assembler)));
            harmony.Patch(AccessTools.Method(typeof(UIMinerWindow), "_OnOpen"), postfix: new HarmonyMethod(typeof(BigInventoryFix), nameof(Miner)));
            return "inventory, assembler and miner windows fit the screen";
        }

        static void Inventory(UIInventoryWindow __instance) => ViewportFit.Attach(__instance.windowTrans);
        static void Assembler(UIAssemblerWindow __instance) => ViewportFit.Attach(__instance.windowTrans);
        static void Miner(UIMinerWindow __instance) => ViewportFit.Attach(__instance.windowTrans);
    }
}
