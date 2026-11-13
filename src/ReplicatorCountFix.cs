using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // The game's replicator: the number above the craft button counts batches, while a recipe can make several items per
    // batch (for example x8). It now counts the items the queue would make: 285 batches of 8 show 2280.
    public static class ReplicatorCountFix
    {
        public static string Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(UIReplicatorWindow), "_OnUpdate").ThrowIfMissing("UIReplicatorWindow._OnUpdate"),
                postfix: new HarmonyMethod(typeof(ReplicatorCountFix), nameof(Postfix)));
            return "craftable count shows items, not batches";
        }

        static readonly AccessTools.FieldRef<UIReplicatorWindow, RecipeProto> Selected = AccessTools.FieldRefAccess<UIReplicatorWindow, RecipeProto>("selectedRecipe");

        // The game writes the text every update, so scaling it here never scales twice.
        static void Postfix(UIReplicatorWindow __instance)
        {
            var recipe = Selected(__instance);
            var text = __instance.currPredictCountText;
            if (recipe?.ResultCounts == null || recipe.ResultCounts.Length == 0 || text == null) return;
            text.text = Items(text.text, recipe.ResultCounts[0]);
            Widen(__instance, text);
        }

        // The frame is sized for the game's longest count, ">999"; a longer number widens it, centred, instead of spilling out.
        // The frame is the count text plus the fixed-width pictures of its group at least half the text's width (its backdrop).
        static RectTransform[] frames;
        static Vector2[] sizes, places;
        static float room = -1f;
        static void Widen(UIReplicatorWindow window, Text text)
        {
            if (room < 0f)
            {
                var rect = text.rectTransform;
                var list = new System.Collections.Generic.List<RectTransform> { rect };
                var group = window.currPredictGroup != null ? window.currPredictGroup.transform : rect.parent;
                foreach (var g in group.GetComponentsInChildren<Graphic>(true))
                {
                    var r = g.rectTransform;
                    if (r != rect && r.anchorMin.x == r.anchorMax.x && r.rect.width >= rect.rect.width * 0.5f) list.Add(r);
                }
                frames = list.ToArray();
                sizes = System.Array.ConvertAll(frames, r => r.sizeDelta);
                places = System.Array.ConvertAll(frames, r => r.anchoredPosition);
                room = Width(text, ">999");
                Plugin.Log.LogInfo("Replicator count frame: " + string.Join(", ", System.Array.ConvertAll(frames, r => r.name + " " + r.rect.width.ToString("0"))) + "; room " + room.ToString("0"));
            }
            float extra = Extra(room, Width(text, text.text));
            for (int i = 0; i < frames.Length; i++)
            {
                var r = frames[i];
                var size = new Vector2(sizes[i].x + extra, sizes[i].y);
                if (r.sizeDelta == size) continue;
                r.sizeDelta = size;
                r.anchoredPosition = new Vector2(places[i].x + extra * (r.pivot.x - 0.5f), places[i].y);
            }
        }

        static float Width(Text text, string s)
        {
            var settings = text.GetGenerationSettings(Vector2.zero);
            settings.scaleFactor = 1f;
            return text.cachedTextGeneratorForLayout.GetPreferredWidth(s, settings);
        }

        public static float Extra(float room, float need) => need > room ? (float)System.Math.Ceiling(need - room) : 0f;

        // "285" with 8 per batch -> "2280"; ">999" -> ">7992"; empty or unreadable stays as it is.
        public static string Items(string batches, int perBatch)
        {
            if (perBatch <= 1 || string.IsNullOrEmpty(batches)) return batches;
            bool more = batches[0] == '>';
            if (!int.TryParse(more ? batches.Substring(1) : batches, out int n)) return batches;
            return (more ? ">" : "") + (n * perBatch);
        }
    }
}
