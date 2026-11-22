using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Relic;

public static class Rules
{
	/// <summary>
	/// Cost categories: the same table as the card roulette (balance.jsonc, loaded by BalanceFile). Relic
	/// Upgrader used to keep its own copy; its 20 categories had identical numbers.
	/// </summary>
	public static IReadOnlyDictionary<string, AttributeBalance> Families => Tinker.Card.BalanceFile.Current.Families;

	public static double Risk(string family, decimal reference, int gained)
	{
		return RiskCurve.Risk(Families[family], gained, (double)reference);
	}

	/// <summary>The upgrade's chance (for boons); with <paramref name="forPrice"/>, without the minimum, for the Workshop price.</summary>
	public static double Chance(string family, decimal reference, int gained, int units, double otherStrain = 0.0, bool forPrice = false)
	{
		if (units < 1 || gained < 0 || !double.IsFinite(otherStrain) || otherStrain < 0.0)
		{
			throw new ArgumentOutOfRangeException("units");
		}
		ModSettings modSettings = Settings.Active;
		double num = (double)modSettings.MaxChance / 100.0;
		double min = Math.Min((double)modSettings.MinChance / 100.0, num);
		double exponent = (Risk(family, reference, gained + units) + otherStrain) * (double)modSettings.Difficulty / 100.0;
		return forPrice ? RiskCurve.Unclamped(num, exponent) : RiskCurve.Chance(num, min, exponent);
	}

	public static int Percent(double chance)
	{
		return (int)Math.Round(chance * 100.0, MidpointRounding.AwayFromZero);
	}


	public static double BoonChance(double baseChance, ModSettings? settings = null)
	{
		double num = Math.Clamp(baseChance, 0.0, 1.0);
		ModSettings modSettings = settings ?? Settings.Active;
		if (!Boons.All.Any(modSettings.AllowsBoon) || modSettings.BoonBase == 0)
		{
			return 0.0;
		}
		if (!(num <= 0.0))
		{
			return Math.Clamp((double)modSettings.BoonBase / 100.0 * (1.0 - num) / num, 0.0, (double)modSettings.BoonMax / 100.0);
		}
		return (double)modSettings.BoonMax / 100.0;
	}
}
