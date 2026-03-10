using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MyCharacterMod;

/// <summary>
/// Mod entry point. The game scans for [ModInitializer] and calls the named method.
/// Harmony.PatchAll auto-discovers all [HarmonyPatch] classes in this assembly.
/// </summary>
[ModInitializer(nameof(Init))]
public static class MyCharacterBootstrap
{
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        new Harmony("yourname.mycharacter").PatchAll(Assembly.GetExecutingAssembly());
        Log.Info("[MyCharacter] Mod initialized.");
    }
}
