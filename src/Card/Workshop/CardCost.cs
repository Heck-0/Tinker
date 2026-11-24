using System;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Card;

/// <summary>
/// A card upgrade as the Workshop prices it (see <see cref="WorkshopPrice"/>): its size, the card's base value,
/// its upgrade history (strain) and its cost category.
/// </summary>
internal static class CardCost
{
	public static BalanceConfig Balance => Settings.Balance;

	public static UpgradeRequest BuildRequest(CardModel card, string attribute, int units)
	{
		if (units <= 0 || (AttributeDirection.SignOf(attribute) < 0 && units > CardAttributes.CurrentValue(card, attribute) - CardAttributes.Minimum(card, attribute)))
		{
			throw new ArgumentOutOfRangeException("units", "The upgrade goes below the attribute's minimum.");
		}
		if (AttributeDirection.SignOf(attribute) > 0 && units > CardAttributes.Maximum(card, attribute) - CardAttributes.CurrentValue(card, attribute))
		{
			throw new ArgumentOutOfRangeException("units", "The upgrade goes over the attribute's maximum.");
		}
		if (attribute == "cost" && (units <= 0 || units > CardAttributes.CurrentValue(card, attribute)))
		{
			throw new ArgumentOutOfRangeException("units", "The cost can't go below zero.");
		}
		CardMachineState cardMachineState = CardStateStore.Get(card);
		int num = CardAttributes.CurrentValue(card, attribute);
		return new UpgradeRequest(attribute, num, units, CardAttributes.NativeReferenceValue(card, attribute), BidirectionalEffects.Gain(card.GetType().Name, attribute, cardMachineState), Multiplier(attribute), UpgradeCost.Strain(cardMachineState, attribute, Reference, Multiplier, Balance, (string a) => CardAttributes.RiskFamily(card, a), (string a) => BidirectionalEffects.Axis(card.GetType().Name, a)), CardAttributes.RiskFamily(card, attribute));
		double Multiplier(string a)
		{
			if (!(BalanceFamilies.Of(a) == "damage"))
			{
				return 1.0;
			}
			return VarDirections.HitsFor(card);
		}
		int Reference(string a)
		{
			try
			{
				return CardAttributes.NativeReferenceValue(card, a);
			}
			catch (Exception)
			{
				return 0;
			}
		}
	}
}
