using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BUG_UIFix
{
    // DSPReplicatorPlus: its 1 ... 500 buttons count batches, while the count above the craft button (ReplicatorCountItems)
    // and the queue count items for a one-product recipe: with 224 shown (28 batches of 8) 50 and 100 stayed grey. There a
    // button now adds that many items, rounded up to whole batches (50 at 8 per batch adds 7 batches, 56), and is on when the
    // count reaches it. MAX is left alone (BUG_ModsPatch makes it add the real most).
    public static class ReplicatorPlusFix
    {
        public const string Guid = "Appun.DSP.plugin.DSPReplicatorPlus";
        static readonly int[] Amounts = { 500, 100, 50, 10, 5, 1 };
        static FieldInfo[] heads, ends;
        static MethodInfo addEnd, addHead;

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            Type ui = AccessTools.TypeByName("DSPReplicatorPlus.UI").ThrowIfMissing("DSPReplicatorPlus.UI");
            Type patch = AccessTools.TypeByName("DSPReplicatorPlus.Patch").ThrowIfMissing("DSPReplicatorPlus.Patch");
            addEnd = AccessTools.Method(ui, "mechaForgeAddTask").ThrowIfMissing("UI.mechaForgeAddTask");
            addHead = AccessTools.Method(ui, "mechaForgeAddTaskHead").ThrowIfMissing("UI.mechaForgeAddTaskHead");
            heads = new FieldInfo[Amounts.Length];
            ends = new FieldInfo[Amounts.Length];
            var click = new HarmonyMethod(typeof(ReplicatorPlusFix), nameof(Click));
            for (int i = 0; i < Amounts.Length; i++)
            {
                heads[i] = AccessTools.Field(ui, "head" + Amounts[i]).ThrowIfMissing("UI.head" + Amounts[i]);
                ends[i] = AccessTools.Field(ui, "end" + Amounts[i]).ThrowIfMissing("UI.end" + Amounts[i]);
                harmony.Patch(AccessTools.Method(ui, "onClickHead" + Amounts[i]).ThrowIfMissing("UI.onClickHead" + Amounts[i]), prefix: click);
                harmony.Patch(AccessTools.Method(ui, "onClickEnd" + Amounts[i]).ThrowIfMissing("UI.onClickEnd" + Amounts[i]), prefix: click);
            }
            harmony.Patch(AccessTools.Method(patch, "UIReplicatorWindow_OnUpdate_Postfix").ThrowIfMissing("Patch.UIReplicatorWindow_OnUpdate_Postfix"),
                postfix: new HarmonyMethod(typeof(ReplicatorPlusFix), nameof(Enable)));
            return "amount buttons count items, as the count above them";
        }

        // Items per batch when the count is in items (one product), else 1 (the buttons stay in batches).
        public static int PerBatch(RecipeProto r) => r?.ResultCounts != null && r.ResultCounts.Length == 1 && r.ResultCounts[0] > 1 ? r.ResultCounts[0] : 1;
        public static int BatchesFor(int items, int perBatch) => (items + perBatch - 1) / perBatch;

        static int Per(UIReplicatorWindow window)
        {
            if (window == null || GameMain.sandboxToolsEnabled && window.isInstantItem) return 1;  // ReplicatorPlus's own rules there
            return PerBatch(ReplicatorCountFix.Selected(window));
        }

        // onClickEnd500 / onClickHead50 ...: the amount is in the method name.
        static bool Click(MethodBase __originalMethod)
        {
            int per = Per(UIRoot.instance?.uiGame?.replicator);
            if (per <= 1) return true;
            string name = __originalMethod.Name;
            bool head = name.StartsWith("onClickHead");
            int items = int.Parse(name.Substring(head ? "onClickHead".Length : "onClickEnd".Length));
            (head ? addHead : addEnd).Invoke(null, new object[] { BatchesFor(items, per) });
            return false;
        }

        // Runs after ReplicatorPlus has set the buttons by batches; a static target, so the window is __0, not __instance.
        static void Enable(UIReplicatorWindow __0)
        {
            var recipe = ReplicatorCountFix.Selected(__0);
            int per = Per(__0);
            if (recipe == null || !recipe.Handcraft || per <= 1) return;
            int batches = Batches(ReplicatorCountFix.Forge(__0), recipe.ID, BatchesFor(Amounts[0], per));
            for (int i = 0; i < Amounts.Length; i++)
            {
                bool on = (long)batches * per >= Amounts[i];
                Set(heads[i], on);
                Set(ends[i], on);
            }
        }

        static void Set(FieldInfo field, bool on)
        {
            var button = (field.GetValue(null) as GameObject)?.GetComponent<Button>();
            if (button != null) button.interactable = on;
        }

        // Batches the replicator can make, counted only up to what the largest button needs; a few times a second.
        static int cachedRecipe, cachedFrame = int.MinValue, cached;
        static int Batches(MechaForge forge, int recipeId, int enough)
        {
            if (forge == null) return 0;
            if (recipeId != cachedRecipe || Time.frameCount - cachedFrame >= 15)
            {
                var marks = new System.Collections.Generic.HashSet<int>(forge.bottleneckItems);  // the trials add shortage marks
                cached = forge.PredictTaskCount(recipeId, enough);
                forge.bottleneckItems.Clear();
                forge.bottleneckItems.UnionWith(marks);
                cachedRecipe = recipeId;
                cachedFrame = Time.frameCount;
            }
            return cached;
        }
    }
}
