using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Relic;

/// <summary>
/// An upgradable relic value. Most count in whole units; a fraction (a multiplier like Bowler Hat's ×1.25) moves
/// by <see cref="Step"/> per unit, and its price counts units from <see cref="Neutral"/>, the "no effect" value.
/// </summary>
public sealed record RelicAttribute(string Key, string Family, int Direction = 1, decimal Maximum = 10000m, string? LabelKey = null)
{
	/// <summary>How much one unit changes the value.</summary>
	public decimal Step { get; init; } = 1m;

	/// <summary>The value that means "no effect": prices measure the value's distance from it, in units.</summary>
	public decimal Neutral { get; init; }

	/// <summary>The lowest value (for "lower is better" values, and the save check).</summary>
	public decimal Minimum { get; init; } = 1m;

	/// <summary>The value as the price formula's reference: units away from <see cref="Neutral"/>.</summary>
	public decimal PriceReference(decimal value) => Math.Abs(value - Neutral) / Step;

	/// <summary>The value after buying <paramref name="units"/> units.</summary>
	public decimal After(decimal value, int units) => value + Direction * units * Step;


	public string Label
	{
		get
		{
			if (!(LabelKey == "drawLoss"))
			{
				if (!(Key == "Discount") || Direction >= 0)
				{
					if (!(Key == "Stars") || Direction >= 0)
					{
						if (!(Key == "Gold") || Direction >= 0)
						{
							if (!(Key == "Energy") || Direction >= 0)
							{
								if (!(Key == "Cards") || Direction >= 0)
								{
									if (!(Key == "Damage") || Direction >= 0)
									{
										if (!(Key == "Turns") || !(Family == "duration"))
										{
											return Text.Attribute(Key);
										}
										return Text.T("Duration (turns)");
									}
									return Text.T("Self damage");
								}
								return Text.T("Cards required");
							}
							return Text.T("Energy required");
						}
						return Text.T("Gold paid");
					}
					return Text.T("Stars required");
				}
				return Text.T("Price (%)");
			}
			return Text.T("Cards lost");
		}
	}

	public int Limit(RelicModel relic)
	{
		decimal baseValue = relic.DynamicVars[Key].BaseValue;
		return (int)Math.Max(0m, Math.Floor(((Direction < 0) ? (baseValue - Minimum) : (Maximum - baseValue)) / Step));
	}

	public decimal Value(RelicModel relic)
	{
		return relic.DynamicVars[Key].BaseValue;
	}
}
