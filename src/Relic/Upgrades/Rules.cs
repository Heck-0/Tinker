using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Relic;

public static class Rules
{
	/// <summary>Cost categories: the same table cards use (balance.jsonc, loaded by BalanceFile).</summary>
	public static IReadOnlyDictionary<string, AttributeBalance> Families => Tinker.Card.BalanceFile.Current.Families;

	public static double Risk(string family, decimal reference, int gained)
	{
		return RiskCurve.Risk(Families[family], gained, (double)reference);
	}

	/// <summary>The upgrade's cost exponent for the Workshop price: its risk plus strain, times the difficulty.</summary>
	public static double Exponent(string family, decimal reference, int gained, int units, double otherStrain = 0.0)
	{
		if (units < 1 || gained < 0 || !double.IsFinite(otherStrain) || otherStrain < 0.0)
		{
			throw new ArgumentOutOfRangeException("units");
		}
		return (Risk(family, reference, gained + units) + otherStrain) * Settings.Active.Difficulty / 100.0;
	}
}
