# Coverage gaps (game v0.113.0, not yet re-checked against v0.113.1)

What the game has that Tinker can't upgrade yet, and which gaps are worth filling.
Generated from `tools/coverage.ps1` (full lists: `tools/coverage/cards.csv`, `relics.csv`). Re-run it after
a game update. How to add things: [ADDING_CARDS_AND_RELICS.md](ADDING_CARDS_AND_RELICS.md).

## Summary

| | In the game | Fully covered | Gaps |
|---|---|---|---|
| Player cards (Basic … Ancient, Event) | 642 | 448 | 118 cards have values the direction table has never seen (10 fixed in 1.2.1, 9 already covered by extra-hit effects); 5 have nothing to bet on |
| Relics | 316 | 139 have some support | ~48 more have values that matter during play; ~48 only act on pickup (not worth adding) |

Every card can still bet on its **energy cost**, so "uncovered" means its main numbers (damage, block…)
aren't offered.

## Cards

### 1. Multi-hit cards: renamed value — fixed in 1.2.1

The game renamed the hit-count value from `Repeat` to `HitCount`. Card Upgrader's tables used the old name,
so 10 cards lost their hit-count upgrade, and their Damage bets were priced as if the card hit once:

CelestialMight, Conflagration, Exterminate, FightMe, GunkUp, Peck, Ricochet, SevenStars, Spite, SwordBoomerang

Fixed in 1.2.1: `HitCount` entries in `VarDirections.Overrides`, `HitsFor` reads `HitCount` (then `Repeat`),
`"HitCount"` maps to the `repeat` family in `BalanceFamilies.Of`, a "Hits" label in `Lines`, and Fight Me's
description fix in `TextFixes` uses the new name.

Not affected (the coverage script lists them, but they're fine):

- AstralPulse, DaggerSpray, Dismantle, Maul, Refract, RipAndTear, Thrash, TwinStrike, Uproar already get hit
  upgrades through Card Upgrader's own "extra hits" effect (`ExpandedEffects`, `extra:hits`), which patches
  the hit count at attack time and wasn't broken by the rename.
- RapidShot and WahKi are Fool cards; they belong with section 2.

### 2. The Fool (new character): 85 of 90 cards uncovered

The Fool's card pool was added after the table was generated. Straightforward values, direction `1`:
`Damage`, `Block`, `Cards`, `Energy`, `HitCount`, and plain debuffs like `VulnerablePower` / `WeakPower`.
Fool-specific powers (`AimPower`, `StretchPower`, `Joy`, `Sadness`, `ChoBarbsPower`, `KiVersePower`, …) need
a look at what each power does before choosing a direction. Start with the commons and the basics
(StrikeFool, DefendFool, FoolishShot).

### 3. Other recent changes (14 cards)

| Card | Character | Values to add |
|---|---|---|
| Cacophony | Necrobinder | Cards |
| DingyDagger, GodByrd | Event | Damage |
| MightyShield / MightyWeapon | Event | Block or Damage, Cards |
| ExpectAFight | Ironclad | Block, Energy |
| FiendFire | Ironclad | CalculationBase, CalculationExtra |
| KnifeTrap | Silent | CalculationBase, CalculationExtra |
| MementoMori | Silent | Damage |
| MyShinies | Colorless | Damage, Gold, CalculationBase/Extra |
| Radiate | Regent | Stars |
| ShadowStep | Silent | Cards |
| Tank | Ironclad | DamageIncrease (+), DamageDecrease (check) |
| TearAsunder | Ironclad | Repeat |

`Calculation*` values are formula inputs; look at how the card uses them before picking a direction.

### 4. Nothing to bet on (5 cards)

BlotOutTheSky, Florescence, LockedIn, MythicForm, Recall have no numeric values and no special handler.
They'd need a custom effect in `FixedEffects` / `ExpandedEffects`, which is real coding work.

## Relics

Only relics that use their value **during play** are listed. Relics that act once on pickup (most Ancient
relics: "obtain X cards/relics", Strawberry, Mango, Pear, Lee's Waffle, …) are left out, because
upgrading them after pickup changes nothing.

### Easy: one `Group(...)` line each

| Relic | Rarity | Value | Family | Direction | Notes |
|---|---|---|---|---|---|
| TheAbacus | Shop | Block | block | 1 | block on shuffle |
| Twine | Rare | Block | block | 1 | |
| BathTowel | Event | Heal | resource | 1 | |
| ObsidianVice | Event | Damage | damage | 1 | |
| Fertilizer | Rare | StrengthPower | scaling | 1 | |
| SwordOfJade | Event | StrengthPower | scaling | 1 | |
| TormentousCollar | Event | WeakPower | power | 1 | applies Weak to enemies |
| FencingManual | Common | Forge | resource | 1 | |
| SymbioticVirus | Uncommon | Dark | repeat | 1 | Dark orbs channelled |
| FuneraryMask | Uncommon | Cards | generation | 1 | |
| StoneCracker | Uncommon | Cards | generic | 1 | |
| WarHammer | Ancient | Cards | generic | 1 | |
| WhittlingKnife, SongBow | Unc., Starter | AimPower | power | 1 | Fool |
| ExerciseMat | Common | StretchPower | power | 1 | Fool |
| Storyteller | Starter | AimPower, EndOfTurnAim | power | 1 | Fool |
| LizardTail | Rare | Heal | percentage | 1 | % of max HP; use maximum 100 |

### Thresholds and drawbacks (direction `-1`: lower is better)

| Relic | Value | Family | Notes |
|---|---|---|---|
| PhilosophersStone | StrengthPower | drawback | Strength given to **enemies** (same as Brimstone's EnemyStrength) |
| BlessedAntler | Cards | drawback | cards shuffled into your draw pile |
| Pendulum | Turns | threshold | every N turns; also `Cards` (+1, draw) |
| TheBoot | DamageMinimum, DamageThreshold | threshold | check which way each one helps |
| MightyWings | DamageThreshold | threshold | plus `StrengthPower` (+1, scaling) |
| BeatingRemnant | MaxHpLoss | protection | cap on HP lost per turn; check direction |
| Pocketwatch | CardThreshold | threshold | check direction: compared with cards played last turn |

### Not worth adding as-is

- **SturdyClamp**: its `Block` value is never read (the cap is a hard-coded 10).
- **BowlerHat** (×1.25 gold) and **UndyingSigil** (×0.5 damage): fractional multipliers. The Workshop sells
  whole units, so these need custom handling.
- Already-supported relics with a second value left out on purpose or for review: BigMushroom (MaxHp,
  pickup-only), EmberTea (Combats), JossPaper (Cards), PumpkinCandle (CombatCount).
