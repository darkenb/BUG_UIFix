using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BUG_UIFix
{
    // Galactic Scale replaces UIPlanetDetail.SetResCount to make room for its "Planetary Radius" row, with the offsets of an
    // older game (-90 / 210). The game now uses -106 / 226 with at least 46 for the rows, so the orbit group sat 16 units
    // inside the last resource row. This postfix runs after GS's prefix and lays out like the game, plus 20 for the radius row.
    public static class GalacticScaleFix
    {
        public const string Guid = "dsp.galactic-scale.2";

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            harmony.Patch(AccessTools.Method(typeof(UIPlanetDetail), "SetResCount"), postfix: new HarmonyMethod(typeof(GalacticScaleFix), nameof(SetResCount)) { priority = Priority.Last });
            // Our local Galactic Scale build already returns to the galaxy view itself.
            if (AccessTools.Method(AccessTools.TypeByName("GalacticScale.Modeler"), "CancelPendingScans") != null) return "planet panel layout fixed";
            Type display = AccessTools.TypeByName("GalacticScale.SystemDisplay").ThrowIfMissing("GalacticScale.SystemDisplay");
            inGalaxySelect = AccessTools.Field(display, "inGalaxySelect").ThrowIfMissing("SystemDisplay.inGalaxySelect");
            showStarCount = AccessTools.Method(display, "ShowStarCount").ThrowIfMissing("SystemDisplay.ShowStarCount");
            Type select = AccessTools.TypeByName("GalacticScale.PatchOnUIGalaxySelect").ThrowIfMissing("GalacticScale.PatchOnUIGalaxySelect");
            harmony.Patch(AccessTools.Method(select, "SetStarmapGalaxy").ThrowIfMissing("PatchOnUIGalaxySelect.SetStarmapGalaxy"),
                transpiler: new HarmonyMethod(typeof(GalacticScaleFix), nameof(AfterGalaxySet)));
            return "planet panel layout fixed; a new galaxy preview shows the galaxy view";
        }

        // Changing a setting while a star system was open on the new game screen made a new galaxy but left the system's
        // planet and star panels open over it. After GS stores the new galaxy in the map, the galaxy view is shown.
        static FieldInfo inGalaxySelect;
        static MethodInfo showStarCount;

        static IEnumerable<CodeInstruction> AfterGalaxySet(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            Func<CodeInstruction, bool> store = c => c.operand is MethodInfo m && m.DeclaringType == typeof(UIVirtualStarmap) && m.Name == "set_galaxyData";
            int at = code.FindIndex(c => store(c));
            if (at < 0 || code.FindIndex(at + 1, c => store(c)) >= 0) throw new InvalidOperationException("galaxy store in SetStarmapGalaxy not found once");
            code.Insert(at + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GalacticScaleFix), nameof(ShowGalaxyView))));
            return code;
        }

        public static void ShowGalaxyView()
        {
            if (!(bool)inGalaxySelect.GetValue(null)) return;
            UIRoot.instance.uiGame.planetDetail.gameObject.SetActive(false);
            UIRoot.instance.uiGame.starDetail.gameObject.SetActive(false);
            showStarCount.Invoke(null, null);
        }

        // The game re-applies resourcesTabHeight to the panel every frame (tabs, 0.10.35); GS's prefix never sets it, so the
        // panel and its background shrank to a black strip at the top.
        private static void SetResCount(int count, RectTransform ___rectTrans, RectTransform ___paramGroup, ref int ___resourcesTabHeight)
        {
            Vector2 size, position;
            Layout(count, out size.y, out position.y);
            ___resourcesTabHeight = (int)size.y;
            ___rectTrans.sizeDelta = new Vector2(___rectTrans.sizeDelta.x, size.y);
            ___paramGroup.anchoredPosition = new Vector2(___paramGroup.anchoredPosition.x, position.y);
        }

        // Panel height and orbit group position for `count` resource rows (pure, checked in check.cs).
        public static void Layout(int count, out float height, out float paramY)
        {
            int rows = Mathf.Max(count * 20, 46);
            height = 226 + rows + 20f;
            paramY = -106 - rows;
        }
    }
}
