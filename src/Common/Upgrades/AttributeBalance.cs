namespace Tinker.Common;

/// <summary>One cost category's numbers (balance.jsonc "families").</summary>
public sealed class AttributeBalance
{
	public double Weight { get; set; } = 1.0;

	public double Curve { get; set; } = 1.0;

	public double ReferenceFloor { get; set; } = 5.0;
}
