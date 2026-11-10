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
            return "planet panel layout fixed";
        }

        private static void SetResCount(int count, RectTransform ___rectTrans, RectTransform ___paramGroup)
        {
            Vector2 size, position;
            Layout(count, out size.y, out position.y);
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
