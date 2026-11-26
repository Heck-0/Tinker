using System;
using System.Text.Json;

namespace Tinker.Relic;

public sealed class ModSettings
{
	public SettingsPreset Preset { get; set; }

	public int Difficulty { get; set; } = 100;

	public ModSettings Copy()
	{
		return FromJson(ToJson());
	}

	public string ToJson()
	{
		return JsonSerializer.Serialize(this);
	}

	public static ModSettings FromJson(string json)
	{
		ModSettings? modSettings = JsonSerializer.Deserialize<ModSettings>(json) ?? throw new FormatException("Empty settings");
		modSettings.Validate();
		return modSettings;
	}

	public void Validate()
	{
		if (!Enum.IsDefined(Preset))
		{
			throw new FormatException("Invalid Preset");
		}
		if (!double.IsFinite(Difficulty) || Difficulty < 25 || Difficulty > 300)
		{
			throw new FormatException("Invalid Difficulty");
		}
	}

	public static ModSettings ForPreset(SettingsPreset preset)
	{
		ModSettings modSettings = new ModSettings
		{
			Preset = preset
		};
		if (preset == SettingsPreset.Easy)
		{
			modSettings.Difficulty = 70;
		}
		if (preset == SettingsPreset.Hard)
		{
			modSettings.Difficulty = 130;
		}
		return modSettings;
	}
}
