using System.Reflection;

namespace Tinker;

/// <summary>
/// Settings that mean the same thing on both roulettes, kept in sync across the two halves.
///
/// Each half still keeps its own settings object underneath: Card Upgrader's (CardSettings,
/// applied live) and Relic Upgrader's (Tinker.Relic.ModSettings, frozen into each run's save). That
/// keeps old saves and both halves' balance code working unchanged. What is shared is written to both
/// objects from one place, and the preset is computed for the pair.
///
/// Both halves' presets already used identical numbers for every shared value, so applying a preset
/// gives the same result as before.
/// </summary>
internal static class Settings
{
    private static CardSettings Card => Tinker.Card.Settings.Local;

    private static Tinker.Relic.ModSettings Relic => Tinker.Relic.Settings.Local;

    /// <summary>The preset both halves match, or Custom.</summary>
    public static SettingsPreset Preset => Card.Preset == Relic.Preset ? Card.Preset : SettingsPreset.Custom;

    public static void ApplyPreset(SettingsPreset preset)
    {
        if (preset == SettingsPreset.Custom)
        {
            return;
        }
        Card.CopyFrom(CardSettings.ForPreset(preset));
        Tinker.Relic.Settings.Local = Tinker.Relic.ModSettings.ForPreset(preset);
        CopySharedToRelic();
        Refresh();
    }

    public static void RestoreDefaults()
    {
        Card.CopyFrom(CardSettings.Default());
        Tinker.Relic.Settings.Local = new Tinker.Relic.ModSettings();
        CopySharedToRelic();
        Refresh();
    }

    // ---- shared values: read from the card side, written to both ----

    public static int Difficulty
    {
        get => Card.UpgradeDifficulty;
        set { Card.UpgradeDifficulty = value; Relic.Difficulty = System.Math.Clamp(value, 25, 300); }
    }

    public static int MaxChance
    {
        get => Card.MaxChance;
        set { Card.MaxChance = value; Relic.MaxChance = System.Math.Clamp(value, 10, 100); }
    }

    public static int MinChance
    {
        get => Card.MinChance;
        set { Card.MinChance = value; Relic.MinChance = System.Math.Clamp(value, 0, 50); }
    }

    /// <summary>Makes the relic side's shared values equal the card side's.</summary>
    public static void CopySharedToRelic()
    {
        Difficulty = Difficulty;
        MaxChance = MaxChance;
        MinChance = MinChance;
    }

    /// <summary>
    /// Call after any change: sanitizes the card side (which also redetects its preset and refreshes
    /// its graphs) and redetects the relic side's preset.
    /// </summary>
    public static void Refresh()
    {
        Tinker.Card.Settings.LocalChanged();
        Relic.Preset = DetectRelicPreset(Relic);
    }

    private static SettingsPreset DetectRelicPreset(Tinker.Relic.ModSettings settings)
    {
        foreach (SettingsPreset preset in new[] { SettingsPreset.Normal, SettingsPreset.Easy, SettingsPreset.Hard })
        {
            Tinker.Relic.ModSettings reference = Tinker.Relic.ModSettings.ForPreset(preset);
            bool same = true;
            foreach (PropertyInfo property in typeof(Tinker.Relic.ModSettings).GetProperties())
            {
                if (property.Name != nameof(Tinker.Relic.ModSettings.Preset) && !Equals(property.GetValue(settings), property.GetValue(reference)))
                {
                    same = false;
                    break;
                }
            }
            if (same)
            {
                return preset;
            }
        }
        return SettingsPreset.Custom;
    }
}
