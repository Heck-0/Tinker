using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Tinker.Card;

// Saved by class name: never rename it.
/// <summary>
/// The Workshop relic (clicking it opens the Workshop, see <see cref="WorkshopRelicPatch"/>). It also declares
/// CardUpgraderState, the saved-property name card upgrades are stored under, so the game gives that name a
/// network id for co-op (see ModEntry.RegisterNetwork). The Dial itself never sets it.
/// </summary>
public sealed class TinkerersDial : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Event;

	public override bool IsAllowedInShops => false;

	protected override string IconBaseName => "captains_wheel";

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string CardUpgraderState { get; set; }
}
