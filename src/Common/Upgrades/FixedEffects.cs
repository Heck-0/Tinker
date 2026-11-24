using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

public static class FixedEffects
{
	public const string Prefix = "effect:";

	public const string Lightning = "effect:lightning";

	public const string Frost = "effect:frost";

	public const string Dark = "effect:dark";

	public const string Plasma = "effect:plasma";

	public const string FrostPerEnemy = "effect:frostPerEnemy";

	public const string Evokes = "effect:evokes";

	public const string ExtraEvokes = "effect:extraEvokes";

	public const string ExtraLightning = "effect:extraLightning";

	public const string Glass = "effect:glass";

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

	public static IEnumerable<string> For(string card)
	{
		if (card == null)
		{
			yield break;
		}
		switch (card.Length)
		{
		default:
			yield break;
		case 8:
		{
			char c = card[1];
			if ((uint)c <= 103u)
			{
				if (c != 'a')
				{
					if (c != 'g' || !(card == "Ignition"))
					{
						yield break;
					}
					goto IL_030c;
				}
				if (!(card == "Darkness"))
				{
					yield break;
				}
				goto IL_02ec;
			}
			if (c != 'o')
			{
				if (c != 'u' || !(card == "Dualcast"))
				{
					yield break;
				}
				break;
			}
			if (!(card == "ColdSnap"))
			{
				yield break;
			}
			goto IL_02cc;
		}
		case 7:
			switch (card[0])
			{
			default:
				yield break;
			case 'G':
				break;
			case 'R':
				if (card == "Rainbow")
				{
					yield return "effect:lightning";
					yield return "effect:frost";
					yield return "effect:dark";
				}
				yield break;
			case 'S':
				goto IL_01ef;
			case 'T':
				if (card == "Tempest")
				{
					yield return "effect:extraLightning";
				}
				yield break;
			}
			if (!(card == "Glacier"))
			{
				yield break;
			}
			goto IL_02cc;
		case 12:
		{
			char c = card[0];
			if (c != 'M')
			{
				if (c != 'S' || !(card == "ShadowShield"))
				{
					yield break;
				}
				goto IL_02ec;
			}
			if (!(card == "MeteorStrike"))
			{
				yield break;
			}
			goto IL_030c;
		}
		case 9:
			switch (card[0])
			{
			case 'G':
				if (card == "Glasswork")
				{
					yield return "effect:glass";
				}
				break;
			case 'M':
				if (card == "MultiCast")
				{
					yield return "effect:extraEvokes";
				}
				break;
			}
			yield break;
		case 3:
			if (!(card == "Zap"))
			{
				yield break;
			}
			goto IL_02ac;
		case 13:
			if (!(card == "BallLightning"))
			{
				yield break;
			}
			goto IL_02ac;
		case 10:
			if (!(card == "Coolheaded"))
			{
				yield break;
			}
			goto IL_02cc;
		case 4:
			if (!(card == "Null"))
			{
				yield break;
			}
			goto IL_02ec;
		case 6:
			if (!(card == "Fusion"))
			{
				yield break;
			}
			goto IL_030c;
		case 5:
			{
				if (card == "Chill")
				{
					yield return "effect:frostPerEnemy";
				}
				yield break;
			}
			IL_02cc:
			yield return "effect:frost";
			yield break;
			IL_02ac:
			yield return "effect:lightning";
			yield break;
			IL_02ec:
			yield return "effect:dark";
			yield break;
			IL_030c:
			yield return "effect:plasma";
			yield break;
			IL_01ef:
			if (!(card == "Shatter"))
			{
				yield break;
			}
			break;
		}
		yield return "effect:evokes";
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
