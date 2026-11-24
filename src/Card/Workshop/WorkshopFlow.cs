using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using Tinker.Relic;

namespace Tinker.Card;

/// <summary>
/// A Workshop purchase, as sent to the other players in co-op:
/// <list type="bullet">
/// <item><see cref="WorkshopFlow.OpCard"/>: card upgrade. Index = deck position, Attribute = upgrade, Units = size.</item>
/// <item><see cref="WorkshopFlow.OpRelic"/>: relic upgrade. Id = relic id, Index = copy, Attribute = upgrade, Units = size.</item>
/// <item><see cref="WorkshopFlow.OpTrait"/>: card trait. Index = deck position, Id = trait id.</item>
/// <item><see cref="WorkshopFlow.OpBoon"/>: relic boon. Id = relic id, Index = copy, Attribute = boon (index in Boons.All).</item>
/// </list>
/// </summary>
internal sealed record WorkshopPurchase(int Op, int Index, string Id, int Attribute, int Units);

/// <summary>
/// The Workshop, opened by clicking Tinkerer's Dial outside combat: buy card and relic upgrades, card traits and
/// relic boons with gold until you leave. Nothing is random: what you pick is what you get.
///
/// Co-op: the buyer's game applies each purchase, then sends it to the other players (<see cref="WorkshopMessage"/>),
/// whose games apply the same purchase and gold loss. Nobody has to wait for the Workshop to close.
/// </summary>
internal static class WorkshopFlow
{
    public const int OpCard = 1;

    public const int OpRelic = 2;

    public const int OpTrait = 3;

    public const int OpBoon = 4;

    private static bool _busy;

    /// <summary>Why the Workshop can't open right now ("" = busy, ignore the click), or null if it can.</summary>
    public static string BlockedReason(Player player)
    {
        if (player?.RunState == null || NRun.Instance == null)
        {
            return Lang.T("Only during a run.");
        }
        if (CombatManager.Instance.IsInProgress)
        {
            return Lang.T("The Workshop can't be used in combat.");
        }
        if (_busy || WorkshopScreen.Current != null)
        {
            return "";
        }
        return null;
    }

