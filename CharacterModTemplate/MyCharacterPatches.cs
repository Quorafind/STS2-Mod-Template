using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace MyCharacterMod;

// ============================================================================
// TEXTURE LOADING HELPER
// ============================================================================
// The game's ResourceLoader can't load raw PNGs from mod PCKs.
// This helper loads images via Godot's Image.LoadFromFile and caches them.

internal static class ModTextureHelper
{
    private static readonly Dictionary<string, Texture2D?> TextureCache = new();

    public static Texture2D? LoadTexture(string path)
    {
        if (TextureCache.TryGetValue(path, out Texture2D? cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            Image image = Image.LoadFromFile(path);
            if (image.GetWidth() > 0 && image.GetHeight() > 0)
                texture = ImageTexture.CreateFromImage(image);
        }
        catch
        {
            texture = null;
        }

        TextureCache[path] = texture;
        return texture;
    }

    public static bool IsModAssetPath(string path)
    {
        // Match paths belonging to your mod's assets
        return path.Contains("/mycharacter", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/MyCharacter", StringComparison.Ordinal)
            || path.Contains("char_select_mycharacter", StringComparison.Ordinal);
    }
}

// ============================================================================
// ESSENTIAL PATCHES (required for any character mod)
// ============================================================================

/// <summary>
/// [REQUIRED] Intercept AssetCache.LoadAsset to handle mod textures.
/// Without this, all mod images will fail to load.
/// </summary>
[HarmonyPatch(typeof(AssetCache), "LoadAsset")]
internal static class ModAssetCachePatch
{
    private static readonly AccessTools.FieldRef<AssetCache, ConcurrentDictionary<string, Resource>> CacheRef =
        AccessTools.FieldRefAccess<AssetCache, ConcurrentDictionary<string, Resource>>("_cache");

    private static bool Prefix(AssetCache __instance, string path, ref Resource __result)
    {
        if (!ModTextureHelper.IsModAssetPath(path))
            return true;

        Texture2D? texture = ModTextureHelper.LoadTexture(path);
        if (texture != null)
        {
            CacheRef(__instance)[path] = texture;
            __result = texture;
            return false;
        }

        return true;
    }
}

/// <summary>
/// [REQUIRED] Add your character to the character list.
/// ModelDb.AllCharacters returns a hardcoded list of 5 characters.
/// This patch appends your character.
/// </summary>
[HarmonyPatch(typeof(ModelDb), "get_AllCharacters")]
internal static class AddCharacterPatch
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        CharacterModel? myChar =
            ModelDb.GetByIdOrNull<CharacterModel>(ModelDb.GetId(typeof(MyCharacter)));

        if (myChar == null) return;

        __result = __result.Concat(new[] { myChar }).Distinct().ToArray();
    }
}

/// <summary>
/// [REQUIRED] Load character select button icon.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), "get_CharacterSelectIcon")]
internal static class CharSelectIconPatch
{
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not MyCharacter) return true;

        Texture2D? texture = ModTextureHelper.LoadTexture(
            "res://images/packed/character_select/char_select_mycharacter.png");
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "get_CharacterSelectLockedIcon")]
internal static class CharSelectLockedIconPatch
{
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not MyCharacter) return true;

        Texture2D? texture = ModTextureHelper.LoadTexture(
            "res://images/packed/character_select/char_select_mycharacter_locked.png");
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

/// <summary>
/// [REQUIRED] Top panel character icon.
/// </summary>
[HarmonyPatch(typeof(CharacterModel), "get_IconTexture")]
internal static class IconTexturePatch
{
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not MyCharacter) return true;

        string path = $"res://images/ui/top_panel/character_icon_{__instance.Id.Entry.ToLower()}.png";
        Texture2D? texture = ModTextureHelper.LoadTexture(path);
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "get_IconOutlineTexture")]
internal static class IconOutlineTexturePatch
{
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is not MyCharacter) return true;

        string path = $"res://images/ui/top_panel/character_icon_{__instance.Id.Entry.ToLower()}_outline.png";
        Texture2D? texture = ModTextureHelper.LoadTexture(path);
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

[HarmonyPatch(typeof(CharacterModel), "get_Icon")]
internal static class IconScenePatch
{
    private static bool Prefix(CharacterModel __instance, ref Control __result)
    {
        if (__instance is not MyCharacter) return true;

        string path = $"res://images/ui/top_panel/character_icon_{__instance.Id.Entry.ToLower()}.png";
        Texture2D? texture = ModTextureHelper.LoadTexture(path);
        if (texture != null)
        {
            var rect = new TextureRect
            {
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                AnchorRight = 1.0f,
                AnchorBottom = 1.0f,
                GrowHorizontal = Control.GrowDirection.Both,
                GrowVertical = Control.GrowDirection.Both,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            __result = rect;
            return false;
        }
        return true;
    }
}

// ============================================================================
// CARD / RELIC / POWER ICON PATCHES
// ============================================================================

/// <summary>
/// [REQUIRED] Load card portraits for your character's cards.
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_Portrait")]
internal static class CardPortraitPatch
{
    private static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance.Pool is not MyCharacterCardPool) return true;

