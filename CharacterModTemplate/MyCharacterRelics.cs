using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharacterMod;

/// <summary>
/// Starting relic. Every character needs one.
/// Constructor param is the image asset filename (without .png).
///
/// Relic lifecycle hooks:
///   - BeforeCombatStart() - once when combat begins
///   - AfterPlayerTurnStart() - each turn
///   - AfterPlayerTurnEnd() - each turn
///   - AfterCardPlayed() - each card played
///   - AfterCreatureDied() - when any creature dies
///   - Flash() - triggers the relic flash animation
/// </summary>
public sealed class StarterRelic : MyCharacterRelic
{
    public StarterRelic() : base("starter_relic") { }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Block", 3m)];

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature.CombatState == null) return;

        Flash();
        // Example: gain some block at start of combat
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].BaseValue, 0, null);
    }
}

// ============================================================================
// RELIC IMPLEMENTATION COOKBOOK (commented examples)
// ============================================================================

// --- Relic that triggers every turn ---
// public sealed class TurnRelic : MyCharacterRelic
// {
//     public TurnRelic() : base("turn_relic") { }
//     public override RelicRarity Rarity => RelicRarity.Common;
//
//     public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
//     {
//         if (player != Owner) return;
//         Flash();
//         await CardPileCmd.DrawCards(ctx, Owner, 1);
//     }
// }

// --- Relic that reacts to card plays ---
// public sealed class CardPlayRelic : MyCharacterRelic
// {
//     public CardPlayRelic() : base("card_play_relic") { }
//     public override RelicRarity Rarity => RelicRarity.Uncommon;
//
//     public override async Task AfterCardPlayed(CardPlay cardPlay, CardModel model)
//     {
//         if (model.Type == CardType.Attack)
//         {
//             Flash();
//             await CreatureCmd.GainBlock(Owner.Creature, 3, ValueProp.Relic);
//         }
//     }
// }
