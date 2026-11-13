using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BUG_UIFix
{
    // UniverseGenTweaks adds four slider rows to the new game screen and moves the rows below once, by a fixed 144.
    // GenesisBook and DSP_Battle add/move rows of their own, so rows ended up on top of each other, and the Dark Fog detail
    // button (a sibling, not a child, of its row) stayed behind. Every frame all visible setting rows are stacked from the
    // star count row down, by their real rect edges, at least 10 units apart; hidden rows take no space. From our local 1.2.14.
    public static class UniverseGenTweaksFix
    {
        public const string Guid = "org.soardev.universegentweaks";
        static readonly string[] UgtRows = { "min-dist", "min-step", "max-step", "flatten" };

        static RectTransform starCountRow;
        static Transform settingsParent, darkFogRow, detailButton;
        static Vector3 rowOrigin, detailOffset;
        static Transform[] rows;

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            if (AccessTools.Field(AccessTools.TypeByName("UniverseGenTweaks.MoreSettings"), "_settingRows") != null) return "already contains this fix, skipped";
            harmony.Patch(AccessTools.Method(typeof(UIGalaxySelect), "_OnCreate"), prefix: new HarmonyMethod(typeof(UniverseGenTweaksFix), nameof(BeforeCreate)) { priority = Priority.First });
            harmony.Patch(AccessTools.Method(typeof(UIGalaxySelect), "_OnOpen"), postfix: new HarmonyMethod(typeof(UniverseGenTweaksFix), nameof(Opened)) { priority = Priority.Last });
            harmony.Patch(AccessTools.Method(typeof(UIGalaxySelect), "_OnUpdate"), postfix: new HarmonyMethod(typeof(UniverseGenTweaksFix), nameof(Update)) { priority = Priority.Last });
            return "new game setting rows no longer overlap";
        }

        // Before UGT's own _OnCreate postfix moves anything.
        static void BeforeCreate(UIGalaxySelect __instance)
        {
            starCountRow = (RectTransform)__instance.starCountSlider.transform.parent;
            settingsParent = starCountRow.parent;
            rowOrigin = starCountRow.localPosition;
            darkFogRow = __instance.darkFogToggle.transform.parent;
            detailButton = __instance.darkFogDetailBtn.transform;
            detailOffset = darkFogRow.InverseTransformPoint(detailButton.position);
        }

        // Collected on every open: other mods create their rows late.
        static void Opened(UIGalaxySelect __instance)
        {
            if (settingsParent == null) return;
            var list = new List<Transform>();
            foreach (string name in UgtRows)
            {
                Transform row = settingsParent.Find(name);
                if (row != null) list.Add(row);
            }
            list.Add(__instance.resourceMultiplierSlider.transform.parent);
            list.Add(__instance.sandboxToggle.transform.parent);
            foreach (Transform child in settingsParent) if (child.name == "fast-start-mode") list.Add(child);  // GenesisBook and DSP_Battle
            list.Add(darkFogRow);
            foreach (Transform child in settingsParent) if (child.name == "void-invasion-toggle") list.Add(child);
            list.Add(__instance.addrText.transform.parent);
            list.Add(__instance.propertyMultiplierText.transform);
            rows = list.ToArray();
            Update();
        }

        static void Update()
        {
            if (rows == null || starCountRow == null || !starCountRow.gameObject.activeSelf) return;
            var top = new float[rows.Length];
            var bottom = new float[rows.Length];
            var visible = new bool[rows.Length];
            var y = new float[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i] as RectTransform;
                visible[i] = row != null && row.gameObject.activeSelf;
                if (!visible[i]) continue;
                top[i] = row.rect.yMax;
                bottom[i] = row.rect.yMin;
            }
            Layout(rowOrigin.y, starCountRow.rect.yMin, top, bottom, visible, y);
            for (int i = 0; i < rows.Length; i++)
                if (visible[i]) rows[i].position = settingsParent.TransformPoint(new Vector3(rowOrigin.x, y[i], rowOrigin.z));
            if (detailButton != null && darkFogRow != null && !detailButton.IsChildOf(darkFogRow))
                detailButton.position = darkFogRow.TransformPoint(detailOffset);
        }

        // Each visible row goes at least 36 below the previous one, and its top at least 10 below the previous bottom.
        public static void Layout(float originY, float originBottom, float[] top, float[] bottom, bool[] visible, float[] positions)
        {
            float previousY = originY, previousBottom = originBottom;
            for (int i = 0; i < top.Length; i++)
            {
                if (!visible[i]) continue;
                float y = Mathf.Min(previousY - 36f, previousY + previousBottom - 10f - top[i]);
                positions[i] = y;
                previousY = y;
                previousBottom = bottom[i];
            }
        }
    }
}
