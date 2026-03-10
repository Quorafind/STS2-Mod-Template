using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace MyCharacterMod;

/// <summary>
/// Character definition. Key overrides:
///   - StartingHp / StartingGold
///   - CardPool / RelicPool / PotionPool (connect to your pools)
///   - StartingDeck / StartingRelics
///   - NameColor / DialogueColor / EnergyLabelOutlineColor (theming)
///   - CharacterSelectIconPath / MapMarkerPath (asset paths)
///   - GenerateAnimator (Spine animation state machine)
///
/// IMPORTANT: Class name becomes the ModelId entry (e.g. "CHARACTER.MY_CHARACTER").
/// If a base-game class shares the same Name, you'll get DuplicateModelException.
/// Use a _P suffix (like Strike_P) to avoid collisions. Check:
///   _tools/sts2_decomp/MegaCrit.Sts2.Core.Models.Characters/
/// </summary>
public sealed class MyCharacter : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Masculine;

    // Which existing character must be cleared to unlock this one (null = always unlocked)
    protected override CharacterModel? UnlocksAfterRunAs => null;

    // Theme color shown on character name in UI
    public override Color NameColor => new("4FC3F7");

    public override int StartingHp => 75;
    public override int StartingGold => 99;

    // Link to your custom pools
    public override CardPoolModel CardPool => ModelDb.CardPool<MyCharacterCardPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MyCharacterPotionPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MyCharacterRelicPool>();

    // Starting deck - 4 Strikes, 4 Defends, + your signature cards
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<MyStrike>(),
        ModelDb.Card<MyStrike>(),
        ModelDb.Card<MyStrike>(),
        ModelDb.Card<MyStrike>(),
        ModelDb.Card<MyDefend>(),
        ModelDb.Card<MyDefend>(),
        ModelDb.Card<MyDefend>(),
        ModelDb.Card<MyDefend>(),
        ModelDb.Card<SignatureStrike>(),
        ModelDb.Card<SignatureSkill>()
    ];

    // Starting relic
    public override IReadOnlyList<RelicModel> StartingRelics =>
        [ModelDb.Relic<StarterRelic>()];

    // Animation timing
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;

    // UI colors
    public override Color EnergyLabelOutlineColor => new("1A5276FF");
    public override Color DialogueColor => new("1A3C5E");
    public override Color MapDrawingColor => new("4FC3F7");
    public override Color RemoteTargetingLineColor => new("81D4FA");
    public override Color RemoteTargetingLineOutline => new("1A5276FF");

    // Asset paths (these will be loaded via mod PCK)
    protected override string CharacterSelectIconPath =>
        "res://images/packed/character_select/char_select_mycharacter.png";

    protected override string CharacterSelectLockedIconPath =>
        "res://images/packed/character_select/char_select_mycharacter_locked.png";

    // Reuse an existing map marker (or provide your own)
    protected override string MapMarkerPath =>
        "res://images/packed/map/icons/map_marker_ironclad.png";

    // Reuse existing SFX (or provide your own via FMOD)
    public override string CharacterSelectSfx =>
        "event:/sfx/characters/ironclad/ironclad_select";

    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_attack_blunt"
        ];
    }

    /// <summary>
    /// Builds the Spine animation state machine for combat.
    /// If you only have a static sprite (no Spine data), use a simple Idle-only setup.
    /// If you have proper Spine 4 animations, map them to game states here.
    /// </summary>
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        // Scale if needed (STS1 skeletons are smaller than STS2)
        if (controller.BoundObject is Node2D spriteNode)
            spriteNode.Scale = Vector2.One * 1.0f;

        // Minimal animation setup - just Idle + Hit
        // Replace animation names with your actual Spine animation names
        var idle = new AnimState("Idle", isLooping: true);
        var cast = new AnimState("Hit");
        var attack = new AnimState("Hit");
        var hurt = new AnimState("Hit");
        var die = new AnimState("Hit");

        cast.NextState = idle;
        attack.NextState = idle;
        hurt.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        return animator;
    }
}
