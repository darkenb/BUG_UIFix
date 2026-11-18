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

        internal static readonly AccessTools.FieldRef<UIReplicatorWindow, RecipeProto> Selected = AccessTools.FieldRefAccess<UIReplicatorWindow, RecipeProto>("selectedRecipe");
        internal static readonly AccessTools.FieldRef<UIReplicatorWindow, MechaForge> Forge = AccessTools.FieldRefAccess<UIReplicatorWindow, MechaForge>("mechaForge");

        // The game writes the text every update, so scaling it here never scales twice.
        static void Postfix(UIReplicatorWindow __instance)
        {
            var recipe = Selected(__instance);
            var text = __instance.currPredictCountText;
            if (recipe == null || text == null) return;
            if (NoLimit && text.text.StartsWith(">")) text.text = Most(Forge(__instance), recipe.ID);
            // Same rule as the game's queue: items for a one-product recipe, batches when it makes several products.
            if (CountItems && recipe.ResultCounts != null && recipe.ResultCounts.Length == 1) text.text = Items(text.text, recipe.ResultCounts[0]);
            Widen(__instance, text);
            if (InventoryCounts) Stock(__instance, recipe, Forge(__instance));
        }

        // Switches in the config file (Plugin): each part of this fix can be turned off on its own.
        public static bool CountItems = true, NoLimit = true, InventoryCounts = true;

        // Each input in the recipe tree shows what one batch takes (135); how many the inventory holds was nowhere, so
        // "148 craftable" could not be checked. The inventory count is written under each input, red below one batch.
        static readonly AccessTools.FieldRef<UIReplicatorWindow, System.Collections.Generic.List<UIButton>> Inputs =
            AccessTools.FieldRefAccess<UIReplicatorWindow, System.Collections.Generic.List<UIButton>>("treeDownList");
        static readonly System.Collections.Generic.Dictionary<UIButton, Text> stocks = new System.Collections.Generic.Dictionary<UIButton, Text>();
        static readonly Color Enough = new Color(0.75f, 0.75f, 0.75f, 0.9f), Short = new Color(1f, 0.4f, 0.35f, 0.95f);

        static void Stock(UIReplicatorWindow window, RecipeProto recipe, MechaForge forge)
        {
            var buttons = Inputs(window);
            var package = GameMain.mainPlayer?.package;
            if (buttons == null || package == null || recipe.Items == null) return;
            if (recipe.ID != stockRecipe || Time.frameCount - stockFrame >= 15) { chained.Clear(); stockRecipe = recipe.ID; stockFrame = Time.frameCount; }
            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.gameObject.activeSelf || button.transitions == null || button.transitions.Length < 3) continue;
                if (!(button.transitions[2].target is Text need)) continue;
                if (!stocks.TryGetValue(button, out var have) || have == null) stocks[button] = have = NewStock(need, button);
                bool shown = i < recipe.Items.Length && need.enabled;
                if (have.gameObject.activeSelf != shown) have.gameObject.SetActive(shown);
                if (!shown) continue;
                int item = recipe.Items[i];
                if (!chained.TryGetValue(item, out int count)) chained[item] = count = Obtainable(forge, package, item);
                have.text = count.ToString("#,0");
                have.color = count >= recipe.ItemCounts[i] ? Enough : Short;
            }
        }

        // The queue makes a missing input from its handcraft recipe (chain production), so an input counts what the inventory
        // holds, the queue's leftovers and what that chain can make from the inventory: 0 coal dust with 2 craftable shows 2.
        static readonly System.Collections.Generic.Dictionary<int, int> chained = new System.Collections.Generic.Dictionary<int, int>();
        static int stockRecipe, stockFrame = int.MinValue;
        static int Obtainable(MechaForge forge, StorageComponent package, int item)
        {
            long have = package.GetItemCount(item) + (forge?.extraItems?.GetCount(item) ?? 0);
            var proto = LDB.items.Select(item);
            var craft = proto?.handcraft;
            if (forge != null && craft != null && GameMain.history != null && GameMain.history.RecipeUnlocked(craft.ID))
            {
                var marks = new System.Collections.Generic.HashSet<int>(forge.bottleneckItems);  // the trials add shortage marks
                have += (long)System.Math.Min(forge.PredictTaskCount(craft.ID, MostBatches - 1), MostBatches) * proto.handcraftProductCount;
                forge.bottleneckItems.Clear();
                forge.bottleneckItems.UnionWith(marks);
            }
            return (int)System.Math.Min(have, int.MaxValue);
        }

        // A copy of the batch count, centred under the input's icon: clear of that count (bottom right of the icon) and of
        // the row of items made from this recipe's product (above the recipe).
        const string StockName = "bug-inventory-count";
        static Text NewStock(Text need, UIButton button)
        {
            // The game makes further input buttons by copying the first one, ours included: reuse that copy, not a second text.
            var copied = button.transform.Find(StockName)?.GetComponent<Text>();
            if (copied != null) return copied;
            var text = Object.Instantiate(need, button.transform);
            text.name = StockName;
            text.material = null;  // the copied value material makes bright text glow
            text.raycastTarget = false;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, 1f);  // midway between the icon and the window's bottom edge (seen in game)
            return text;
        }

        // The game stops counting at ">999" batches; the real most is shown, up to MostBatches. BUG_ModsPatch makes
        // ReplicatorPlus's MAX buttons add this same number (it used to add 1000).
        public const int MostBatches = 99999;
        static int cachedRecipe, cachedFrame = int.MinValue;
        static string cached;
        static string Most(MechaForge forge, int recipeId)
        {
            // Up to ~34 trial plans: a few times a second is enough for a number the eye reads.
            if (recipeId != cachedRecipe || Time.frameCount - cachedFrame >= 15)
            {
                var marks = new System.Collections.Generic.HashSet<int>(forge.bottleneckItems);  // the trials add shortage marks
                cached = System.Math.Min(forge.PredictTaskCount(recipeId, MostBatches - 1), MostBatches).ToString();
                forge.bottleneckItems.Clear();
                forge.bottleneckItems.UnionWith(marks);
                cachedRecipe = recipeId;
                cachedFrame = Time.frameCount;
            }
            return cached;
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
