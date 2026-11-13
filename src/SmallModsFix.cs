using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace BUG_UIFix
{
    // Multifunction: its window's inventory button shows the current inventory size, and with DSPBigInventory the most
    // that fits on screen: "17×21 (≤30×18)".
    public static class MultifunctionFix
    {
        public const string Guid = "cn.blacksnipe.dsp.Multfuntion_mod";

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            if (AccessTools.TypeByName("Multifunction_mod.Patchs.PlanetReformCollectPatch") != null) return "already contains this fix, skipped";
            Type draw = AccessTools.TypeByName("Multifunction_mod.GUIDraw").ThrowIfMissing("Multifunction_mod.GUIDraw");
            harmony.Patch(AccessTools.Method(draw, "PlayerPannel").ThrowIfMissing("GUIDraw.PlayerPannel"),
                transpiler: new HarmonyMethod(typeof(MultifunctionFix), nameof(PackageButton)));
            return "inventory button shows the inventory size";
        }

        static IEnumerable<CodeInstruction> PackageButton(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            int at = code.FindIndex(c => c.opcode == OpCodes.Ldstr && (string)c.operand == "背包");
            if (at < 0 || at + 1 >= code.Count || code[at + 1].opcode != OpCodes.Call) throw new InvalidOperationException("inventory button label not found");
            code.Insert(at + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MultifunctionFix), nameof(WithPackageSize))));
            return code;
        }

        public static string WithPackageSize(string label)
        {
            Player player = GameMain.mainPlayer;
            if (player?.package == null || player.packageColCount <= 0) return label;
            int columns = player.packageColCount;
            string text = label + "\n" + columns + "×" + ((player.package.size - 1) / columns + 1);
            if (ScreenLimit("MaxColumnsNow", 0, out int maxColumns) && ScreenLimit("MaxRowsNow", columns, out int maxRows)) text += " (≤" + maxColumns + "×" + maxRows + ")";
            return text;
        }

        // DSPBigInventory's screen limit, looked up by name (false without that mod): our local builds have it themselves,
        // the Thunderstore release gets it from BUG-ModsPatch (only while that module is active).
        static bool ScreenLimit(string name, int argument, out int limit)
        {
            limit = int.MaxValue;
            try
            {
                bool withColumns = name == "MaxRowsNow";
                Type type = AccessTools.TypeByName("DSPBigInventory.ScreenLimits");
                if (type == null)
                {
                    type = AccessTools.TypeByName("ModsPatch.BigInventory.ScreenLimits");
                    if (type == null || !(bool)AccessTools.Field(type, "Active").GetValue(null)) return false;
                }
                MethodInfo method = type.GetMethod(name, withColumns ? new[] { typeof(int) } : Type.EmptyTypes);
                if (method == null) return false;
                limit = (int)method.Invoke(null, withColumns ? new object[] { argument } : null);
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    // LaserClearing: a recreated toggle button (new game, UI rebuilt) gets its tip and colour again; the mod kept the old
    // status and skipped the update because it looked unchanged.
    public static class LaserClearingFix
    {
        public const string Guid = "starfi5h.plugin.LaserClearing";
        static FieldInfo button, status;

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            Type ui = AccessTools.TypeByName("LaserClearing.UI_Patch").ThrowIfMissing("LaserClearing.UI_Patch");
            if (AccessTools.Field(AccessTools.TypeByName("LaserClearing.LocalLaser_Patch"), "laserTargets") != null) return "already contains this fix, skipped";
            button = AccessTools.Field(ui, "enableButton").ThrowIfMissing("UI_Patch.enableButton");
            status = AccessTools.Field(ui, "buttonStatus").ThrowIfMissing("UI_Patch.buttonStatus");
            harmony.Patch(AccessTools.Method(ui, "OnEnableChanged").ThrowIfMissing("UI_Patch.OnEnableChanged"),
                prefix: new HarmonyMethod(typeof(LaserClearingFix), nameof(BeforeButton)));
            return "toggle button tip and colour after a rebuild";
        }

        static void BeforeButton()
        {
            if (button.GetValue(null) == null) status.SetValue(null, Enum.ToObject(status.FieldType, 0));  // ButtonStatus.None
        }
    }
}

namespace BUG_UIFix
{
    // MechaDronesTweaks: the mecha window and the tech tree showed the game's drone speed, not the modded one. The game reads
    // the field there by address (ldflda, for ToString), which the mod's ldfld-only transpiler misses; drones themselves
    // already fly at the modded speed. Those reads now get the same value the mod uses: FixedSpeed, or speed * SpeedMultiplier.
    public static class MechaDronesTweaksFix
    {
        public const string Guid = "org.soardev.mechadronestweaks";
        static FieldInfo useFixed, fixedSpeed, multiplier;

        public static string Apply(Harmony harmony)
        {
            if (!Plugin.Loaded(Guid)) return "not loaded, nothing to do";
            Type mod = AccessTools.TypeByName("MechaDronesTweaks.MechaDronesTweaks").ThrowIfMissing("MechaDronesTweaks.MechaDronesTweaks");
            if (AccessTools.Method(mod, "GetDroneSpeed") != null) return "already contains this fix, skipped";
            useFixed = AccessTools.Field(mod, "UseFixedSpeed").ThrowIfMissing("UseFixedSpeed");
            fixedSpeed = AccessTools.Field(mod, "FixedSpeed").ThrowIfMissing("FixedSpeed");
            multiplier = AccessTools.Field(mod, "SpeedMultiplier").ThrowIfMissing("SpeedMultiplier");
            var transpiler = new HarmonyMethod(typeof(MechaDronesTweaksFix), nameof(DisplayedSpeed));
            harmony.Patch(AccessTools.Method(typeof(UIMechaWindow), "UpdateProps"), transpiler: transpiler);
            harmony.Patch(AccessTools.Method(typeof(UITechTree), nameof(UITechTree.RefreshDataValueText)), transpiler: transpiler);
            return "drone speed shown as the mod sets it";
        }

        public static IEnumerable<CodeInstruction> DisplayedSpeed(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            FieldInfo speed = AccessTools.Field(typeof(GameHistoryData), nameof(GameHistoryData.constructionDroneSpeed));
            LocalBuilder temporary = null;
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode != OpCodes.Ldflda || !Equals(code.operand, speed)) { yield return code; continue; }
                temporary ??= generator.DeclareLocal(typeof(float));
                code.opcode = OpCodes.Call;  // keeps the instruction's labels
                code.operand = AccessTools.Method(typeof(MechaDronesTweaksFix), nameof(Speed));
                yield return code;
                yield return new CodeInstruction(OpCodes.Stloc, temporary);
                yield return new CodeInstruction(OpCodes.Ldloca, temporary);
            }
        }

        public static float Speed(GameHistoryData history) =>
            (bool)useFixed.GetValue(null) ? (float)fixedSpeed.GetValue(null) : history.constructionDroneSpeed * (float)multiplier.GetValue(null);
    }
}
