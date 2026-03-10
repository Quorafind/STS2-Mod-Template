using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharacterMod;

/// <summary>
/// Example buff power. Powers are applied to creatures and persist until removed.
///
/// PowerType: Buff or Debuff (affects UI color)
/// StackType:
///   - Counter: stacks add/subtract amount (Strength, Dexterity)
///   - Single: only one instance allowed (stances)
///   - None: no stacking behavior
///
/// Common hooks:
///   - AfterApplied() / AfterRemoved() - setup/cleanup
///   - ModifyDamageMultiplicative() - multiply outgoing/incoming damage
///   - ModifyDamageAdditive() - add/subtract flat damage
///   - ModifyBlockAdditive() / ModifyBlockMultiplicative() - modify block gained
///   - AfterPlayerTurnStart() / AfterPlayerTurnEnd()
///   - AfterCardPlayed()
///   - AfterAttacked() / AfterDamaged()
/// </summary>
public sealed class ExampleBuff : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // If true, power icon is hidden from the UI (used for invisible stance markers)
    protected override bool IsVisibleInternal => true;

    /// <summary>
    /// Modify outgoing damage. Return a multiplier (1.0 = no change).
    /// 'dealer' is who deals damage, 'target' is who receives it.
    /// </summary>
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // Only boost powered card attacks from the owner
        bool isPoweredAttack = props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
        if (dealer == Owner && isPoweredAttack)
        {
            // Each stack adds 10% damage
            return 1m + (Amount * 0.1m);
        }

        return 1m;
    }
}

/// <summary>
/// Example debuff power applied to enemies.
/// </summary>
public sealed class ExampleDebuff : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// Reduce block gained by the debuffed creature.
    /// ModifyBlockAdditive returns the flat amount to add/subtract from block.
    /// </summary>
    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target == Owner)
        {
            return -Amount;
        }

        return 0m;
    }
}
