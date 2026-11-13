using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // New game screen: a small text box at the right end of the star count slider (and of UniverseGenTweaks' four sliders).
    // A typed value is clamped to the slider's min-max and set on the slider, so the mods react as to a drag.
    // Vanilla game only; no other mod needed.
    public static class NewGameInputFix
    {
        // Row name -> slider value per shown unit (UniverseGenTweaks shows slider/10, flatness slider/50).
        static readonly Dictionary<string, float> UgtScale = new() { { "min-dist", 10f }, { "min-step", 10f }, { "max-step", 10f }, { "flatten", 50f } };

        class Box { public Slider slider; public Text shown; public InputField input; public float scale; }
        static readonly List<Box> boxes = new();

        public static string Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(UIGalaxySelect), "_OnOpen"), postfix: new HarmonyMethod(typeof(NewGameInputFix), nameof(Opened)) { priority = Priority.Last });
            harmony.Patch(AccessTools.Method(typeof(UIGalaxySelect), "_OnUpdate"), postfix: new HarmonyMethod(typeof(NewGameInputFix), nameof(Update)));
            return "new game slider values can be typed";
        }

        // On open, not create: UniverseGenTweaks clones the star count row in its _OnCreate, our box must not be cloned with it.
        static void Opened(UIGalaxySelect __instance)
        {
            boxes.RemoveAll(b => b.input == null);
            Add(__instance, __instance.starCountSlider, __instance.starCountText, 1f);
            Transform settings = __instance.starCountSlider.transform.parent.parent;
            foreach (var row in UgtScale)
            {
                Slider slider = settings.Find(row.Key)?.GetComponentInChildren<Slider>(true);
                if (slider != null) Add(__instance, slider, Find(slider.transform, "Text")?.GetComponent<Text>(), row.Value);
            }
        }

        static void Add(UIGalaxySelect ui, Slider slider, Text shown, float scale)
        {
            if (shown == null || boxes.Exists(b => b.slider == slider)) return;
            // A small box just right of the bar's end; the number on the slider handle stays as it was.
            // The box takes its own presses (InputField handles pointer and drag), so the slider does not move.
            var go = UnityEngine.Object.Instantiate(ui.seedInput.gameObject, slider.transform, false);
            go.name = "bug-value-input";
            var input = go.GetComponent<InputField>();
            input.onEndEdit = new InputField.EndEditEvent();  // drop the seed box's listeners
            input.onValueChanged = new InputField.OnChangeEvent();
            input.contentType = InputField.ContentType.Standard;
            input.characterLimit = 6;
            input.textComponent.font = shown.font;
            input.textComponent.fontSize = 13;
            input.textComponent.alignment = TextAnchor.MiddleCenter;
            input.textComponent.enabled = true;
            if (go.GetComponent<Image>() is Image bg) bg.enabled = true;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);
            rect.sizeDelta = new Vector2(46f, 20f);
            rect.localScale = Vector3.one;
            var box = new Box { slider = slider, shown = shown, input = input, scale = scale };
            input.onEndEdit.AddListener(text => Commit(box, text));
            boxes.Add(box);
        }

        static void Update()
        {
            foreach (var b in boxes)
            {
                if (b.input == null) continue;
                if (!b.input.isFocused && b.input.text != b.shown.text) b.input.text = b.shown.text;
            }
        }

        static void Commit(Box b, string text)
        {
            if (Parse(text, b.scale, b.slider.minValue, b.slider.maxValue, b.slider.wholeNumbers) is float value) b.slider.value = value;
            b.input.text = b.shown.text;  // invalid input or a clamped value: show what the slider really has
        }

        // Typed number (dot or comma) -> slider value inside [min, max]; null when not a number. Checked in check.cs.
        public static float? Parse(string text, float scale, float min, float max, bool whole)
        {
            if (!float.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float shown)) return null;
            float value = shown * scale;
            if (whole) value = Mathf.Round(value);
            return Mathf.Clamp(value, min, max);
        }

        static Transform Find(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
