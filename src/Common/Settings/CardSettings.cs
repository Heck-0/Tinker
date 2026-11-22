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

	/// <summary>Workshop: gold for an upgrade the roulette would win every time; riskier ones cost proportionally more.</summary>
	public int WorkshopGoldRate { get; set; } = 25;

	public int UpgradeDifficulty { get; set; } = 100;

	public int MaxChance { get; set; } = 95;

	public int MinChance { get; set; } = 3;

	public bool TraitsEnabled { get; set; } = true;

	public int TraitBase { get; set; } = 20;

	public int TraitMaxOnSuccess { get; set; } = 80;

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
			modSettings.MinChance = 5;
			modSettings.TraitBase = 30;
			modSettings.TraitMaxOnSuccess = 90;
			break;
		case SettingsPreset.Hard:
			modSettings.UpgradeDifficulty = 130;
			modSettings.MaxChance = 90;
			modSettings.MinChance = 2;
			modSettings.TraitBase = 15;
			modSettings.TraitMaxOnSuccess = 70;
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
		balance.MaxChance = (double)Clamp(MaxChance, 1, 100) / 100.0;
		balance.MinChance = Math.Min(balance.MaxChance, (double)Clamp(MinChance, 0, 100) / 100.0);
		balance.ExceptionalBase = (TraitsEnabled ? ((double)Clamp(TraitBase, 0, 100) / 100.0) : 0.0);
		balance.ExceptionalMax = (double)Clamp(TraitMaxOnSuccess, 0, 100) / 100.0;
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
		MaxChance = Clamp(MaxChance, 1, 100);
		MinChance = Clamp(MinChance, 0, MaxChance);
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
