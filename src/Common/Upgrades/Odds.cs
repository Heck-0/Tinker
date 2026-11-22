using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

public static class Odds
{
	public static double SuccessChance(UpgradeRequest request, BalanceConfig config)
	{
		return RiskCurve.Chance(config.MaxChance, config.MinChance, Exponent(request, config));
	}

	/// <summary>The chance the Workshop prices from: like <see cref="SuccessChance"/>, without the minimum.</summary>
	public static double PriceChance(UpgradeRequest request, BalanceConfig config)
	{
		return RiskCurve.Unclamped(config.MaxChance, Exponent(request, config));
	}

	private static double Exponent(UpgradeRequest request, BalanceConfig config)
	{
		if (request.Units <= 0)
		{
			throw new ArgumentOutOfRangeException("request", "Units debe ser > 0");
		}
		if (request.ExistingGain < 0)
		{
			throw new ArgumentOutOfRangeException("request", "ExistingGain debe ser >= 0");
		}
		AttributeBalance attributeBalance = config.For(request.Attribute, request.RiskFamily);
		double num2 = request.Units;
		double risk = RiskCurve.Risk(attributeBalance, request.Multiplier * ((double)request.ExistingGain + num2), (double)request.ReferenceValue);
		return risk + request.OtherStrain;
	}

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

	public static int DisplayPercent(double chance)
	{
		return (int)Math.Round(chance * 100.0, MidpointRounding.AwayFromZero);
	}

}
