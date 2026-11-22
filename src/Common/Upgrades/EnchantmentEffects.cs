using System;

namespace Tinker.Common;

public static class EnchantmentEffects
{
	public const string Prefix = "enchant:";

	public static bool Supported(string name)
	{
		switch (name)
		{
		default:
			return name == "Inky";
		case "Sown":
		case "Spiral":
		case "Glam":
		case "Swift":
		case "Sharp":
		case "Nimble":
		case "Adroit":
		case "Momentum":
		case "Vigorous":
			return true;
		}
	}

	public static string Key(string name)
	{
		return "enchant:" + name;
	}

	public static bool Known(string key)
	{
		if (key != null && key.StartsWith("enchant:", StringComparison.Ordinal))
		{
			return Supported(key.Substring("enchant:".Length));
		}
		return false;
	}

	public static string Family(string key)
	{
		switch (key.Substring("enchant:".Length))
		{
		case "Sown":
			return "energy";
		case "Spiral":
			return "enchantRepeat";
		case "Glam":
			return "enchantFirstRepeat";
		case "Swift":
			return "draw";
		case "Sharp":
			return "damage";
		case "Vigorous":
			return "enchantFirstDamage";
		case "Adroit":
		case "Nimble":
			return "block";
		case "Momentum":
			return "scaling";
		default:
			return "power";
		}
	}
}
