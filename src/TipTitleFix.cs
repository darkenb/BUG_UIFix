using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // Item and recipe tips: a long name (Ore Enrichment's "Matter Recombinator Recycling: Advanced Buildings III") wrapped
    // onto a second line over the line below it. A name wider than its line gets a smaller font; short names keep theirs.
    public static class TipTitleFix
    {
        public static string Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(UIItemTip), nameof(UIItemTip.SetTip)).ThrowIfMissing("UIItemTip.SetTip"),
                postfix: new HarmonyMethod(typeof(TipTitleFix), nameof(Postfix)));
            return "long item and recipe names fit the tip's title line";
        }

        static readonly Dictionary<Text, int> baseSizes = new Dictionary<Text, int>();  // tips are reused, a few at most

        static void Postfix(UIItemTip __instance)
        {
            var title = __instance.nameText;
            if (title == null) return;
            if (!baseSizes.TryGetValue(title, out int size)) baseSizes[title] = size = title.fontSize;
            var settings = title.GetGenerationSettings(Vector2.zero);
            settings.fontSize = size;
            settings.scaleFactor = 1f;
            float need = title.cachedTextGeneratorForLayout.GetPreferredWidth(title.text, settings);
            title.fontSize = TechTitleFix.Size(size, title.rectTransform.rect.width, need);
        }
    }
}
