using System;

namespace Tinker.Common;

public readonly struct UpgradeRequest
{
	public string Attribute { get; }

	public int Units { get; }

	public int ReferenceValue { get; }

	public int ExistingGain { get; }


	public double Multiplier { get; }

	public double OtherStrain { get; }

	public string RiskFamily { get; }

	public UpgradeRequest(string attribute, int currentValue, int units, int? referenceValue = null, int existingGain = 0, double multiplier = 1.0, double otherStrain = 0.0, string riskFamily = null)
	{
		Attribute = attribute;
		Units = units;

		ReferenceValue = referenceValue ?? currentValue;
		ExistingGain = existingGain;
		Multiplier = ((multiplier > 0.0) ? multiplier : 1.0);
		OtherStrain = ((otherStrain > 0.0) ? otherStrain : 0.0);
		RiskFamily = riskFamily;
	}
}
