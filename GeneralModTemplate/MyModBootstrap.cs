using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MyModNamespace;

/// <summary>
/// Mod entry point.
///
/// The game discovers this class via the [ModInitializer] attribute and calls
/// the named static method (Init) during startup. Harmony.PatchAll scans this
/// assembly for all [HarmonyPatch] classes and applies them automatically.
///
/// Key rules:
///   1. The method name in nameof() must match the actual method name
///   2. Use a unique Harmony ID (e.g. "yourname.yourmod") to avoid conflicts
///   3. Guard against double-init with _initialized flag
/// </summary>
[ModInitializer(nameof(Init))]
public static class MyModBootstrap
{
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        new Harmony("yourname.mymod").PatchAll(Assembly.GetExecutingAssembly());
        Log.Info("[MyMod] Mod initialized.");
    }
}
