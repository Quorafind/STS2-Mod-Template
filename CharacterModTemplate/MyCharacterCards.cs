using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharacterMod;

// ============================================================================
// BASIC CARDS
// ============================================================================

/// <summary>
/// Basic Strike card.
/// DamageVar(baseDamage, ValueProp.Move) - Move means it's a card play attack.
/// OnUpgrade() modifies the dynamic vars for the upgraded version.
///
/// Key patterns for attack cards:
///   - TargetType.AnyEnemy for single-target attacks
///   - TargetType.AllEnemies for AOE attacks
///   - DamageCmd.Attack(damage).FromCard(this).Targeting(target).Execute(ctx)
///   - .WithHitFx("vfx/vfx_attack_slash") for hit visual effects
/// </summary>
public sealed class MyStrike : MyCharacterCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move)];

    public MyStrike()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

/// <summary>
/// Basic Defend card.
/// Key patterns for block cards:
///   - GainsBlock => true (tells UI to show block badge)
///   - TargetType.Self
///   - CreatureCmd.GainBlock(creature, BlockVar, cardPlay)
/// </summary>
public sealed class MyDefend : MyCharacterCard
{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5m, ValueProp.Move)];

    public MyDefend()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// ============================================================================
// SIGNATURE CARDS (your character's unique starting cards)
// ============================================================================

/// <summary>
/// Example signature attack. Replace with your character's unique mechanic.
/// Demonstrates: attack + apply power pattern.
/// </summary>
public sealed class SignatureStrike : MyCharacterCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("Strength", 1m)
    ];

    public SignatureStrike()
        : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // Deal damage
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        // Apply a buff to self (example: gain Strength via built-in power)
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

/// <summary>
/// Example signature skill. Demonstrates: block + draw cards.
/// </summary>
public sealed class SignatureSkill : MyCharacterCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public SignatureSkill()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// ============================================================================
// CARD IMPLEMENTATION COOKBOOK (commented examples for reference)
// ============================================================================

// --- AOE Attack ---
// public sealed class Cleave : MyCharacterCard
// {
//     protected override IEnumerable<DynamicVar> CanonicalVars =>
//         [new DamageVar(8m, ValueProp.Move)];
//
//     public Cleave() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
//
//     protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
//     {
//         await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
//             .TargetingAllEnemies()
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(ctx);
//     }
// }

// --- Exhaust card ---
// public sealed class Burn : MyCharacterCard
// {
//     public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
//     ...
// }

// --- Retain card ---
// public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

// --- X-cost card ---
// public sealed class Whirlwind : MyCharacterCard
// {
//     public override bool HasEnergyCostX => true;
//     public Whirlwind() : base(-1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
//     protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
//     {
//         int x = ResolveEnergyXValue(ctx);
//         for (int i = 0; i < x; i++) { ... }
//     }
// }

// --- Apply debuff to enemy ---
// await PowerCmd.Apply<WeaknessPower>(target, amount, Owner.Creature, this);

// --- Gain energy ---
// await PlayerCmd.GainEnergy(Owner, amount);

// --- Add generated card to hand ---
// CardModel card = Owner.Creature.CombatState!.CreateCard<SomeToken>(Owner);
// await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
