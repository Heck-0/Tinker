using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Tinker.Common;

public sealed class CardSettings
{
	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = false
	};

	public SettingsPreset Preset { get; set; }

	/// <summary>Workshop: about what the smallest upgrade on a fresh card or relic costs; bigger ones cost more.</summary>
	public int WorkshopGoldRate { get; set; } = 25;

	public int UpgradeDifficulty { get; set; } = 100;

	public bool TraitsEnabled { get; set; } = true;

	/// <summary>Workshop: what a trait costs (bought directly, one per card).</summary>
	public int TraitPrice { get; set; } = 150;

	public HashSet<string> DisabledTraits { get; set; } = new HashSet<string>(StringComparer.Ordinal);

	public static CardSettings Default()
	{
		return new CardSettings();
	}

	public CardSettings Clone()
	{
		return FromJson(ToJson()) ?? Default();
	}

	public void CopyFrom(CardSettings other)
	{
		CardSettings obj = other.Clone();
		PropertyInfo[] properties = typeof(CardSettings).GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.CanRead && propertyInfo.CanWrite)
			{
				propertyInfo.SetValue(this, propertyInfo.GetValue(obj));
			}
		}
	}

	public bool IsTraitEnabled(string id)
	{
		if (TraitsEnabled)
		{
			return !DisabledTraits.Contains(id);
		}
		return false;
	}

	public static CardSettings ForPreset(SettingsPreset preset)
	{
		CardSettings modSettings = Default();
		modSettings.Preset = preset;
		switch (preset)
		{
		case SettingsPreset.Easy:
			modSettings.UpgradeDifficulty = 70;
			modSettings.TraitPrice = 100;
			break;
		case SettingsPreset.Hard:
			modSettings.UpgradeDifficulty = 130;
			modSettings.TraitPrice = 200;
			break;
		}
		return modSettings;
	}

	public SettingsPreset DetectPreset()
	{
		SettingsPreset[] array = new SettingsPreset[3]
		{
			SettingsPreset.Normal,
			SettingsPreset.Easy,
			SettingsPreset.Hard
		};
		foreach (SettingsPreset settingsPreset in array)
		{
			if (SameValues(ForPreset(settingsPreset)))
			{
				return settingsPreset;
			}
		}
		return SettingsPreset.Custom;
	}

	private bool SameValues(CardSettings other)
	{
		CardSettings modSettings = Clone();
		CardSettings modSettings2 = other.Clone();
		SettingsPreset preset = (modSettings2.Preset = SettingsPreset.Custom);
		modSettings.Preset = preset;
		return modSettings.ToJson() == modSettings2.ToJson();
	}

	public BalanceConfig ApplyTo(BalanceConfig balance)
	{
		double num = (double)Clamp(UpgradeDifficulty, 10, 400) / 100.0;
		foreach (AttributeBalance value in balance.Families.Values)
		{
			value.Weight *= num;
		}
		return balance;
	}

	private static int Clamp(int v, int min, int max)
	{
		return Math.Max(min, Math.Min(max, v));
	}

	public void Sanitize()
	{
		WorkshopGoldRate = Clamp(WorkshopGoldRate, 1, 200);
		UpgradeDifficulty = Clamp(UpgradeDifficulty, 10, 400);
		TraitPrice = Clamp(TraitPrice, 0, 9999);
		if (DisabledTraits == null)
		{
			HashSet<string> hashSet = (DisabledTraits = new HashSet<string>(StringComparer.Ordinal));
		}
	}

	public string ToJson()
	{
		CardSettings obj = (CardSettings)MemberwiseClone();
		obj.DisabledTraits = new HashSet<string>(DisabledTraits.OrderBy<string, string>((string x) => x, StringComparer.Ordinal), StringComparer.Ordinal);
		return JsonSerializer.Serialize(obj, Json);
	}

	public static CardSettings FromJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		try
		{
			CardSettings modSettings = JsonSerializer.Deserialize<CardSettings>(json, Json);
			if (modSettings == null)
			{
				return null;
			}
			modSettings.DisabledTraits = new HashSet<string>(modSettings.DisabledTraits ?? new HashSet<string>(), StringComparer.Ordinal);
			modSettings.Sanitize();
			return modSettings;
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
