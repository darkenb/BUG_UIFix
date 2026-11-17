using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BUG_UIFix
{
    // Clicking a building, belt, sorter or station opens the inventory with its window, and the inventory puts itself on top
    // when it opens (UIInventoryWindow._OnOpen: SetAsLastSibling). A large inventory (DSPBigInventory) covered the clicked
    // window. Whenever either opens, the open building windows are drawn above the inventory again, in their own order.
    public static class WindowOrderFix
    {
        public static string Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ManualBehaviour), nameof(ManualBehaviour._Open)).ThrowIfMissing("ManualBehaviour._Open"),
                postfix: new HarmonyMethod(typeof(WindowOrderFix), nameof(Postfix)));
            return "a clicked building's window stays in front of the inventory";
        }

        // The windows the game opens for a clicked object (UIGame.ShutAllFunctionWindow, without the mecha, replicator,
        // statistics, control panel and blueprint windows, which are not opened by a click).
        static ManualBehaviour[] Windows(UIGame g) => new ManualBehaviour[]
        {
            g.storageWindow, g.tankWindow, g.minerWindow, g.assemblerWindow, g.fractionatorWindow, g.ejectorWindow, g.siloWindow,
            g.labWindow, g.markerWindow, g.inserterWindow, g.splitterWindow, g.beltWindow, g.accumulatorWindow, g.nodeWindow,
            g.generatorWindow, g.exchangerWindow, g.stationWindow, g.dispenserWindow, g.monitorWindow, g.spraycoaterWindow,
            g.turretWindow, g.fieldGeneratorWindow, g.battleBaseWindow, g.communicatorWindow,
        };

        static void Postfix(ManualBehaviour __instance)
        {
            var game = UIRoot.instance?.uiGame;
            var inventory = game?.inventoryWindow;
            if (inventory == null || !inventory.active) return;
            var windows = Windows(game);
            if (__instance != inventory && System.Array.IndexOf(windows, __instance) < 0) return;
            // Highest first: each moves to just above the inventory, so the lower ones end up below it and the order stays.
            foreach (var w in windows.Where(w => w != null && w.active).OrderByDescending(w => w.transform.GetSiblingIndex()))
                Raise(w.transform, inventory.transform);
        }

        static bool logged;

        // The window, or its ancestor next to the inventory's (when they sit in different groups), goes just above the inventory.
        static void Raise(Transform window, Transform inventory)
        {
            for (var a = window; a != null; a = a.parent)
                for (var b = inventory; b != null; b = b.parent)
                {
                    if (a.parent == null || a.parent != b.parent) continue;
                    if (a == b) return;
                    int index = NewIndex(a.GetSiblingIndex(), b.GetSiblingIndex());
                    if (index < 0) return;
                    if (!logged)
                    {
                        logged = true;
                        Plugin.Log.LogInfo("Window order: " + a.name + " moved above " + b.name + " in " + a.parent.name);
                    }
                    a.SetSiblingIndex(index);
                    return;
                }
        }

        // Unity draws later siblings on top. A window below the inventory takes the inventory's index (the inventory moves
        // down one); one already above it stays (-1).
        public static int NewIndex(int window, int inventory) => window < inventory ? inventory : -1;
    }
}
