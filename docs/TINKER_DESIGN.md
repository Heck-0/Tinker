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

    gold = max(1, round(rate × e^exponent))      exponent = weight · ratio^curve + strain

*ratio* is the upgrade's size (plus earlier upgrades of the same value) next to the value's base, with the
weight and curve of its cost category (`content/balance.jsonc`); *strain* comes from the card's or relic's
upgrades in other values; the difficulty setting scales it; *rate* is the *Gold per upgrade* setting
(default 25). So the smallest upgrade on a fresh card costs about 25 gold, while big upgrades, and upgrades
on cards or relics that are already upgraded, cost more, with **no ceiling**: a value pushed to several
times its base gets very expensive (Strength 2 → 10: the next +1 costs about 22,000 gold).

The categories' numbers come from the original roulette, where the same curve set a success chance with a
3 % floor. That chance, its floor and its cap are gone (1.6.0): prices come straight from the curve.

## Which values can be upgraded

Decided at run time by the rule engine (`src/Common/Rules`): it reads each card's and relic's compiled code
and applies generic rules (the game's own upgrade direction, thresholds, values that hurt you, value types,
buffs and debuffs, values that are never read or only read on pickup). Hand-written overrides
(`VarDirections.Overrides` for cards, `Catalog` overrides for relics) cover what the rules get wrong or can't
tell, cost categories that differ from the default, and fractional values, which are bought in steps of
0.01. Rules and overrides are listed in [ADDING_CARDS_AND_RELICS.md](ADDING_CARDS_AND_RELICS.md).

## Enchantments

Bought directly in the Workshop's right-hand panel: all 22 of the game's enchantments, each with its own
price and amount (`enchantments` in `content/balance.jsonc`, read by `ShopEnchantments`), the same on
every preset. Only enchantments the card can take are shown
(the game's `CanEnchant`), one per card as in the game. The amounts are the game's own: its shop list
(Sharp 2, Nimble 3, Adroit 3, Steady 1, Swift 2, Vigorous 5) and the relics and events that give the rest. Prices are set by value: every-play
effects and extra plays at the top, once-a-combat and situational ones lower, mixed blessings (Corrupted,
Slither) at the bottom. Nothing is rolled. Relics have no extras (the traits and boons of 1.5.0-1.7.0 were
removed in 1.8.0).

| Enchantment | Gold | Does | Fits |
|---|---:|---|---|
| Instinct | 400 | double damage | Attacks |
| Tezcatara's Ember | 400 | costs 0, Eternal, +3 damage | any |
| Glam | 320 | first play each combat is played an extra time | any |
| Spiral | 280 | every play is played an extra time | basic Strike / Defend |
| Adroit 3 | 280 | gain 3 Block per play | any |
| Momentum 5 | 280 | +5 damage after each play this combat | Attacks |
| Sown | 240 | first play each combat: gain 1 Energy | any |
| Imbued | 240 | played by itself on turn 1 | Skills |
| Royally Approved | 240 | Innate and Retain | Attacks, Skills |
| Nimble 3 | 240 | +3 Block per play | cards that gain Block |
| Sharp 2 | 240 | +2 damage per play | Attacks |
| Clone | 240 | copied at rest sites | only while you have Pael's Growth |
| Swift 2 | 200 | first play each combat: draw 2 | any |
| Soul's Power | 200 | removes Exhaust | cards that Exhaust |
| Corrupted | 200 | ×1.5 damage, you take 2 per play | Attacks |
| Slumbering Essence | 160 | costs 1 less per turn held in hand | any |
| Goopy | 160 | gains Exhaust; +1 Block forever after each play | Defends |
| Inky | 160 | apply 1 Weak per play | any |
| Steady | 160 | Retain | any |
| Vigorous 5 | 160 | +5 damage on its first play each combat | Attacks |
| Perfect Fit | 120 | goes on top of the draw pile when shuffled | any |
| Slither | 80 | random cost (0-3) each time it's drawn | playable, not X-cost |


## Support items: removed

Seal of Fate, Oracle's Inkwell and Ink of Fortune were removed in 1.5.0, along with the old Relic Upgrader
items (Ancient Ward, Oracle Tablet, Fate Elixir). Their classes were deleted in 1.6.0, so runs that held them
may not load. Tinkerer's Dial is the mod's only item.

## Unchanged

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