        Texture2D? texture = ModTextureHelper.LoadTexture(__instance.PortraitPath);
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

[HarmonyPatch(typeof(CardModel), "get_HasPortrait")]
internal static class CardHasPortraitPatch
{
    private static bool Prefix(CardModel __instance, ref bool __result)
    {
        if (__instance.Pool is not MyCharacterCardPool) return true;
        __result = ModTextureHelper.LoadTexture(__instance.PortraitPath) != null;
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), "get_HasBetaPortrait")]
internal static class CardHasBetaPortraitPatch
{
    private static bool Prefix(CardModel __instance, ref bool __result)
    {
        if (__instance.Pool is not MyCharacterCardPool) return true;
        __result = ModTextureHelper.LoadTexture(__instance.BetaPortraitPath) != null;
        return false;
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
internal static class CardReloadPatch
{
    private static readonly AccessTools.FieldRef<NCard, TextureRect> PortraitRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_portrait");

    private static void Postfix(NCard __instance)
    {
        CardModel? model = __instance.Model;
        if (model?.Pool is not MyCharacterCardPool) return;

        Texture2D? texture = ModTextureHelper.LoadTexture(model.PortraitPath);
        TextureRect? portrait = PortraitRef(__instance);
        if (texture != null && portrait != null)
            portrait.Texture = texture;
    }
}

/// <summary>
/// [REQUIRED] Load relic icons.
/// </summary>
[HarmonyPatch(typeof(RelicModel), "get_Icon")]
internal static class RelicIconPatch
{
    private static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        if (__instance is not MyCharacterRelic) return true;

        Texture2D? texture = ModTextureHelper.LoadTexture(__instance.PackedIconPath);
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

[HarmonyPatch(typeof(RelicModel), "get_IconOutline")]
internal static class RelicIconOutlinePatch
{
    private static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        if (__instance is not MyCharacterRelic) return true;

        string outlinePath = __instance.PackedIconPath.Replace("/relics/", "/relics/outline/");
        Texture2D? texture = ModTextureHelper.LoadTexture(outlinePath);
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

/// <summary>
/// [REQUIRED] Load power icons for your mod's powers.
/// </summary>
[HarmonyPatch(typeof(PowerModel), "get_Icon")]
internal static class PowerIconPatch
{
    private static readonly Assembly ModAssembly = typeof(MyCharacter).Assembly;

    private static bool Prefix(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance.GetType().Assembly != ModAssembly) return true;

        string path = $"res://images/powers/{__instance.Id.Entry.ToLower()}.png";
        Texture2D? texture = ModTextureHelper.LoadTexture(path);
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_BigIcon")]
internal static class PowerBigIconPatch
{
    private static readonly Assembly ModAssembly = typeof(MyCharacter).Assembly;

    private static bool Prefix(PowerModel __instance, ref Texture2D __result)
    {
        if (__instance.GetType().Assembly != ModAssembly) return true;

        string path = $"res://images/powers/{__instance.Id.Entry.ToLower()}.png";
        Texture2D? texture = ModTextureHelper.LoadTexture(path);
        if (texture != null) { __result = texture; return false; }
        return true;
    }
}

// ============================================================================
// SFX PATCHES (reuse existing character SFX)
// ============================================================================

[HarmonyPatch(typeof(CharacterModel), "get_AttackSfx")]
internal static class AttackSfxPatch
{
    private static void Postfix(CharacterModel __instance, ref string __result)
    {
        if (__instance is MyCharacter)
            __result = "event:/sfx/characters/ironclad/ironclad_attack";
    }
}

[HarmonyPatch(typeof(CharacterModel), "get_CastSfx")]
internal static class CastSfxPatch
{
    private static void Postfix(CharacterModel __instance, ref string __result)
    {
        if (__instance is MyCharacter)
            __result = "event:/sfx/characters/ironclad/ironclad_cast";
    }
}

[HarmonyPatch(typeof(CharacterModel), "get_DeathSfx")]
internal static class DeathSfxPatch
{
    private static void Postfix(CharacterModel __instance, ref string __result)
    {
        if (__instance is MyCharacter)
            __result = "event:/sfx/characters/ironclad/ironclad_die";
    }
}

// ============================================================================
// CRASH PREVENTION PATCHES
// ============================================================================

/// <summary>
/// [REQUIRED] Skip hardcoded progress checks that throw for unknown characters.
/// </summary>
[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
internal static class EliteEpochPatch
{
    private static bool Prefix(ProgressSaveManager __instance, MegaCrit.Sts2.Core.Entities.Players.Player localPlayer)
    {
        return localPlayer.Character is not MyCharacter;
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
internal static class BossEpochPatch
{
    private static bool Prefix(ProgressSaveManager __instance, MegaCrit.Sts2.Core.Entities.Players.Player localPlayer)
    {
        return localPlayer.Character is not MyCharacter;
    }
}

[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
internal static class CharUnlockEpochPatch
{
    private static bool Prefix(ProgressSaveManager __instance, MegaCrit.Sts2.Core.Entities.Players.Player localPlayer)
    {
        return localPlayer.Character is not MyCharacter;
    }
}

/// <summary>
/// [REQUIRED] Register mod model entries in network serialization cache.
/// Without this, multiplayer combat replays crash with ArgumentException.
/// </summary>
[HarmonyPatch(typeof(ModelIdSerializationCache), "Init")]
internal static class SerializationCachePatch
{
    private static void Postfix()
    {
        var entryMap = typeof(ModelIdSerializationCache)
            .GetField("_entryNameToNetIdMap", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as Dictionary<string, int>;
        var entryList = typeof(ModelIdSerializationCache)
            .GetField("_netIdToEntryNameMap", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as List<string>;
        var catMap = typeof(ModelIdSerializationCache)
            .GetField("_categoryNameToNetIdMap", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as Dictionary<string, int>;
        var catList = typeof(ModelIdSerializationCache)
            .GetField("_netIdToCategoryNameMap", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as List<string>;

        if (entryMap == null || entryList == null || catMap == null || catList == null)
            return;

        bool added = false;
        foreach (Type type in ReflectionHelper.GetSubtypesInMods<AbstractModel>())
        {
            ModelId id = ModelDb.GetId(type);
            if (!catMap.ContainsKey(id.Category))
            {
                catMap[id.Category] = catList.Count;
                catList.Add(id.Category);
                added = true;
            }
            if (!entryMap.ContainsKey(id.Entry))
            {
                entryMap[id.Entry] = entryList.Count;
                entryList.Add(id.Entry);
                added = true;
            }
        }

        if (added)
        {
            var entryBitProp = typeof(ModelIdSerializationCache)
                .GetProperty("EntryIdBitSize", BindingFlags.Static | BindingFlags.Public);
            var catBitProp = typeof(ModelIdSerializationCache)
                .GetProperty("CategoryIdBitSize", BindingFlags.Static | BindingFlags.Public);
            entryBitProp?.SetValue(null, Mathf.CeilToInt(Math.Log2(entryList.Count)));
            catBitProp?.SetValue(null, Mathf.CeilToInt(Math.Log2(catList.Count)));
        }
    }
}

/// <summary>
/// [RECOMMENDED] Handle TheArchitect WinRun null dialogue for modded characters.
/// TheArchitect only defines dialogues for base game characters; modded ones crash.
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Events.TheArchitect), "WinRun")]
internal static class ArchitectWinRunPatch
{
    private static bool Prefix(MegaCrit.Sts2.Core.Models.Events.TheArchitect __instance, ref Task __result)
    {
        var field = AccessTools.Field(typeof(MegaCrit.Sts2.Core.Models.Events.TheArchitect), "_dialogue");
        if (field?.GetValue(__instance) != null)
            return true;

        if (MegaCrit.Sts2.Core.Context.LocalContext.IsMe(__instance.Owner))
            MegaCrit.Sts2.Core.Runs.RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();

        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// [OPTIONAL] Character select background portrait.
/// Only needed if you want a custom portrait on the character select screen.
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectScreen), "SelectCharacter")]
internal static class CharSelectBgPatch
{
    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, Control> BgContainerRef =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, Control>("_bgContainer");

    private static void Postfix(
        NCharacterSelectScreen __instance,
        NCharacterSelectButton charSelectButton,
        CharacterModel characterModel)
    {
        if (characterModel is not MyCharacter) return;

        Control? bgContainer = BgContainerRef(__instance);
        if (bgContainer == null) return;

        Node? bg = bgContainer.GetNodeOrNull(characterModel.Id.Entry + "_bg");
        if (bg == null) return;

        TextureRect? portrait = bg.GetNodeOrNull<TextureRect>("Portrait");
        if (portrait != null)
        {
            Texture2D? texture = ModTextureHelper.LoadTexture(
                "res://images/ui/charSelect/mycharacterPortrait.jpg");
            if (texture != null)
                portrait.Texture = texture;
        }
    }
}
