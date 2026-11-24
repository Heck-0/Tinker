using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Tinker.Relic;

namespace Tinker.Card;

/// <summary>
/// What the Workshop sells and for how much gold: card and relic upgrades (priced by <see cref="WorkshopPrice"/>),
/// and card enchantments (each with its own price, <see cref="ShopEnchantments"/>).
/// </summary>
internal static class WorkshopOptions
{
    public static int Rate => Settings.Active.WorkshopGoldRate;

    public static bool CardUsable(CardModel card) => card != null && (CardAttributes.SupportedFor(card).Any() || (Settings.Active.EnchantmentsEnabled && ShopEnchantments.For(card).Count > 0));

    public static bool RelicUsable(RelicModel relic) => Catalog.For(relic).Length != 0;


    public static int CardPrice(CardModel card, string attribute, int units)
    {
        try
        {
            return WorkshopPrice.Gold(UpgradeCost.Exponent(CardCost.BuildRequest(card, attribute, units), CardCost.Balance), Rate);
        }
        catch (ArgumentOutOfRangeException)
        {
            return int.MaxValue;
        }
    }

    public static int RelicPrice(Player player, RelicModel relic, RelicAttribute attribute, int units)
    {
        try
        {
            return WorkshopPrice.Gold(Catalog.Exponent(relic, attribute, units), Rate);
        }
        catch (ArgumentOutOfRangeException)
        {
            return int.MaxValue;
        }
    }
}
