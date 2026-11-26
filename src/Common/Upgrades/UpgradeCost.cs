using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

/// <summary>
/// How costly a card upgrade is: the exponent the Workshop price grows with (see <see cref="WorkshopPrice"/>).
/// It is the upgrade's own risk (its size next to the card's base value, in its cost category) plus the strain
/// from the card's upgrades in other values.
/// </summary>
public static class UpgradeCost
{
	public static double Exponent(UpgradeRequest request, BalanceConfig config)
	{
		if (request.Units <= 0)
		{
			throw new ArgumentOutOfRangeException("request", "Units must be > 0");
		}
		if (request.ExistingGain < 0)
		{
			throw new ArgumentOutOfRangeException("request", "ExistingGain must be >= 0");
		}
		AttributeBalance family = config.For(request.Attribute, request.RiskFamily);
		double risk = RiskCurve.Risk(family, request.Multiplier * (request.ExistingGain + (double)request.Units), request.ReferenceValue);
		return risk + request.OtherStrain;
	}

	/// <summary>The card's upgrades in values other than <paramref name="attribute"/>, which make further upgrades pricier.</summary>
	public static double Strain(CardMachineState state, string attribute, Func<string, int> referenceOf, Func<string, double> multiplierOf, BalanceConfig config, Func<string, string> familyOf = null, Func<string, string> axisOf = null)
	{
		if (state == null || attribute == null)
		{
			return 0.0;
		}
		double num = 0.0;
		foreach (IGrouping<string, KeyValuePair<string, AttributeLedger>> item in from e in state.Ledgers
			group e by axisOf?.Invoke(e.Key) ?? e.Key)
		{
			if (!(item.Key == (axisOf?.Invoke(attribute) ?? attribute)))
			{
				KeyValuePair<string, AttributeLedger> keyValuePair = item.First();
				int num2 = item.Sum((KeyValuePair<string, AttributeLedger> e) => e.Value?.Gain ?? 0);
				if (num2 > 0)
				{
					AttributeBalance attributeBalance = config.For(keyValuePair.Key, familyOf?.Invoke(keyValuePair.Key));
					double val = multiplierOf?.Invoke(keyValuePair.Key) ?? 1.0;
					num += RiskCurve.Risk(attributeBalance, Math.Max(1.0, val) * (double)num2, (double)referenceOf(keyValuePair.Key));
				}
			}
		}
		return num * config.StrainFactor;
	}
}
