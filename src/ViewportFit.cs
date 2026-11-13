using System;
using System.Collections.Generic;
using UnityEngine;

namespace BUG_UIFix
{
    // From DSPBigInventory 1.0.11 (local): scales a window down and moves it so its whole frame stays inside the safe area.
    [DefaultExecutionOrder(10000)]
    public sealed class ViewportFit : MonoBehaviour
    {
        static readonly List<ViewportFit> instances = new List<ViewportFit>();
        readonly Vector3[] corners = new Vector3[4];
        RectTransform window;
        Vector3 originalScale;

        public static void Attach(RectTransform rect)
        {
            if (rect == null || rect.GetComponent<ViewportFit>() != null) return;
            var fit = rect.gameObject.AddComponent<ViewportFit>();
            fit.window = rect;
            fit.originalScale = rect.localScale;
            instances.Add(fit);
        }

        public static void RemoveAll()
        {
            foreach (var fit in instances)
            {
                if (fit == null) continue;
                if (fit.window != null) fit.window.localScale = fit.originalScale;
                Destroy(fit);
            }
            instances.Clear();
        }

        void OnDestroy()
        {
            if (window != null) window.localScale = originalScale;
            instances.Remove(this);
        }

        public static float ScaleToFit(float width, float height, Rect available)
        {
            // shortcut: no minimum scale; add scrolling if very large inventories need full-size cells.
            if (!(width > 0) || !(height > 0) || !(available.width > 0) || !(available.height > 0) ||
                float.IsInfinity(width) || float.IsInfinity(height)) return 1f;
            return Math.Min(1f, Math.Min(available.width / width, available.height / height));
        }

        public static Vector2 OffsetToFit(Rect bounds, Rect available)
        {
            float x = bounds.xMin < available.xMin ? available.xMin - bounds.xMin :
                      bounds.xMax > available.xMax ? available.xMax - bounds.xMax : 0f;
            float y = bounds.yMin < available.yMin ? available.yMin - bounds.yMin :
                      bounds.yMax > available.yMax ? available.yMax - bounds.yMax : 0f;
            return new Vector2(x, y);
        }

        Rect Measure(Camera camera)
        {
            window.GetWorldCorners(corners);
            Vector2 first = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            float left = first.x, right = first.x, bottom = first.y, top = first.y;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                left = Math.Min(left, point.x); right = Math.Max(right, point.x);
                bottom = Math.Min(bottom, point.y); top = Math.Max(top, point.y);
            }
            return Rect.MinMaxRect(left, bottom, right, top);
        }

        void LateUpdate()
        {
            if (window == null || UIRoot.instance == null || UIRoot.instance.overlayCanvas == null) return;
            var parent = window.parent as RectTransform;
            if (parent == null || originalScale.x <= 0 || window.localScale.x <= 0) return;
            Rect available = Screen.safeArea;
            available = new Rect(available.x + 8, available.y + 8, available.width - 16, available.height - 16);
            if (available.width <= 0 || available.height <= 0) return;
            Camera camera = UIRoot.instance.overlayCanvas.worldCamera;
            Rect bounds = Measure(camera);
            float current = window.localScale.x / originalScale.x;
            float scale = ScaleToFit(bounds.width / current, bounds.height / current, available);
            var desired = new Vector3(originalScale.x * scale, originalScale.y * scale, originalScale.z);
            if (window.localScale != desired)
            {
                window.localScale = desired;
                bounds = Measure(camera);
            }
            Vector2 offset = OffsetToFit(bounds, available);
            if (offset == Vector2.zero) return;
            Vector2 from, to;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, bounds.min, camera, out from) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, bounds.min + offset, camera, out to))
                window.anchoredPosition += to - from;
        }
    }
}
