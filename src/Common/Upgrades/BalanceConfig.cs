using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Tinker.Common;

/// <summary>
/// The price curve's numbers. Defaults come from content/balance.jsonc, embedded in the DLL; the copy next to
/// the DLL (if any) is applied on top through <see cref="ApplyOverrides"/>, so it only needs what it changes.
/// </summary>
public sealed class BalanceConfig
{
	private const string ResourceName = "Tinker.balance.jsonc";

	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	private static readonly Lazy<string> DefaultText = new Lazy<string>(() =>
	{
		using Stream stream = typeof(BalanceConfig).Assembly.GetManifestResourceStream(ResourceName)
			?? throw new InvalidOperationException("Missing resource " + ResourceName + " in the DLL.");
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	});

	public double StrainFactor { get; set; }

	public Dictionary<string, AttributeBalance> Families { get; } = new Dictionary<string, AttributeBalance>(StringComparer.Ordinal);

	/// <summary>The enchantments the Workshop sells, by the game's enchantment id ("Sharp"), with price and amount.</summary>
	public Dictionary<string, EnchantmentOffer> Enchantments { get; } = new Dictionary<string, EnchantmentOffer>(StringComparer.Ordinal);

	/// <summary>The built-in balance (the embedded balance.jsonc).</summary>
	public BalanceConfig()
	{
		List<string> warnings = Apply(DefaultText.Value, addFamilies: true);
		if (warnings.Count != 0)
		{
			throw new InvalidOperationException("The built-in balance is invalid: " + string.Join("; ", warnings));
		}
	}


	public AttributeBalance For(string attribute)
	{
		if (Families.TryGetValue(BalanceFamilies.Of(attribute), out var value))
		{
			return value;
		}
		if (Families.TryGetValue("generic", out var value2))
		{
			return value2;
		}
		throw new KeyNotFoundException("No balance for attribute " + attribute);
	}

	public AttributeBalance For(string attribute, string family)
	{
		if (family == null || !Families.TryGetValue(family, out var value))
		{
			return For(attribute);
		}
		return value;
	}

	/// <summary>Applies a balance.jsonc on top; returns warnings for what was ignored.</summary>
	public List<string> ApplyOverrides(string json) => Apply(json, addFamilies: false);

	private List<string> Apply(string json, bool addFamilies)
	{
		List<string> warnings = new List<string>();
		BalanceFileShape file;
		try
		{
			file = JsonSerializer.Deserialize<BalanceFileShape>(json, Options);
		}
		catch (JsonException ex)
		{
			warnings.Add($"not valid JSON, or a value is not a number ({ex.Path}): {ex.Message}; the file is ignored.");
			return warnings;
		}
		if (file == null)
		{
			warnings.Add("empty file.");
			return warnings;
		}
		Set(file.StrainFactor, v => v >= 0.0 && v <= 5.0, v => StrainFactor = v, "strainFactor (0 to 5)", warnings);

		foreach (KeyValuePair<string, FamilyShape> item in file.Families ?? new Dictionary<string, FamilyShape>())
		{
			if (item.Value == null)
			{
				warnings.Add("Family \"" + item.Key + "\" is empty; ignored.");
				continue;
			}
			if (!Families.TryGetValue(item.Key, out var family))
			{
				if (!addFamilies)
				{
					warnings.Add("Unknown family \"" + item.Key + "\"; ignored.");
					continue;
				}
				family = Families[item.Key] = new AttributeBalance();
			}
			Set(item.Value.Weight, v => v > 0.0 && v <= 20.0, v => family.Weight = v, item.Key + ".weight", warnings);
			Set(item.Value.Curve, v => v > 0.0 && v <= 5.0, v => family.Curve = v, item.Key + ".curve", warnings);
			Set(item.Value.ReferenceFloor, v => v >= 1.0 && v <= 100.0, v => family.ReferenceFloor = v, item.Key + ".referenceFloor", warnings);
		}
		foreach (KeyValuePair<string, EnchantmentShape> item in file.Enchantments ?? new Dictionary<string, EnchantmentShape>())
		{
			if (item.Value == null)
			{
				warnings.Add("Enchantment \"" + item.Key + "\" is empty; ignored.");
				continue;
			}
			if (!Enchantments.TryGetValue(item.Key, out var offer))
			{
				if (!addFamilies)
				{
					warnings.Add("Unknown enchantment \"" + item.Key + "\"; ignored.");
					continue;
				}
				offer = Enchantments[item.Key] = new EnchantmentOffer();
			}
			Set(item.Value.Price, v => v <= 99999.0, v => offer.Price = (int)Math.Round(v), item.Key + ".price", warnings);
			Set(item.Value.Amount, v => v >= 1.0 && v <= 999.0, v => offer.Amount = (int)Math.Round(v), item.Key + ".amount", warnings);
			if (item.Value.Relic != null)
			{
				offer.Relic = (item.Value.Relic.Length == 0) ? null : item.Value.Relic;
			}
		}
		return warnings;
	}

	private static void Set(double? value, Func<double, bool> valid, Action<double> set, string name, List<string> warnings)
	{
		if (!value.HasValue)
		{
			return;
		}
		if (valid(value.Value))
		{
			set(value.Value);
		}
		else
		{
			warnings.Add(name + " out of range; ignored.");
		}
	}

	// The file's shape. Unknown keys ("version", and the old "minChance", "maxChance", "exceptional", "maxUnits") are ignored.
	private sealed class BalanceFileShape
	{
		public double? StrainFactor { get; set; }

		public Dictionary<string, FamilyShape> Families { get; set; }

		public Dictionary<string, EnchantmentShape> Enchantments { get; set; }
	}

	private sealed class EnchantmentShape
	{
		public double? Price { get; set; }

		public double? Amount { get; set; }

		public string Relic { get; set; }
	}

	private sealed class FamilyShape
	{
		public double? Weight { get; set; }

		public double? Curve { get; set; }

		public double? ReferenceFloor { get; set; }
	}
}

/// <summary>One enchantment the Workshop sells: its price in gold at Normal, its amount, and the relic it needs (if any).</summary>
public sealed class EnchantmentOffer
{
	public int Price { get; set; }

	public int Amount { get; set; } = 1;

	public string Relic { get; set; }
}
