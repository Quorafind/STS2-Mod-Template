using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MyCharacterMod;

/// <summary>
/// Base card class for your character. All character-specific cards should extend this.
/// Key override: PortraitPath points to your mod's card art directory.
///
/// Card portrait images go in: images/packed/card_portraits/mycharacter/{id}.png
/// where {id} is the lowercase ModelId entry (e.g. "my_strike").
/// </summary>
public abstract class MyCharacterCard : CardModel
{
    protected MyCharacterCard(
        int canonicalEnergyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary = true)
        : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override CardPoolModel Pool => ModelDb.CardPool<MyCharacterCardPool>();

    public override string PortraitPath =>
        $"res://images/packed/card_portraits/mycharacter/{Id.Entry.ToLower()}.png";

    public override string BetaPortraitPath => PortraitPath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath];
}

/// <summary>
/// Base relic class for your character.
/// Relic icons go in: images/relics/{assetName}.png
/// Outline icons go in: images/relics/outline/{assetName}.png
/// </summary>
public abstract class MyCharacterRelic(string assetName) : RelicModel
{
    public override string PackedIconPath => $"res://images/relics/{assetName}.png";

    protected override string PackedIconOutlinePath =>
        $"res://images/relics/outline/{assetName}.png";

    protected override string BigIconPath => PackedIconPath;
}
