using Godot;
using MegaCrit.Sts2.Core.Models;

namespace MyCharacterMod;

/// <summary>
/// Card pool defines which cards belong to your character.
/// GenerateAllCards() returns ALL cards (basic, common, uncommon, rare, tokens).
///
/// Key properties:
///   - Title: lowercase identifier used in UI
///   - EnergyColorName: references an existing energy orb color
///     Options: "ironclad", "silent", "defect", "necrobinder", "architect"
///   - CardFrameMaterialPath: card border style
///     Options: "card_frame_red", "card_frame_green", "card_frame_blue",
///              "card_frame_pink", "card_frame_colorless"
/// </summary>
public sealed class MyCharacterCardPool : CardPoolModel
{
    public override string Title => "mycharacter";

    // Reuse an existing energy color (change to match your character's theme)
    public override string EnergyColorName => "ironclad";

    public override string CardFrameMaterialPath => "card_frame_blue";

    public override Color DeckEntryCardColor => new("4FC3F7");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        return
        [
            // -- Basic --
            ModelDb.Card<MyStrike>(),
            ModelDb.Card<MyDefend>(),
            ModelDb.Card<SignatureStrike>(),
            ModelDb.Card<SignatureSkill>(),

            // -- Common --
            // Add your common cards here: ModelDb.Card<YourCard>(),

            // -- Uncommon --
            // Add your uncommon cards here

            // -- Rare --
            // Add your rare cards here
        ];
    }
}

/// <summary>
/// Relic pool for your character.
/// </summary>
public sealed class MyCharacterRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "ironclad";

    public override Color LabOutlineColor => new("4FC3F7");

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            ModelDb.Relic<StarterRelic>(),
            // Add more relics: ModelDb.Relic<YourRelic>(),
        ];
    }
}

/// <summary>
/// Potion pool (empty by default - add custom potions if needed).
/// </summary>
public sealed class MyCharacterPotionPool : PotionPoolModel
{
    public override string EnergyColorName => "ironclad";

    public override Color LabOutlineColor => new("4FC3F7");

    protected override IEnumerable<PotionModel> GenerateAllPotions()
    {
        return Array.Empty<PotionModel>();
    }
}
