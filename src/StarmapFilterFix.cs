using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // UXAssist's starmap filter (">>" / "X", top left) was drawn under the game's Planet Shield Details panel, which comes
    // later in the "In Game" hierarchy; the planet list was unreadable. While the filter is open the shield panel is moved
    // before the starmap (drawn behind it) and an opaque backdrop sits behind the list. Closing restores both.
    internal static class StarmapFilterFix
    {
        public const string Guid = "org.soardev.uxassist";

        private static Component toggler;
        private static PropertyInfo checkedProperty;
        private static GameObject backdrop;
        private static int shieldIndex = -1;

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
            return "starmap filter drawn above the planet shield panel";
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
                Transform inGame = UIRoot.instance.uiGame.transform;
                Transform shield = inGame.Find("Planet Shield Details");
                Transform starmap = toggler.transform;
                while (starmap.parent != null && starmap.parent != inGame) starmap = starmap.parent;
                if (shield == null || starmap.parent != inGame) return;
                if (open && shieldIndex < 0 && shield.GetSiblingIndex() > starmap.GetSiblingIndex())
                {
                    shieldIndex = shield.GetSiblingIndex();
                    shield.SetSiblingIndex(starmap.GetSiblingIndex());
                }
                else if (!open && shieldIndex >= 0)
                {
                    shield.SetSiblingIndex(shieldIndex);
                    shieldIndex = -1;
                }
            }
            catch (Exception error) { Plugin.Log.LogError("UXAssist starmap filter fix: " + error); }
        }
    }

    internal static class MemberExtensions
    {
        public static T ThrowIfMissing<T>(this T member, string name) where T : class => member ?? throw new MissingMemberException(name);
    }
}
