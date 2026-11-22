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
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using Tinker.Relic;

namespace Tinker.Card;

/// <summary>A Workshop purchase: <see cref="Op"/> 1 = card (deck index, attribute index), 2 = relic (relic id + copy, attribute index).</summary>
internal sealed record WorkshopPurchase(int Op, int Index, string RelicId, int Attribute, int Units, int Floor);

/// <summary>
/// The Workshop, opened by clicking Tinkerer's Dial outside combat: buy card and relic upgrades with gold
/// until you leave. Purchases are certain; the only roll is the bonus trait / boon, seeded per purchase.
///
/// Co-op: the buyer's game applies each purchase, then sends it to the other players (<see cref="WorkshopMessage"/>),
/// whose games apply the same upgrade and gold loss. Nobody has to wait for the Workshop to close.
/// </summary>
internal static class WorkshopFlow
{
    public const int OpCard = 1;

    public const int OpRelic = 2;

    private static bool _busy;

    /// <summary>Seed id for relic boon rolls (kept from Relic Upgrader so the id stays stable).</summary>
    private static readonly ModelId RelicRollId = new ModelId("relicupgrader", "RELIC_ROULETTE");

    /// <summary>Why the Workshop can't open right now ("" = busy, ignore the click), or null if it can.</summary>
    public static string BlockedReason(Player player)
    {
        if (player?.RunState == null || NRun.Instance == null)
        {
            return Lang.T("Solo durante una partida.", "Only during a run.");
        }
        if (CombatManager.Instance.IsInProgress)
        {
            return Lang.T("El taller no se puede usar en combate.", "The Workshop can't be used in combat.");
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
            ModEntry.Error("Error en el taller: " + ex);
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
        int floor = player.RunState.TotalFloor;
        if (choice.Relic != null)
        {
            return (choice.RelicAttributeIndex < 0) ? null : new WorkshopPurchase(OpRelic, CopyIndex(player, choice.Relic), choice.Relic.Id.Entry, choice.RelicAttributeIndex, choice.RelicUnits, floor);
        }
        int deckIndex = player.Deck.Cards.ToList().IndexOf(choice.Card);
        int attributeIndex = CardAttributes.SupportedFor(choice.Card).ToList().IndexOf(choice.Request.Attribute);
        return (deckIndex < 0 || attributeIndex < 0) ? null : new WorkshopPurchase(OpCard, deckIndex, "", attributeIndex, choice.Request.Units, floor);
    }

    /// <summary>Applies a purchase; false if it couldn't be (logged).</summary>
    private static Task<bool> Apply(Player player, WorkshopPurchase purchase, bool remote)
    {
        return purchase.Op switch
        {
            OpCard => BuyCard(player, purchase, remote),
            OpRelic => BuyRelic(player, purchase, remote),
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

    /// <summary>
    /// Takes the gold. Another player's purchase was already checked by their game, so it goes through even if
    /// this game counts less gold for them (it then takes what there is).
    /// </summary>
    private static async Task<bool> Pay(Player player, int price, bool remote)
    {
        if (price <= 0 || price == int.MaxValue || (!remote && player.Gold < price))
        {
            return false;
        }
        await PlayerCmd.LoseGold(Math.Min(price, player.Gold), player, GoldLossType.Spent);
        return true;
    }

    /// <summary>Seed for the bonus trait / boon roll: the same on every co-op game (floor and gold are synced).</summary>
    private static ulong Seed(int floor, int goldBefore, int salt) => (ulong)(((long)floor * 1000003L + salt) ^ (7919L * goldBefore));

    private static async Task<bool> BuyCard(Player player, WorkshopPurchase purchase, bool remote)
    {
        IReadOnlyList<CardModel> cards = player.Deck.Cards;
        if (purchase.Index < 0 || purchase.Index >= cards.Count || purchase.Units < 1)
        {
            ModEntry.Error($"Compra del taller con carta {purchase.Index} fuera del mazo ({cards.Count}); se ignora.");
            return false;
        }
        CardModel card = cards[purchase.Index];
        List<string> attributes = CardAttributes.SupportedFor(card).ToList();
        if (purchase.Attribute < 0 || purchase.Attribute >= attributes.Count)
        {
            ModEntry.Error($"Compra del taller con atributo {purchase.Attribute} inválido en {card.Id.Entry}; se ignora.");
            return false;
        }
        string attribute = attributes[purchase.Attribute];
        int units = purchase.Units;
        int percent = WorkshopOptions.CardPercent(card, attribute, units);
        int price = WorkshopOptions.CardPrice(card, attribute, units);
        int goldBefore = player.Gold;
        if (!await Pay(player, price, remote))
        {
            ModEntry.Error($"Compra del taller sin oro suficiente: {card.Id.Entry} {attribute} cuesta {price}, hay {player.Gold}.");
            return false;
        }
        CardMachineState state = CardStateStore.GetOrCreate(card);
        CardAttributes.ApplyRaw(card, attribute, state.ApplySuccess(attribute, units));
        Rng rng = GameCompat.SeededRng(player, ModelDb.Relic<TinkerersDial>().Id, Seed(purchase.Floor, goldBefore, 17));
        ExceptionalDefinition trait = ExceptionalEffects.TryGrant(card, state, percent / 100.0, rng.NextDouble(), rng.NextDouble());
        CardStateStore.Set(card, state);
        ModEntry.Info($"Taller, planta {purchase.Floor}: {card.Id.Entry} {attribute} +{units} por {price} de oro ({percent} %){(trait == null ? "" : ", rasgo " + trait.Id)}.");
        return true;
    }

    private static async Task<bool> BuyRelic(Player player, WorkshopPurchase purchase, bool remote)
    {
        RelicModel relic = FindRelic(player, purchase.RelicId, purchase.Index);
        if (relic == null)
        {
            ModEntry.Error($"Compra del taller con reliquia {purchase.RelicId} #{purchase.Index} no encontrada; se ignora.");
            return false;
        }
        RelicAttribute[] attributes = Catalog.For(relic);
        if (purchase.Attribute < 0 || purchase.Attribute >= attributes.Length)
        {
            ModEntry.Error($"Compra del taller con atributo {purchase.Attribute} inválido en {relic.Id.Entry}; se ignora.");
            return false;
        }
        RelicAttribute attribute = attributes[purchase.Attribute];
        int units = purchase.Units;
        if (units < 1 || units > Catalog.MaxBet(relic, attribute))
        {
            ModEntry.Error($"Compra del taller con {units} unidades fuera de rango en {relic.Id.Entry}; se ignora.");
            return false;
        }
        double rawChance = Catalog.Chance(relic, attribute, units);
        int percent = Rules.Percent(rawChance);
        int price = WorkshopOptions.RelicPrice(player, relic, attribute, units);
        int goldBefore = player.Gold;
        if (!await Pay(player, price, remote))
        {
            ModEntry.Error($"Compra del taller sin oro suficiente: {relic.Id.Entry} {attribute.Key} cuesta {price}, hay {player.Gold}.");
            return false;
        }
        Store.Upgrade(relic, attribute, units);
        Rng rng = new Rng(player, RelicRollId, Seed(purchase.Floor, goldBefore, 91));
        string boon = Boons.TryGrant(relic, rawChance, rng.NextDouble(), rng.NextDouble());
        ModEntry.Info($"Taller, planta {purchase.Floor}: {relic.Id.Entry} {attribute.Key} +{units} por {price} de oro ({percent} %){(boon == null ? "" : ", bufo " + boon)}.");
        return true;
    }
}
