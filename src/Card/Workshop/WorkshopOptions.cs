using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Tinker.Relic;

namespace Tinker.Card;

/// <summary>
/// What the Workshop sells and for how much gold: every card and relic upgrade, priced from the odds engine's
/// chance for the same upgrade (see <see cref="WorkshopPrice"/>).
/// </summary>
internal static class WorkshopOptions
{
    public static int Rate => Settings.Active.WorkshopGoldRate;

    public static bool CardUsable(CardModel card) => card != null && CardAttributes.SupportedFor(card).Any();

    public static bool RelicUsable(RelicModel relic) => Catalog.For(relic).Length != 0;

    /// <summary>The odds engine's chance for this card upgrade, in percent.</summary>
    public static int CardPercent(CardModel card, string attribute, int units)
    {
        return Odds.DisplayPercent(Odds.SuccessChance(CardOdds.BuildRequest(card, attribute, units), CardOdds.Balance));
    }

    public static int CardPrice(CardModel card, string attribute, int units)
    {
        try
        {
            return WorkshopPrice.Gold(CardPercent(card, attribute, units), Rate);
        }
        catch (ArgumentOutOfRangeException)
        {
            return int.MaxValue;
        }
    }

    /// <summary>The odds engine's chance for this relic upgrade, in percent.</summary>
    public static int RelicPercent(RelicModel relic, RelicAttribute attribute, int units)
    {
        return Rules.Percent(Catalog.Chance(relic, attribute, units));
    }

    public static int RelicPrice(Player player, RelicModel relic, RelicAttribute attribute, int units)
    {
        try
        {
            return WorkshopPrice.Gold(RelicPercent(relic, attribute, units), Rate);
        }
        catch (ArgumentOutOfRangeException)
        {
            return int.MaxValue;
        }
    }
}
