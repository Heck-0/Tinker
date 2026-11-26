using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

public static class FixedEffects
{
	public static bool Known(string attribute)
	{
		switch (attribute)
		{
		default:
			return attribute == "effect:glass";
		case "effect:lightning":
		case "effect:frost":
		case "effect:dark":
		case "effect:plasma":
		case "effect:frostPerEnemy":
		case "effect:evokes":
		case "effect:extraEvokes":
		case "effect:extraLightning":
			return true;
		}
	}

	/// <summary>The orb and evoke effects a card offers, as attribute keys.</summary>
	public static IEnumerable<string> For(string card)
	{
		switch (card)
		{
		case "Zap":
		case "BallLightning":
			return new[] { "effect:lightning" };
		case "Coolheaded":
		case "ColdSnap":
		case "Glacier":
			return new[] { "effect:frost" };
		case "Darkness":
		case "Null":
		case "ShadowShield":
			return new[] { "effect:dark" };
		case "Fusion":
		case "Ignition":
		case "MeteorStrike":
			return new[] { "effect:plasma" };
		case "Rainbow":
			return new[] { "effect:lightning", "effect:frost", "effect:dark" };
		case "Chill":
			return new[] { "effect:frostPerEnemy" };
		case "Tempest":
			return new[] { "effect:extraLightning" };
		case "Glasswork":
			return new[] { "effect:glass" };
		case "MultiCast":
			return new[] { "effect:extraEvokes" };
		case "Dualcast":
		case "Shatter":
			return new[] { "effect:evokes" };
		default:
			return Array.Empty<string>();
		}
	}

	public static bool Supports(string card, string attribute)
	{
		return For(card).Contains(attribute);
	}

	public static int Native(string card, string attribute)
	{
		if (!(attribute == "effect:extraEvokes") && !(attribute == "effect:extraLightning"))
		{
			if (!(card == "MeteorStrike"))
			{
				if (!(card == "Glacier") && !(attribute == "effect:evokes"))
				{
					return 1;
				}
				return 2;
			}
			return 3;
		}
		return 0;
	}

	public static int Value(string card, string attribute, CardMachineState state)
	{
		if (!Supports(card, attribute))
		{
			throw new ArgumentException("Unsupported effect for " + card);
		}
		return checked(Native(card, attribute) + (state?.RawDelta(attribute) ?? 0));
	}
}
