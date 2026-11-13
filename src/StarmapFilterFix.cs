using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // UXAssist's starmap filter (">>" / "X", top left) was drawn under other panels of the starmap (the game's Planet Shield
    // Details, and blue frames of other mods); the planet list was unreadable. While the filter is open its panel gets a
    // canvas sorted above every other canvas of the game UI, with an opaque backdrop behind the list. Closing restores it.
    internal static class StarmapFilterFix
    {
        public const string Guid = "org.soardev.uxassist";

        private static Component toggler;
        private static PropertyInfo checkedProperty;
        private static GameObject backdrop;
        private static Canvas canvas;

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            Type filter = AccessTools.TypeByName("UXAssist.Functions.UI.StarmapFilterUI") ?? throw new MissingMemberException("UXAssist.Functions.UI.StarmapFilterUI");
            if (AccessTools.Field(filter, "_shieldSiblingIndex") != null) return "already contains this fix, skipped";
            AccessTools.Field(filter, "StarmapFilterToggler").ThrowIfMissing("StarmapFilterUI.StarmapFilterToggler");
            harmony.Patch(AccessTools.Method(filter, "InitStarmapButtons").ThrowIfMissing("StarmapFilterUI.InitStarmapButtons"), postfix: new HarmonyMethod(typeof(StarmapFilterFix), nameof(AfterInit)));
            // The filter can stay open when the starmap closes: the shield panel goes back then, and behind again on the next open.
            harmony.Patch(AccessTools.Method(typeof(UIStarmap), "_OnOpen").ThrowIfMissing("UIStarmap._OnOpen"), postfix: new HarmonyMethod(typeof(StarmapFilterFix), nameof(MapOpened)));
            harmony.Patch(AccessTools.Method(typeof(UIStarmap), "_OnClose").ThrowIfMissing("UIStarmap._OnClose"), postfix: new HarmonyMethod(typeof(StarmapFilterFix), nameof(MapClosed)));
            return "starmap filter drawn above the other starmap panels";
        }

        private static void AfterInit()
        {
            try
            {
                Type filter = AccessTools.TypeByName("UXAssist.Functions.UI.StarmapFilterUI");
                toggler = (Component)AccessTools.Field(filter, "StarmapFilterToggler").GetValue(null);
                if (toggler == null) return;
                checkedProperty = AccessTools.Property(toggler.GetType(), "Checked").ThrowIfMissing("MyCheckButton.Checked");
                EventInfo onChecked = toggler.GetType().GetEvent("OnChecked").ThrowIfMissing("MyCheckButton.OnChecked");

                // Behind the planet-type list: 24 below the icon row, 150 wide buttons at x 24, rows down to 402.
                var rect = (RectTransform)new GameObject("bug-uifix-filter-bg", typeof(RectTransform)).transform;
                rect.SetParent(toggler.transform.parent, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition3D = new Vector3(0f, -24f, 0f);
                rect.sizeDelta = new Vector2(176f, 382f);
                rect.SetAsFirstSibling();
                Image image = rect.gameObject.AddComponent<Image>();
                image.color = new Color(0.02f, 0.05f, 0.09f, 0.96f);
                image.raycastTarget = true;   // clicks between the buttons stay in the filter
                backdrop = rect.gameObject;
                backdrop.SetActive(false);

                // A canvas of its own sorts above panels that sit later in the hierarchy or have their own canvas; it needs its own raycaster.
                GameObject panel = toggler.transform.parent.gameObject;
                canvas = panel.GetComponent<Canvas>() ?? panel.AddComponent<Canvas>();
                if (panel.GetComponent<GraphicRaycaster>() == null) panel.AddComponent<GraphicRaycaster>();

                onChecked.AddEventHandler(toggler, (Action)Toggled);
            }
            catch (Exception error) { Plugin.Log.LogError("UXAssist starmap filter fix skipped: " + error); }
        }

        private static void Toggled() => Sync(true);  // the button is only reachable on the open starmap
        private static void MapOpened() => Sync(true);
        private static void MapClosed() => Sync(false);

        private static void Sync(bool mapOpen)
        {
            if (toggler == null || checkedProperty == null) return;
            try
            {
                bool filterOpen = (bool)checkedProperty.GetValue(toggler, null), open = filterOpen && mapOpen;
                if (backdrop != null) backdrop.SetActive(filterOpen);
                if (canvas == null) return;
                if (open)
                {
                    int top = 0;
                    foreach (Canvas other in UIRoot.instance.uiGame.GetComponentsInChildren<Canvas>(true))
                        if (other != canvas) top = Math.Max(top, other.sortingOrder);
                    foreach (Canvas root in UIRoot.instance.uiGame.GetComponentsInParent<Canvas>(true)) top = Math.Max(top, root.sortingOrder);
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = top + 1;
                }
                else canvas.overrideSorting = false;
            }
            catch (Exception error) { Plugin.Log.LogError("UXAssist starmap filter fix: " + error); }
        }
    }

    internal static class MemberExtensions
    {
        public static T ThrowIfMissing<T>(this T member, string name) where T : class => member ?? throw new MissingMemberException(name);
    }
}
