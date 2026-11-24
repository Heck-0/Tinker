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

	/// <summary>Workshop: whether it sells the game's shop enchantments for cards.</summary>
	public bool EnchantmentsEnabled { get; set; } = true;

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

	public static CardSettings ForPreset(SettingsPreset preset)
	{
		CardSettings modSettings = Default();
		modSettings.Preset = preset;
		switch (preset)
		{
		case SettingsPreset.Easy:
			modSettings.UpgradeDifficulty = 70;
			break;
		case SettingsPreset.Hard:
			modSettings.UpgradeDifficulty = 130;
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
	}

	public string ToJson()
	{
		return JsonSerializer.Serialize(this, Json);
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
			modSettings.Sanitize();
			return modSettings;
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
