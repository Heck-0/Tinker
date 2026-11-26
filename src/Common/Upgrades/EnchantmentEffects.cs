using System;

namespace Tinker.Common;

public static class EnchantmentEffects
{
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

	public static string Key(string name) => UpgradeKey.Of(UpgradeKey.Kinds.Enchant, name);

	public static bool Known(string key)
	{
		UpgradeKey parsed = UpgradeKey.Parse(key);
		return parsed.Kind == UpgradeKey.Kinds.Enchant && Supported(parsed.Name);
	}

	public static string Family(string key)
	{
		switch (UpgradeKey.Parse(key).Name)
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
