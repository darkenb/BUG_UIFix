using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // DSPAllPlanetInfo's "Interstellar Logistics" window.
    // 1. It reads DSPGame.globalOption.uiLayoutHeight, which the game leaves at 0 while the UI scale is "auto" (the real value
    //    is UICanvasScalerHandler.uiLayoutHeight). The window then landed on top of the game's planet panel at the right
    //    edge, and the row count came out negative, so no station was listed.
    // 2. The window was as tall as the screen. It is now draggable (body or title), foldable with a -/+ button, opens in the
    //    middle of the screen until it is first dragged, keeps position and fold state in the config, and stays on screen.
    //    Its background now fits the rows; the copied planet panel's width left an empty strip on the left.
    // 3. The cloned planet panel kept its tab strip, LSTM and menu buttons, which do nothing there; they are removed.
    public static class AllPlanetInfoFix
    {
        public const string Guid = "Appun.DSP.plugin.AllPlanetInfo";
        private const float CollapsedHeight = 10f;
        // The window keeps the copied planet panel's width (340), but the mod lays its rows out in the right 262 (x -250..+2):
        // the left of the background was always empty. Only the background is trimmed: the rows and divider lines follow
        // the window width, so the window itself keeps it.
        private const float RowsWidth = 262f;

        private static ConfigEntry<float> height, x, y;
        private static ConfigEntry<bool> saved, collapsed;
        private static Type ui;
        private static FieldInfo lineMax;
        private static GameObject content, collapseButton;
        private static readonly Vector3[] corners = new Vector3[4];

        private static GameObject Field(string name) => (GameObject)AccessTools.Field(ui, name).GetValue(null);
        private static GameObject Window => Field("infoWindow");

        public static string Apply(Harmony harmony, ConfigFile config)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            if (AccessTools.TypeByName("DSPAllPlanetInfo.WindowDrag") != null) return "already contains this fix, skipped";
            ui = AccessTools.TypeByName("DSPAllPlanetInfo.UI").ThrowIfMissing("DSPAllPlanetInfo.UI");
            lineMax = AccessTools.Field(AccessTools.TypeByName("DSPAllPlanetInfo.InfoCreater"), "lineMax").ThrowIfMissing("InfoCreater.lineMax");
            foreach (string name in new[] { "infoWindow", "WindowTitle", "previousButton", "nextButton", "planetInfoButton", "ItemName", "ItemIcon", "ItemCount", "ItemLogic", "Line" })
                AccessTools.Field(ui, name).ThrowIfMissing("UI." + name);
            MethodInfo create = AccessTools.Method(ui, "Create").ThrowIfMissing("UI.Create");
            MethodInfo place = AccessTools.Method(ui, "setWindowPos").ThrowIfMissing("UI.setWindowPos");
            MethodInfo opened = AccessTools.Method(AccessTools.TypeByName("DSPAllPlanetInfo.Hook+UIPlanetDetail_OnOpen_Postfix"), "Postfix").ThrowIfMissing("Hook.UIPlanetDetail_OnOpen_Postfix.Postfix");

            height = config.Bind("DSPAllPlanetInfo", "WindowHeight", 600f, new ConfigDescription("Height of the Interstellar Logistics window in UI units; more stations go to the next page.", new AcceptableValueRange<float>(150f, 1400f)));
            collapsed = config.Bind("DSPAllPlanetInfo", "WindowCollapsed", false, "Window folded to its title bar (the -/+ button).");
            saved = config.Bind("DSPAllPlanetInfo", "WindowPositionSaved", false, "False: the window opens in the middle of the screen. Set when the window is dragged.");
            x = config.Bind("DSPAllPlanetInfo", "WindowX", 0f, "Saved window position (local UI units), written when the window is dragged.");
            y = config.Bind("DSPAllPlanetInfo", "WindowY", 0f, "Saved window position (local UI units), written when the window is dragged.");

            harmony.Patch(place, prefix: new HarmonyMethod(typeof(AllPlanetInfoFix), nameof(SetWindowPos)));
            harmony.Patch(create, postfix: new HarmonyMethod(typeof(AllPlanetInfoFix), nameof(AfterCreate)));
            harmony.Patch(opened, postfix: new HarmonyMethod(typeof(AllPlanetInfoFix), nameof(AfterOpen)));
            return "logistics window placement, drag, fold and row count fixed";
        }

        public static int Rows(float windowHeight) => ((int)windowHeight - 30) / 20;   // rows start 40 below the top, 20 apart

        // Replaces UI.setWindowPos.
        private static bool SetWindowPos()
        {
            try
            {
                int layoutHeight = UICanvasScalerHandler.uiLayoutHeight;
                int layoutWidth = layoutHeight * Screen.width / Screen.height;
                Field("planetInfoButton").transform.localPosition = new Vector3(-(layoutWidth / 2) + 50, layoutHeight / 2 - 30, 0f);
                var rect = (RectTransform)Window.transform;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height.Value);
                if (saved.Value) rect.localPosition = new Vector3(x.Value, y.Value, 0f);
                else CenterOnScreen();
                if (content != null) ApplyCollapsed();
                ClampToScreen();
                Field("previousButton").transform.localPosition = new Vector3(-125f, -20f, 0f);
                Field("nextButton").transform.localPosition = new Vector3(-20f, -20f, 0f);
                return false;
            }
            catch (Exception error)
            {
                Plugin.Log.LogError("DSPAllPlanetInfo window placement: " + error);
                return true;
            }
        }

        private static void AfterOpen() => lineMax.SetValue(null, Rows(height.Value));

        // Rows and page buttons move into one container so folding hides them all; the title and the -/+ button stay.
        private static void AfterCreate()
        {
            try
            {
                GameObject window = Window;
                content = new GameObject("content", typeof(RectTransform));
                var contentRect = (RectTransform)content.transform;
                contentRect.SetParent(window.transform, worldPositionStays: false);
                contentRect.anchorMin = contentRect.anchorMax = ((RectTransform)window.transform).pivot;   // on the window pivot whatever the height
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                foreach (string group in new[] { "ItemName", "ItemIcon", "ItemCount", "ItemLogic", "Line" })
                    foreach (GameObject row in (GameObject[])AccessTools.Field(ui, group).GetValue(null))
                        if (row != null) row.transform.SetParent(contentRect, worldPositionStays: true);
                GameObject previous = Field("previousButton");
                previous.transform.SetParent(contentRect, worldPositionStays: true);
                Field("nextButton").transform.SetParent(contentRect, worldPositionStays: true);

                collapseButton = UnityEngine.Object.Instantiate(previous, window.transform);
                collapseButton.name = "collapseButton";
                UnityEngine.Object.Destroy(collapseButton.GetComponent<UIButton>());
                UnityEngine.Object.Destroy(collapseButton.transform.Find("Text").GetComponent<Localizer>());
                collapseButton.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 20f);
                collapseButton.transform.localPosition = new Vector3(-15f, 15f, 0f);
                collapseButton.SetActive(true);
                Button button = collapseButton.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();   // drop the page listener copied from the source button
                button.onClick.AddListener(() => { collapsed.Value = !collapsed.Value; ApplyCollapsed(); ClampToScreen(); });

                foreach (string leftover in new[] { "black-bg/detail-tabs", "lstm-show-btn", "menu-btn" })
                {
                    Transform t = window.transform.Find(leftover);
                    if (t != null) UnityEngine.Object.Destroy(t.gameObject);
                }

                var background = window.transform.Find("black-bg") as RectTransform;
                float unused = ((RectTransform)window.transform).rect.width - RowsWidth;
                if (background != null && unused > 0f) background.offsetMin += new Vector2(unused, 0f);

                // Drag anywhere on the background or the title (drag events bubble up to the window).
                Graphic backgroundGraphic = background != null ? background.GetComponent<Graphic>() : null;
                if (backgroundGraphic != null) backgroundGraphic.raycastTarget = true;
                else if (window.GetComponent<Graphic>() == null) window.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
                Field("WindowTitle").GetComponent<Text>().raycastTarget = true;
                window.AddComponent<WindowDrag>();
                SetWindowPos();
            }
            catch (Exception error) { Plugin.Log.LogError("DSPAllPlanetInfo window frame: " + error); }
        }

        private static void ApplyCollapsed()
        {
            var rect = (RectTransform)Window.transform;
            content.SetActive(!collapsed.Value);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, collapsed.Value ? CollapsedHeight : height.Value);
            collapseButton.transform.Find("Text").GetComponent<Text>().text = collapsed.Value ? "+" : "-";
        }

        private static RectTransform ScreenRect() => (RectTransform)Window.GetComponentInParent<Canvas>().rootCanvas.transform;

        // Moves stay in the canvas plane: the UI camera is perspective, so leaving the plane draws the window bigger or smaller.
        private static void CenterOnScreen()
        {
            var rect = (RectTransform)Window.transform;
            RectTransform screen = ScreenRect();
            rect.GetWorldCorners(corners);
            Vector2 middle = screen.InverseTransformPoint((corners[0] + corners[2]) * 0.5f);
            rect.position += screen.TransformVector(screen.rect.center - middle);
        }

        // Keeps the window and the title row above its top edge on screen; the top-left wins when it does not fit.
        internal static void ClampToScreen()
        {
            var rect = (RectTransform)Window.transform;
            RectTransform screen = ScreenRect();
            rect.GetWorldCorners(corners);
            Vector2 min = screen.InverseTransformPoint(corners[0]), max = screen.InverseTransformPoint(corners[2]);
            max.y += 30f;
            rect.position += screen.TransformVector(Clamp(min, max, screen.rect));
            Vector3 local = rect.localPosition;
            if (local.z != 0f) rect.localPosition = new Vector3(local.x, local.y, 0f);
        }

        // Shift that brings the box min..max inside area (pure, checked in check.cs).
        public static Vector2 Clamp(Vector2 min, Vector2 max, Rect area)
        {
            Vector2 shift = Vector2.zero;
            if (max.x > area.xMax) shift.x = area.xMax - max.x;
            if (min.x + shift.x < area.xMin) shift.x = area.xMin - min.x;
            if (min.y < area.yMin) shift.y = area.yMin - min.y;
            if (max.y + shift.y > area.yMax) shift.y = area.yMax - max.y;
            return shift;
        }

        internal static void SavePosition()
        {
            Vector3 local = Window.transform.localPosition;
            x.Value = local.x;
            y.Value = local.y;
            saved.Value = true;
        }
    }

    internal class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Vector3 grab;

        private bool Point(PointerEventData e, out Vector3 world) => RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out world);

        public void OnBeginDrag(PointerEventData e)
        {
            if (Point(e, out Vector3 world)) grab = transform.position - world;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Point(e, out Vector3 world)) return;
            transform.position = world + grab;
            AllPlanetInfoFix.ClampToScreen();
        }

        public void OnEndDrag(PointerEventData e) => AllPlanetInfoFix.SavePosition();
    }
}
