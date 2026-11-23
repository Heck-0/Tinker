using System;

namespace Tinker.Common;

/// <summary>
/// The Workshop's gold price for an upgrade. See docs/TINKER_DESIGN.md.
///
///   gold = max(1, round(rate × e^exponent))
///
/// where the exponent is the upgrade's cost (<see cref="UpgradeCost"/> for cards, Rules.Exponent for relics:
/// its size next to the base value, earlier upgrades and difficulty) and rate is the "Gold per upgrade"
/// setting. Small upgrades on a fresh card cost about the rate; each extra unit costs more, with no ceiling.
/// </summary>
public static class WorkshopPrice
{
    public static int Gold(double exponent, int rate)
    {
        double gold = rate * Math.Exp(exponent);
        if (!(gold < int.MaxValue))
        {
            return int.MaxValue;
        }
        return Math.Max(1, (int)Math.Round(gold, MidpointRounding.AwayFromZero));
    }
}
