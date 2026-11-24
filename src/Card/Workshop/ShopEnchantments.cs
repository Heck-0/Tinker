using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Card;

/// <summary>
/// The enchantments the Workshop sells for cards: the game's own enchantments, each with its price and amount
/// from the "enchantments" table in balance.jsonc (keyed by class name, e.g. "Sharp"). Only those the card can
/// take are offered (the game's <c>CanEnchant</c>);
/// one per card, like the game: a card that already has an enchantment can't take another (its amount can still
/// be upgraded). An enchantment whose entry names a relic is offered only while the player has it.
/// </summary>
internal static class ShopEnchantments
{
    private static Dictionary<string, EnchantmentModel> _models;

    /// <summary>The game's enchantments by class name.</summary>
    private static Dictionary<string, EnchantmentModel> Models => _models ??= ModelDb.DebugEnchantments
        .GroupBy((EnchantmentModel e) => e.GetType().Name, StringComparer.Ordinal)
        .ToDictionary((IGrouping<string, EnchantmentModel> g) => g.Key, (IGrouping<string, EnchantmentModel> g) => g.First(), StringComparer.Ordinal);

    /// <summary>Everything in the table that the game has and that has a price, in table order.</summary>
    public static IEnumerable<(EnchantmentModel Model, EnchantmentOffer Offer)> All()
    {
        foreach (KeyValuePair<string, EnchantmentOffer> entry in Settings.Balance.Enchantments)
        {
            if (entry.Value.Price > 0 && Models.TryGetValue(entry.Key, out EnchantmentModel model))
            {
                yield return (model, entry.Value);
            }
        }
    }

    /// <summary>The enchantments this card can take now (none if it already has one).</summary>
    public static List<(EnchantmentModel Model, int Amount)> For(CardModel card)
    {
        List<(EnchantmentModel, int)> list = new List<(EnchantmentModel, int)>();
        if (card == null || card.Enchantment != null)
        {
            return list;
        }
        foreach ((EnchantmentModel model, EnchantmentOffer offer) in All())
        {
            if (offer.Relic != null && card.Owner?.Relics.Any((RelicModel r) => r.GetType().Name == offer.Relic) != true)
            {
                continue;
            }
            try
            {
                if (model.CanEnchant(card))
                {
                    list.Add((model, offer.Amount));
                }
            }
            catch (Exception ex)
            {
                ModEntry.Warn("Could not check enchantment " + model.GetType().Name + " on " + card.Id.Entry + ": " + ex.Message);
            }
        }
        return list;
    }

    /// <summary>The offer for an enchantment class name, or null.</summary>
    public static (EnchantmentModel Model, int Amount)? Find(string name)
    {
        foreach ((EnchantmentModel model, EnchantmentOffer offer) in All())
        {
            if (model.GetType().Name == name)
            {
                return (model, offer.Amount);
            }
        }
        return null;
    }

    /// <summary>The gold price, from the table.</summary>
    public static int Price(EnchantmentModel model)
    {
        if (!Settings.Balance.Enchantments.TryGetValue(model.GetType().Name, out EnchantmentOffer offer))
        {
            return int.MaxValue;
        }
        return Math.Max(0, offer.Price);
    }

    /// <summary>The enchantment's name, with its amount when the game shows one (e.g. "Sharp 2").</summary>
    public static string Name(EnchantmentModel model, int amount)
    {
        return model.Title.GetFormattedText() + (model.ShowAmount ? " " + amount : "");
    }

    /// <summary>What the enchantment does at this amount, in the game's words.</summary>
    public static string Description(EnchantmentModel model, int amount)
    {
        EnchantmentModel e = model;
        if (!model.IsMutable)
        {
            e = model.ToMutable();
            e.Amount = amount;
            e.RecalculateValues();
        }
        return e.DynamicDescription.GetFormattedText();
    }
}
