

namespace Tinker.Relic;

// Relic upgrade history (saved with the relic).

public sealed class UpgradeLedger
{
	public decimal Reference { get; set; }

	public int Gain { get; set; }

	public int Direction { get; set; } = 1;

	public string Family { get; set; } = "generic";
}
