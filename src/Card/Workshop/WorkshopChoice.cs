using MegaCrit.Sts2.Core.Models;
using Tinker.Common;

namespace Tinker.Card;

/// <summary>What the player picked in the Workshop: a card upgrade, a relic upgrade, or an enchantment for a card.</summary>
internal sealed class WorkshopChoice
{
	public CardModel Card { get; init; }

	public UpgradeRequest Request { get; init; }

	public RelicModel Relic { get; init; }

	/// <summary>Relic purchase: the attribute's index in Catalog.For(Relic).</summary>
	public int RelicAttributeIndex { get; init; }

	public int RelicUnits { get; init; }

	/// <summary>Enchantment bought for <see cref="Card"/> (its id), instead of an upgrade.</summary>
	public string Enchantment { get; init; }

	/// <summary>The bought enchantment's amount.</summary>
	public int EnchantmentAmount { get; init; }
}
