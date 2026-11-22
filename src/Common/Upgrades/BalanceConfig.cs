using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Tinker.Common;

/// <summary>
/// The odds engine's numbers. Defaults come from content/balance.jsonc, embedded in the DLL; the copy next to
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
			?? throw new InvalidOperationException("Falta el recurso " + ResourceName + " en el DLL.");
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	});

	public double MinChance { get; set; }

	public double MaxChance { get; set; }

	public double StrainFactor { get; set; }

	public double ExceptionalBase { get; set; }

	public double ExceptionalMax { get; set; }

	public Dictionary<string, AttributeBalance> Families { get; } = new Dictionary<string, AttributeBalance>(StringComparer.Ordinal);

	/// <summary>The built-in balance (the embedded balance.jsonc).</summary>
	public BalanceConfig()
	{
		List<string> warnings = Apply(DefaultText.Value, addFamilies: true);
		if (warnings.Count != 0)
		{
			throw new InvalidOperationException("El balance integrado no es válido: " + string.Join("; ", warnings));
		}
	}

	public double ExceptionalChanceFor(double successChance)
	{
		double num = Math.Max(0.0, Math.Min(1.0, successChance));
		if (num <= 0.0)
		{
			return ExceptionalMax;
		}
		return Math.Max(0.0, Math.Min(ExceptionalMax, ExceptionalBase * (1.0 - num) / num));
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
		throw new KeyNotFoundException("Sin balance para el atributo " + attribute);
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
			warnings.Add($"no es JSON válido o un valor no es un número ({ex.Path}): {ex.Message}; se ignora el archivo.");
			return warnings;
		}
		if (file == null)
		{
			warnings.Add("archivo vacío.");
			return warnings;
		}
		double min = file.MinChance ?? MinChance;
		double max = file.MaxChance ?? MaxChance;
		if (min < 0.0 || min >= 1.0 || max <= min || max > 1.0)
		{
			warnings.Add($"minChance/maxChance fuera de rango ({min}, {max}); se ignoran.");
		}
		else
		{
			MinChance = min;
			MaxChance = max;
		}
		Set(file.StrainFactor, v => v >= 0.0 && v <= 5.0, v => StrainFactor = v, "strainFactor (entre 0 y 5)", warnings);
		if (file.Exceptional != null)
		{
			double traitBase = file.Exceptional.Base ?? ExceptionalBase;
			double traitMax = file.Exceptional.MaxPerSuccess ?? ExceptionalMax;
			if (traitBase < 0.0 || traitBase > 1.0 || traitMax < 0.0 || traitMax > 1.0)
			{
				warnings.Add("exceptional fuera de rango (base y maxPerSuccess entre 0 y 1); se ignora.");
			}
			else
			{
				ExceptionalBase = traitBase;
				ExceptionalMax = traitMax;
			}
		}
		foreach (KeyValuePair<string, FamilyShape> item in file.Families ?? new Dictionary<string, FamilyShape>())
		{
			if (item.Value == null)
			{
				warnings.Add("La familia «" + item.Key + "» está vacía; se ignora.");
				continue;
			}
			if (!Families.TryGetValue(item.Key, out var family))
			{
				if (!addFamilies)
				{
					warnings.Add("Familia desconocida «" + item.Key + "»; se ignora.");
					continue;
				}
				family = Families[item.Key] = new AttributeBalance();
			}
			Set(item.Value.Weight, v => v > 0.0 && v <= 20.0, v => family.Weight = v, item.Key + ".weight", warnings);
			Set(item.Value.Curve, v => v > 0.0 && v <= 5.0, v => family.Curve = v, item.Key + ".curve", warnings);
			Set(item.Value.ReferenceFloor, v => v >= 1.0 && v <= 100.0, v => family.ReferenceFloor = v, item.Key + ".referenceFloor", warnings);
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
			warnings.Add(name + " fuera de rango; se ignora.");
		}
	}

	// The file's shape. Unknown keys ("version", the old "maxUnits") are ignored.
	private sealed class BalanceFileShape
	{
		public double? MinChance { get; set; }

		public double? MaxChance { get; set; }

		public double? StrainFactor { get; set; }

		public ExceptionalShape Exceptional { get; set; }

		public Dictionary<string, FamilyShape> Families { get; set; }
	}

	private sealed class ExceptionalShape
	{
		public double? Base { get; set; }

		public double? MaxPerSuccess { get; set; }
	}

	private sealed class FamilyShape
	{
		public double? Weight { get; set; }

		public double? Curve { get; set; }

		public double? ReferenceFloor { get; set; }
	}
}
