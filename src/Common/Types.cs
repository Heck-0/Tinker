namespace Tinker.Common;

// Shared by the card and relic halves. Names and values are unchanged, so settings files (enum names) and
// saved run rules (enum numbers) read the same as before.

/// <summary>Settings preset. Custom = values that match no preset.</summary>
public enum SettingsPreset
{
    Normal,
    Easy,
    Hard,
    Custom
}
