using System;

namespace Tinker.Common;

/// <summary>
/// The Workshop's gold price for an upgrade. See docs/TINKER_DESIGN.md.
///
///   gold = max(1, round(rate / chance))      chance = maxChance × e^(−exponent), with no minimum
///
/// where the exponent is the odds engine's for the same upgrade (strain from earlier upgrades and difficulty
/// included), and rate is the "Gold per upgrade" setting. Small upgrades on a fresh card cost about the rate;
/// big ones, and upgrades on already-upgraded cards, cost more, without a ceiling: each extra unit costs more.
/// </summary>
public static class WorkshopPrice
{
    public static int Gold(double chance, int rate)
    {
        double gold = rate / chance;
        if (!(chance > 0.0) || !(gold < int.MaxValue))
        {
            return int.MaxValue;
        }
        return Math.Max(1, (int)Math.Round(gold, MidpointRounding.AwayFromZero));
    }
}
