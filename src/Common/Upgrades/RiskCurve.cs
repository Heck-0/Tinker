using System;

namespace Tinker.Common;

/// <summary>
/// The cost curve, shared by cards and relics:
///
///   ratio = amount / max(|reference|, floor)     how big the upgrade is next to the base value
///   risk  = weight × ratio ^ curve                per cost category (balance.jsonc "families")
///
/// The Workshop price is rate × e^(risk + strain) (see <see cref="WorkshopPrice"/>). Cards apply the difficulty
/// setting by scaling the weights; relics multiply the exponent. Both read the same table (BalanceConfig,
/// loaded from balance.jsonc).
/// </summary>
public static class RiskCurve
{
    public static double Ratio(double amount, double reference, double floor)
    {
        return amount / Math.Max(Math.Abs(reference), floor);
    }

    public static double Risk(double weight, double curve, double ratio)
    {
        return weight * Math.Pow(ratio, curve);
    }

    public static double Risk(AttributeBalance family, double amount, double reference)
    {
        return Risk(family.Weight, family.Curve, Ratio(amount, reference, family.ReferenceFloor));
    }
}
