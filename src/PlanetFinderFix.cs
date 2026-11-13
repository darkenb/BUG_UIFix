using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // PlanetFinder's resource filter buttons were a fixed list of the vanilla veins, liquids and gases on one row.
    // They now come from the loaded game: GenesisBook's extra vein types and ocean/atmosphere items, GalacticScale's themes.
    // Order: veins, gap, liquids, gap, gases; the row wraps and the planet list moves down by the extra rows.
    // The list is read again when a real game starts (the window is first built during the menu demo).
    public static class PlanetFinderFix
    {
        public const string Guid = "com.hetima.dsp.PlanetFinder";
        public const int FirstItemId = 1000;  // vein types are below, item ids (liquids, gases) from here
        static readonly int[] DefaultFluids = { 1000, 1116 }, DefaultGases = { 1120, 1121, 1011 };
        const float ButtonStep = 30f, GapWidth = 6f, RowStep = 32f, Top = 4f;

        static Type selection, window;
        static FieldInfo items, rectTrans, buttons, buttonNames, lastSelected, veinCount, itemSelection, targetItemId, windowTrans, contentTrans;
        static MethodInfo createButton, setIcon, topLeft, margin, windowSize;
        static float rowWidth;
        static List<int> plan;
        static float[] planLefts;
        static int[] planRows;
        static int planRowCount = 1;

        static FieldInfo F(Type type, string name) => AccessTools.Field(type, name).ThrowIfMissing(type.Name + "." + name);
        static MethodInfo M(Type type, string name) => AccessTools.Method(type, name).ThrowIfMissing(type.Name + "." + name);

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            if (AccessTools.TypeByName("PlanetFinderMod.ResourceList") != null) return "already contains this fix, skipped";
            selection = AccessTools.TypeByName("PlanetFinderMod.UIItemSelection").ThrowIfMissing("UIItemSelection");
            window = AccessTools.TypeByName("PlanetFinderMod.UIPlanetFinderWindow").ThrowIfMissing("UIPlanetFinderWindow");
            Type util = AccessTools.TypeByName("PlanetFinderMod.Util").ThrowIfMissing("Util");
            rowWidth = (float)F(AccessTools.TypeByName("PlanetFinderMod.PLFN"), "WINDOW_WIDTH").GetValue(null) - 56f;
            items = F(selection, "items"); rectTrans = F(selection, "rectTrans"); buttons = F(selection, "buttons");
            buttonNames = F(selection, "buttonNames"); lastSelected = F(selection, "lastSelectedItemId");
            veinCount = F(AccessTools.TypeByName("PlanetFinderMod.UIPlanetFinderListItem"), "veinCount");
            itemSelection = F(window, "itemSelection"); targetItemId = F(window, "targetItemId");
            windowTrans = F(window, "windowTrans"); contentTrans = F(window, "contentTrans");
            createButton = M(selection, "CreateSelectButton"); setIcon = M(selection, "SetButtonIcon");
            topLeft = M(util, "NormalizeRectWithTopLeft"); margin = M(util, "NormalizeRectWithMargin");
            windowSize = M(window, "WindowSize");

            harmony.Patch(M(selection, "Init"), prefix: new HarmonyMethod(typeof(PlanetFinderFix), nameof(Init)));
            harmony.Patch(M(window, "BeginGame"), prefix: new HarmonyMethod(typeof(PlanetFinderFix), nameof(BeginGame)));
            harmony.Patch(windowSize, postfix: new HarmonyMethod(typeof(PlanetFinderFix), nameof(WindowSize)));
            harmony.Patch(M(window, "_OnCreate"), postfix: new HarmonyMethod(typeof(PlanetFinderFix), nameof(PlaceListBg)));
            return "resource filter buttons follow the loaded game and wrap";
        }

        static bool IsVeinType(int id) => id < FirstItemId;

        static void EnsurePlan()
        {
            if (plan != null) return;
            var veins = new SortedSet<int>();
            var liquids = new HashSet<int>();
            var gases = new HashSet<int>();
            if (LDB.veins?.dataArray != null)
                foreach (VeinProto v in LDB.veins.dataArray)
                    if (v != null && v.ID >= 1 && v.ID < FirstItemId && LDB.items.Select(v.MiningItem) != null) veins.Add(v.ID);
            if (LDB.themes?.dataArray != null)
                foreach (ThemeProto theme in LDB.themes.dataArray)
                {
                    if (theme == null) continue;
                    if (theme.WaterItemId >= FirstItemId) liquids.Add(theme.WaterItemId);
                    if (theme.GasItems != null) foreach (int id in theme.GasItems) gases.Add(id);
                }
            if (GameMain.galaxy?.stars != null)
                foreach (StarData star in GameMain.galaxy.stars)
                {
                    if (star?.planets == null) continue;
                    foreach (PlanetData planet in star.planets)
                    {
                        if (planet == null) continue;
                        if (planet.waterItemId >= FirstItemId) liquids.Add(planet.waterItemId);
                        if (planet.gasItems != null) foreach (int id in planet.gasItems) gases.Add(id);
                    }
                }
            plan = new List<int>(veins);
            var seen = new HashSet<int>(veins);
            plan.Add(0);
            AddGroup(seen, DefaultFluids, liquids);
            plan.Add(0);
            AddGroup(seen, DefaultGases, gases);
            planRowCount = Layout(plan, rowWidth, out planLefts, out planRows);
        }

        // Defaults first in their original order, then the rest by id; a fluid that is also a gas stays with the fluids.
        static void AddGroup(HashSet<int> seen, int[] defaults, IEnumerable<int> found)
        {
            var extra = new SortedSet<int>(found.Where(id => id >= FirstItemId));
            foreach (int id in defaults.Concat(extra.Except(defaults)))
                if (LDB.items.Select(id) != null && seen.Add(id)) plan.Add(id);
        }

        // Left edge of every entry (-1 for gaps, id 0) and its row; a row wraps when the next button would pass rowWidth.
        public static int Layout(IList<int> ids, float width, out float[] lefts, out int[] rows)
        {
            lefts = new float[ids.Count]; rows = new int[ids.Count];
            float left = 0f; int row = 0; bool pendingGap = false;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == 0) { lefts[i] = -1f; rows[i] = row; pendingGap = left > 0f; continue; }
                if (pendingGap) { left += GapWidth; pendingGap = false; }
                if (left > 0f && left + ButtonStep > width) { row++; left = 0f; }
                lefts[i] = left; rows[i] = row;
                left += ButtonStep;
            }
            return row + 1;
        }

        static float ExtraHeight() { EnsurePlan(); return (planRowCount - 1) * RowStep; }

        static bool Init(object __instance)
        {
            veinCount.SetValue(null, FirstItemId - 1);  // the list rows treat "itemId <= veinCount" as a vein type
            items.SetValue(__instance, new HashSet<int>());
            rectTrans.SetValue(__instance, ((Component)__instance).transform as RectTransform);
            CreateButtons(__instance);
            return false;
        }

        static void CreateButtons(object sel)
        {
            var list = new List<UIButton>(40);
            var names = new List<string>(40);
            buttons.SetValue(sel, list);
            buttonNames.SetValue(sel, names);
            var trans = (RectTransform)rectTrans.GetValue(sel);
            EnsurePlan();
            for (int i = 0; i < plan.Count; i++)
            {
                int id = plan[i];
                if (id == 0) continue;
                Sprite sprite;
                string name;
                if (IsVeinType(id))
                {
                    VeinProto vein = LDB.veins.Select(id);
                    sprite = vein.iconSprite != null ? vein.iconSprite : LDB.items.Select(vein.MiningItem).iconSprite;
                    name = vein.name;
                }
                else
                {
                    ItemProto item = LDB.items.Select(id);
                    sprite = item.iconSprite;
                    name = item.name;
                }
                var btn = (UIButton)createButton.Invoke(null, new[] { sel });
                topLeft.Invoke(null, new object[] { btn, planLefts[i], Top + planRows[i] * RowStep, trans });
                btn.button.interactable = true;
                setIcon.Invoke(null, new object[] { btn, sprite, name, id });
                list.Add(btn);
                names.Add(name);
            }
            trans.sizeDelta = new Vector2(rowWidth, Top + planRowCount * RowStep);
        }

        static void BeginGame(object __instance)
        {
            object sel = itemSelection.GetValue(__instance);
            if (sel == null) return;
            List<int> previous = plan;
            plan = null;
            EnsurePlan();
            if (previous != null && previous.SequenceEqual(plan)) return;
            foreach (UIButton btn in (List<UIButton>)buttons.GetValue(sel)) UnityEngine.Object.Destroy(btn.gameObject);
            ((HashSet<int>)items.GetValue(sel)).Clear();
            lastSelected.SetValue(sel, 0);
            CreateButtons(sel);
            targetItemId.SetValue(__instance, 0);
            ((RectTransform)windowTrans.GetValue(__instance)).sizeDelta = (Vector2)windowSize.Invoke(__instance, null);
            PlaceListBg(__instance);
        }

        static void WindowSize(ref Vector2 __result) => __result.y += ExtraHeight();

        static void PlaceListBg(object __instance)
        {
            Image bg = ((RectTransform)contentTrans.GetValue(__instance))?.Find("list-bg")?.GetComponent<Image>();
            if (bg != null) margin.Invoke(null, new object[] { bg, 70f + ExtraHeight(), 0f, 20f, 16f, null });
        }
    }
}
