using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace MyModNamespace;

// ============================================================================
// HARMONY PATCHING COOKBOOK
// ============================================================================
//
// Harmony patches let you modify game behavior without changing game code.
// There are several patch types:
//
// [Prefix]  - Runs BEFORE the original method.
//             Return false to skip the original. Use ref parameters to modify args.
//
// [Postfix] - Runs AFTER the original method.
//             Use ref __result to modify the return value.
//
// [Transpiler] - Modifies the IL code directly (advanced, rarely needed).
//
// Common parameters:
//   __instance  - The object instance (for non-static methods)
//   __result    - The return value (ref in Postfix to modify)
//   ___fieldName - Access private field "fieldName" on the instance
//   Original method parameters by name
//
// More info: https://harmony.pardeike.net/articles/patching.html
// ============================================================================

// ── Example 1: Keyboard input handler ──────────────────────────────────────
// Intercept game input to add hotkeys.
// NGame._Input is called for every input event in the game.

[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
internal static class ExampleInputPatch
{
    private static void Postfix(InputEvent inputEvent, NGame __instance)
    {
        // Only handle key press events (not releases or repeats)
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } keyEvent)
            return;

        switch (keyEvent.Keycode)
        {
            case Key.F10:
                Log.Info("[MyMod] F10 pressed!");
                // Show a fullscreen text notification
                __instance.AddChildSafely(NFullscreenTextVfx.Create("MyMod: Hello!"));
                break;
        }
    }
}

// ── Example 2: Modify a property return value (Postfix) ────────────────────
// This example shows how to modify player starting gold.
// Uncomment to activate.

// [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Characters.Ironclad), "get_StartingGold")]
// internal static class ModifyStartingGoldPatch
// {
//     private static void Postfix(ref int __result)
//     {
//         __result = 999; // Give Ironclad 999 starting gold
//     }
// }

// ── Example 3: Per-frame game logic (Process) ──────────────────────────────
// NRun._Process runs every frame during a run.
// Useful for continuous checks or per-frame modifications.
// Uncomment to activate.

// [HarmonyPatch(typeof(NRun), nameof(NRun._Process))]
// internal static class ExampleProcessPatch
// {
//     private static void Postfix()
//     {
//         RunState? runState = RunManager.Instance.DebugOnlyGetState();
//         Player? player = LocalContext.GetMe(runState);
//         if (player is null) return;
//
//         // Your per-frame logic here
//     }
// }

// ── Example 4: Skip original method (Prefix returning false) ───────────────
// A Prefix that returns false prevents the original method from running.
// Use sparingly - can break game logic if misused.

// [HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeMethod))]
// internal static class SkipOriginalPatch
// {
//     private static bool Prefix(SomeClass __instance, ref ReturnType __result)
//     {
//         if (someCondition)
//         {
//             __result = myCustomValue; // set return value
//             return false;             // skip original
//         }
//         return true; // run original
//     }
// }

// ── Example 5: Access private fields ───────────────────────────────────────
// Use AccessTools to read/write private fields on game objects.
//
// // Declare a field reference (do this once, as a static field):
// private static readonly AccessTools.FieldRef<TargetClass, FieldType> MyFieldRef =
//     AccessTools.FieldRefAccess<TargetClass, FieldType>("_privateFieldName");
//
// // Use it in a patch:
// FieldType value = MyFieldRef(__instance);       // read
// MyFieldRef(__instance) = newValue;              // write
