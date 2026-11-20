using System;
using HarmonyLib;
using UnityEngine;

namespace BUG_UIFix
{
    // Rebind Build Bar: its reset button sits at a fixed spot above the left end of the build bar, on top of the first
    // slot's item count. It is moved left until it clears every slot button, whatever the slot layout.
    public static class RebindBuildBarFix
    {
        public const string Guid = "org.kremnev8.plugin.RebindBuildBar";
        const string ButtonName = "buildmenu-button";  // the mod's prefab, cloned into the build menu's child group
        public const float Gap = 6f;

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            harmony.Patch(AccessTools.Method(typeof(UIBuildMenu), "_OnCreate").ThrowIfMissing("UIBuildMenu._OnCreate"),
                postfix: new HarmonyMethod(typeof(RebindBuildBarFix), nameof(AfterCreate)) { after = new[] { Guid } });
            return "reset button moved clear of the first slot";
        }

        static void AfterCreate(UIBuildMenu __instance)
        {
            try
            {
                Transform group = __instance.childGroup.transform;
                RectTransform reset = null;
                foreach (Transform child in group)
                    if (child.name.StartsWith(ButtonName, StringComparison.Ordinal)) reset = child as RectTransform;
                if (reset == null) { Plugin.Log.LogWarning("Rebind Build Bar: reset button not found, left as it is"); return; }

                Rect resetRect = Bounds(group, reset, false);
                float shift = 0f;
                for (int pass = 0; pass < 2; pass++)  // a second pass in case the first move lands on another button
                    foreach (UIButton button in __instance.childButtons)
                        if (button != null && button.transform.parent == group)
                        {
                            float dx = ShiftLeft(resetRect, Bounds(group, (RectTransform)button.transform, true), Gap);
                            resetRect.x += dx;
                            shift += dx;
                        }
                if (shift == 0f) return;
                reset.anchoredPosition += new Vector2(shift, 0f);
                Plugin.Log.LogInfo("Rebind Build Bar: reset button moved " + (-shift).ToString("0") + " units left");
            }
            catch (Exception error) { Plugin.Log.LogError("Rebind Build Bar reset button left as it is: " + error); }
        }

        // How far (<= 0) to move `moving` left so it ends Gap left of `fixedRect`, when they share rows and are closer than that.
        public static float ShiftLeft(Rect moving, Rect fixedRect, float gap)
        {
            bool sameRows = moving.yMin < fixedRect.yMax && moving.yMax > fixedRect.yMin;
            if (!sameRows || moving.xMin >= fixedRect.xMax || moving.xMax <= fixedRect.xMin - gap) return 0f;
            return fixedRect.xMin - gap - moving.xMax;
        }

        // A rect in the group's space; with children, it includes all of them (the count text pokes out). Hidden ones
        // count too: the slots are mostly inactive when the menu is created, and a slot shown later must not be covered.
        static Rect Bounds(Transform group, RectTransform rect, bool children)
        {
            var corners = new Vector3[4];
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (RectTransform r in children ? rect.GetComponentsInChildren<RectTransform>(true) : new[] { rect })
            {
                r.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 p = group.InverseTransformPoint(corner);
                    xMin = Math.Min(xMin, p.x); xMax = Math.Max(xMax, p.x);
                    yMin = Math.Min(yMin, p.y); yMax = Math.Max(yMax, p.y);
                }
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
