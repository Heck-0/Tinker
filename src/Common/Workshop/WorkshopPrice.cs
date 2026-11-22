using System;

namespace Tinker.Common;

/// <summary>
/// The Workshop's gold price for an upgrade. See docs/TINKER_DESIGN.md.
///
///   gold = max(1, round(rate × 100 / chance%))
///
/// where chance% is what the roulette's odds engine gives the same upgrade (strain from earlier upgrades,
/// difficulty and support items included), and rate is the "Gold per upgrade" setting. Small upgrades on a
/// fresh card cost about the rate; big ones, and upgrades on already-upgraded cards, cost more.
/// </summary>
public static class WorkshopPrice
{
    public static int Gold(int percent, int rate)
    {
        if (percent <= 0)
        {
            return int.MaxValue;
        }
        return Math.Max(1, (int)Math.Round(rate * 100.0 / percent, MidpointRounding.AwayFromZero));
    }
}
