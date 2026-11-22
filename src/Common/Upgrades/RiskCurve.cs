using System;

namespace Tinker.Common;

/// <summary>
/// The upgrade odds formula, shared by both roulettes.
///
///   ratio  = amount / max(|reference|, floor)          how big the bet is next to the current value
///   risk   = weight × ratio ^ curve                     per cost category (balance.jsonc "families")
///   chance = clamp(maxChance × e^(−exponent), min, max) exponent = risk + strain (× difficulty, relic side)
///
/// Card Upgrader applies the difficulty setting by scaling the weights; Relic Upgrader multiplies the
/// exponent. Both read their categories from the same table (BalanceConfig, loaded from balance.jsonc).
/// The operations are in the same order as each half's original code, so results are unchanged.
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

    public static double Chance(double maxChance, double minChance, double exponent)
    {
        return Math.Max(minChance, Math.Min(maxChance, maxChance * Math.Exp(0.0 - exponent)));
    }
}
