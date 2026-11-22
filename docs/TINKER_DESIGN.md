# The Workshop: upgrades for gold

**Status: built in 1.5.0, not yet tested in game.** Replaces the after-combat roulettes, which
are removed (with drawbacks, support items and the Cardsharp and Illuminator events).

Code: `src/Common/Workshop/WorkshopPrice.cs` (the price), `src/Card/Workshop/` (`WorkshopOptions` prices,
`WorkshopFlow` purchases, `WorkshopMessage` co-op, `WorkshopRelicPatch` for the Dial click, `DialGrant`), and
a Workshop mode in `MachineScreen` (one page: the deck in the grid, relics in a column beside it).

## How it works

1. **Every run starts with Tinkerer's Dial.** Runs that started without it get it as a relic reward after
   the next combat.
2. **Click the Dial any time outside combat** (map, shops, events, rest sites, reward screens) to open the
   Workshop. In combat, clicking it shows the relic as usual.
3. **Pick any card or relic, then the upgrade type** (Block, Damage, Healing…), **then how much gold to
   spend**. You get the biggest upgrade that amount buys; amounts that would buy nothing more than a smaller
   one are skipped. The centre shows gold you have / gold spent, green or red.
4. **Upgrades are certain**, and there are no after-combat upgrade rewards: gold is the only resource, so
   every upgrade competes with card removal, relics and potions in the shop.

Co-op: the buyer's game applies each purchase and its gold cost, then sends it to the other players
(`WorkshopMessage`), whose games apply the same, so nobody waits for anyone's Workshop. Relics are sent by
id and copy number, cards by deck position.

## Price

    gold = max(1, round(rate / chance))      chance = maxChance × e^(−exponent)

*exponent* is the odds engine's for that exact upgrade (same strain from earlier upgrades, same
difficulty); *rate* is the *Gold per upgrade* setting (default 25). So the smallest
upgrade on a fresh card costs about 25 gold, while big upgrades, and upgrades on cards or relics that are
already upgraded, cost more.

The price ignores the engine's minimum chance (`minChance`, 3 %), so it has **no ceiling**. With the
minimum, every upgrade past a point cost the same 833 gold, so +999 cost as much as +1 and was the only
size offered. Now prices grow exponentially: a value pushed to several times its base gets very expensive
(Strength 2 → 10: the next +1 costs about 23,600 gold). The minimum still applies to the trait and boon
chances.

## Support items: removed

Seal of Fate, Oracle's Inkwell and Ink of Fortune were removed in 1.5.0, along with the old Relic Upgrader
items (Ancient Ward, Oracle Tablet, Fate Elixir). Their classes were deleted in 1.6.0, so runs that held them
may not load. Tinkerer's Dial is the mod's only item.

## Unchanged

- **Traits** (cards) and **boons** (relics) can still appear on a purchase, more likely on bigger
  (pricier) upgrades.
- **No drawbacks.**
- Cards and relics keep their upgrade history (`CardMachineState`, relic `Store`), which sets later prices.
  Card upgrades from before 1.6.0 aren't loaded.

## Earlier designs

- **Roulette** (the original): you bet, then a spin decides. Output randomness: a gamble, not a decision.
- **Tinker draft** (3 pre-rolled offers): too limiting, you can only upgrade what's offered.
- **Known-fate roulette** (show the landing before betting): too strong; that's what Roulette Peek does as
  a cheat.
- **Scraps** (each combat drops scraps of one rolled category, spent only on that category): replaced by
  gold, so upgrades compete with everything else gold buys.