    /// <summary>The local player's Workshop. Returns when they leave.</summary>
    public static async Task OpenAsync(Player player)
    {
        if (!LocalContext.IsMe(player) || BlockedReason(player) != null)
        {
            return;
        }
        _busy = true;
        try
        {
            Tinker.Common.Rules.GameRules.DumpIfRequested();
            CardModel lastCard = null;
            RelicModel lastRelic = null;
            while (true)
            {
                WorkshopScreen screen = WorkshopScreen.Open(player, player.Gold, lastCard, lastRelic);
                WorkshopChoice choice;
                try
                {
                    choice = await screen.ChooseAsync();
                }
                finally
                {
                    screen.Close();
                }
                if (choice == null)
                {
                    return;
                }
                WorkshopPurchase purchase = ToPurchase(player, choice);
                if (purchase == null)
                {
                    continue;
                }
                lastCard = choice.Card;
                lastRelic = choice.Relic;
                await Apply(player, purchase, remote: false);
                if (player.RunState.Players.Count > 1)
                {
                    CustomMessageWrapper.Send(WorkshopMessage.For(player, purchase));
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.Error("Workshop error: " + ex);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Another player's purchase, sent by their game.</summary>
    public static void ApplyRemote(ulong playerId, WorkshopPurchase purchase)
    {
        Player player = RunManager.Instance?.DebugOnlyGetState()?.Players.FirstOrDefault((Player p) => p.NetId == playerId);
        if (player == null || LocalContext.IsMe(player))
        {
            return;
        }
        TaskHelper.RunSafely(Apply(player, purchase, remote: true));
    }

    private static WorkshopPurchase ToPurchase(Player player, WorkshopChoice choice)
    {
        if (choice.Relic != null)
        {
            int copy = CopyIndex(player, choice.Relic);
            if (choice.Boon != null)
            {
                int boon = Array.IndexOf(Boons.All, choice.Boon);
                return (boon < 0) ? null : new WorkshopPurchase(OpBoon, copy, choice.Relic.Id.Entry, boon, 0);
            }
            return (choice.RelicAttributeIndex < 0) ? null : new WorkshopPurchase(OpRelic, copy, choice.Relic.Id.Entry, choice.RelicAttributeIndex, choice.RelicUnits);
        }
        int deckIndex = player.Deck.Cards.ToList().IndexOf(choice.Card);
        if (deckIndex < 0)
        {
            return null;
        }
        if (choice.Trait != null)
        {
            return new WorkshopPurchase(OpTrait, deckIndex, choice.Trait, 0, 0);
        }
        int attributeIndex = CardAttributes.SupportedFor(choice.Card).ToList().IndexOf(choice.Request.Attribute);
        return (attributeIndex < 0) ? null : new WorkshopPurchase(OpCard, deckIndex, "", attributeIndex, choice.Request.Units);
    }

    /// <summary>Applies a purchase; false if it couldn't be (logged).</summary>
    private static Task<bool> Apply(Player player, WorkshopPurchase purchase, bool remote)
    {
        return purchase.Op switch
        {
            OpCard => BuyCard(player, purchase, remote),
            OpRelic => BuyRelic(player, purchase, remote),
            OpTrait => BuyTrait(player, purchase, remote),
            OpBoon => BuyBoon(player, purchase, remote),
            _ => Task.FromResult(false),
        };
    }

    /// <summary>Relics are sent by id and copy number, so a different relic order on another game can't pick the wrong one.</summary>
    private static int CopyIndex(Player player, RelicModel relic)
    {
        return player.Relics.Where((RelicModel r) => r.Id.Entry == relic.Id.Entry).ToList().IndexOf(relic);
    }

    private static RelicModel FindRelic(Player player, string id, int copy)
    {
        List<RelicModel> copies = player.Relics.Where((RelicModel r) => r.Id.Entry == id).ToList();
        return (copy >= 0 && copy < copies.Count) ? copies[copy] : null;
    }

    private static CardModel FindCard(Player player, int index)
    {
        IReadOnlyList<CardModel> cards = player.Deck.Cards;
        if (index < 0 || index >= cards.Count)
        {
            ModEntry.Error($"Workshop purchase for card {index} outside the deck ({cards.Count}); ignored.");
            return null;
        }
        return cards[index];
    }

    /// <summary>
    /// Takes the gold. Another player's purchase was already checked by their game, so it goes through even if
    /// this game counts less gold for them (it then takes what there is).
    /// </summary>
    private static async Task<bool> Pay(Player player, int price, bool remote, string what)
    {
        if (price < 0 || price == int.MaxValue || (!remote && player.Gold < price))
        {
            ModEntry.Error($"Workshop purchase without enough gold: {what} costs {price}, have {player.Gold}.");
            return false;
        }
        if (price > 0)
        {
            await PlayerCmd.LoseGold(Math.Min(price, player.Gold), player, GoldLossType.Spent);
        }
        return true;
    }

    private static void Bought(Player player, string what, int price)
    {
        ModEntry.Info($"Workshop, floor {player.RunState?.TotalFloor}: {what} for {price} gold.");
    }

    private static async Task<bool> BuyCard(Player player, WorkshopPurchase purchase, bool remote)
    {
        CardModel card = FindCard(player, purchase.Index);
        if (card == null || purchase.Units < 1)
        {
            return false;
        }
        List<string> attributes = CardAttributes.SupportedFor(card).ToList();
        if (purchase.Attribute < 0 || purchase.Attribute >= attributes.Count)
        {
            ModEntry.Error($"Workshop purchase with invalid attribute {purchase.Attribute} on {card.Id.Entry}; ignored.");
            return false;
        }
        string attribute = attributes[purchase.Attribute];
        int price = WorkshopOptions.CardPrice(card, attribute, purchase.Units);
        string what = $"{card.Id.Entry} {attribute} +{purchase.Units}";
        if (!await Pay(player, price, remote, what))
        {
            return false;
        }
        CardMachineState state = CardStateStore.GetOrCreate(card);
        CardAttributes.ApplyRaw(card, attribute, state.ApplySuccess(attribute, purchase.Units));
        CardStateStore.Set(card, state);
        Bought(player, what, price);
        return true;
    }

    private static async Task<bool> BuyRelic(Player player, WorkshopPurchase purchase, bool remote)
    {
        RelicModel relic = FindRelic(player, purchase.Id, purchase.Index);
        if (relic == null)
        {
            ModEntry.Error($"Workshop purchase for relic {purchase.Id} #{purchase.Index}, not found; ignored.");
            return false;
        }
        RelicAttribute[] attributes = Catalog.For(relic);
        if (purchase.Attribute < 0 || purchase.Attribute >= attributes.Length)
        {
            ModEntry.Error($"Workshop purchase with invalid attribute {purchase.Attribute} on {relic.Id.Entry}; ignored.");
            return false;
        }
        RelicAttribute attribute = attributes[purchase.Attribute];
        int units = purchase.Units;
        if (units < 1 || units > Catalog.MaxUnits(relic, attribute))
        {
            ModEntry.Error($"Workshop purchase with {units} units out of range on {relic.Id.Entry}; ignored.");
            return false;
        }
        int price = WorkshopOptions.RelicPrice(player, relic, attribute, units);
        string what = $"{relic.Id.Entry} {attribute.Key} +{units}";
        if (!await Pay(player, price, remote, what))
        {
            return false;
        }
        Store.Upgrade(relic, attribute, units);
        Bought(player, what, price);
        return true;
    }

    private static async Task<bool> BuyTrait(Player player, WorkshopPurchase purchase, bool remote)
    {
        CardModel card = FindCard(player, purchase.Index);
        if (card == null)
        {
            return false;
        }
        CardMachineState state = CardStateStore.GetOrCreate(card);
        if (state.Property != null || !ExceptionalEffects.ApplicableTo(card).Any((ExceptionalDefinition d) => d.Id == purchase.Id))
        {
            ModEntry.Error($"Workshop purchase: {card.Id.Entry} can't have trait {purchase.Id}; ignored.");
            return false;
        }
        int price = WorkshopOptions.TraitPrice;
        string what = $"{card.Id.Entry} propiedad {purchase.Id}";
        if (!await Pay(player, price, remote, what))
        {
            return false;
        }
        ExceptionalEffects.Grant(card, state, purchase.Id);
        CardStateStore.Set(card, state);
        Bought(player, what, price);
        return true;
    }

    private static async Task<bool> BuyBoon(Player player, WorkshopPurchase purchase, bool remote)
    {
        RelicModel relic = FindRelic(player, purchase.Id, purchase.Index);
        string boon = (purchase.Attribute >= 0 && purchase.Attribute < Boons.All.Length) ? Boons.All[purchase.Attribute] : null;
        if (relic == null || boon == null || !Boons.ForSale(relic).Contains(boon))
        {
            ModEntry.Error($"Workshop purchase: boon {purchase.Attribute} not available for {purchase.Id} #{purchase.Index}; ignored.");
            return false;
        }
        int price = WorkshopOptions.BoonPrice;
        string what = $"{relic.Id.Entry} bufo {boon}";
        if (!await Pay(player, price, remote, what))
        {
            return false;
        }
        Boons.Grant(relic, boon);
        Bought(player, what, price);
        return true;
    }
}
