using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // The tech tree zoomed out: the name above each node runs into the next node's name when it is long (They Come From
    // Void's "Meta driver mounting point extension ( 3 )"). A name wider than the gap to its neighbour on the row gets a
    // smaller font; short names keep theirs. The game rescales the name every frame, so the font is set again only when
    // the name or that scale changes.
    public static class TechTitleFix
    {
        public static string Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(UITechNode), nameof(UITechNode.UpdateLayoutDynamic)).ThrowIfMissing("UITechNode.UpdateLayoutDynamic"),
                postfix: new HarmonyMethod(typeof(TechTitleFix), nameof(Postfix)));
            return "long tech names fit the gap to the next node";
        }

        const float Gap = 14f;  // tree units kept clear before the next name

        class State { public float Room = -1f; public int BaseSize; public string Text; public float Scale; }
        static readonly Dictionary<UITechNode, State> states = new Dictionary<UITechNode, State>();

        static void Postfix(UITechNode __instance)
        {
            var title = __instance.titleText2;
            if (title == null || !title.enabled) return;
            if (!states.TryGetValue(__instance, out var s))
            {
                if (states.Count > 4000) states.Clear();  // trees rebuilt many times: forget the old nodes
                states[__instance] = s = new State { BaseSize = title.fontSize };
            }
            float scale = title.rectTransform.localScale.x;
            if (s.Text == title.text && Mathf.Abs(s.Scale - scale) < 0.01f) return;
            if (s.Room < 0f) s.Room = Spacing(__instance);
            s.Text = title.text;
            s.Scale = scale;
            if (s.Room <= 0f || scale <= 0f) return;
            var settings = title.GetGenerationSettings(Vector2.zero);
            settings.fontSize = s.BaseSize;
            settings.scaleFactor = 1f;
            float need = title.cachedTextGeneratorForLayout.GetPreferredWidth(title.text, settings);
            int size = Size(s.BaseSize, (s.Room - Gap) / scale, need);
            if (title.fontSize != size) title.fontSize = size;
        }

        // Distance to the nearest node on the same row: to the right, or to the left for the last one; 0 when alone.
        static float Spacing(UITechNode node)
        {
            var me = node.transform as RectTransform;
            var parent = me?.parent;
            if (parent == null) return 0f;
            float right = float.MaxValue, left = float.MaxValue;
            Vector2 p = me.anchoredPosition;
            for (int i = 0; i < parent.childCount; i++)
            {
                var other = parent.GetChild(i) as RectTransform;
                if (other == null || other == me || !other.gameObject.activeSelf || other.GetComponent<UITechNode>() == null) continue;
                Vector2 q = other.anchoredPosition;
                if (Mathf.Abs(q.y - p.y) > 1f) continue;
                float dx = q.x - p.x;
                if (dx > 1f) right = Mathf.Min(right, dx);
                else if (dx < -1f) left = Mathf.Min(left, -dx);
            }
            return right < float.MaxValue ? right : left < float.MaxValue ? left : 0f;
        }

        public static int Size(int baseSize, float room, float need) => need > room && need > 0 && room > 0 ? System.Math.Max(8, (int)System.Math.Floor(baseSize * room / need)) : baseSize;
    }
}
