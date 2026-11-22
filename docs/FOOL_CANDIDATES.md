# The Fool: upgrade support (game v0.113.1)

**Implemented in 1.3.0** for sections A, B, the decided parts of C, and the relics. Each row says which value
the Workshop lets you upgrade, and whether a **higher** number is better (**+**) or worse (**−**).

- Cards: directions in `VarDirections.Overrides`, hit counts in `VarDirections.Hits`, cost categories for
  the special cases in `CardAttributes.RiskFamily`, Sadness labels in `FixedEffectPatches.AttributeName`.
  Relics: `Catalog.cs`.
- Since 1.3.0 there are no bet-size limits or value caps; "category" below only sets how fast the odds drop.
- Values in *(base → upgraded)* are the game's own numbers.
- Every Fool card can already bet on its **energy cost**; this adds its other numbers.

## Summary

| | Count | Proposal |
|---|---|---|
| Cards, straightforward | 55 | add |
| Cards, percentages / strong scaling | 9 | add, worth a look |
| Cards, needs a decision | 21 | see section C |
| Cards with no numbers | 5 | skip (needs custom code) |
| Relics with numbers | 6 | add |
| Relics without numbers | 3 | skip |

Fool mechanics, for reference: **Aim** is Vigor for arrow attacks (bonus damage on your next arrow).
**Stretch** is bonus block on your next block card. **Joy** is a 0-cost token card that gives 1 energy.
**Sadness** is an unplayable Status card added to your hand. **Wah / Cho / Ki** are card tags that some
powers care about.

## A. Straightforward (+, standard family): 55 cards

| Card | Rarity | Values to add |
|---|---|---|
| StrikeFool | Basic | Damage (6 → 9) |
| DefendFool | Basic | Block (5 → 8) |
| FoolishShot | Basic | Damage (15 → 20) |
| ArrowStab | Common | Damage (9 → 12) |
| BowSmack | Common | Damage (8 → 11), Energy gained (1) |
| ChoFirm | Common | Block (6 → 8) |
| ChoReady | Common | Block (8 → 11), AimPower (3 → 5) |
| DeepStretch | Common | Cards drawn (1), StretchPower (7 → 10) |
| KiHop | Common | Damage (7 → 8), Cards drawn (2 → 3) |
| KiWhip | Common | Damage (5 → 6), Energy next turn (2 → 3) |
| MourningBlow | Common | Damage (10 → 15) |
| Nock | Common | AimPower (5 → 7) |
| QuickShot | Common | Damage (10 → 13) |
| RainShot | Common | Damage (12 → 15) |
| WahHammer | Common | Damage (9 → 12) |
| WahSweep | Common | Damage (8 → 11) |
| WahTouch | Common | Damage (6 → 8), Vulnerable on enemy (1 → 2) |
| Archery | Uncommon | Aim per trigger (4 → 5) |
| BlossomShot | Uncommon | Damage (11 → 14) |
| Brace | Uncommon | AimPower (8 → 11) |
| ChoKi | Uncommon | Block (8 → 11) |
| ChoLimbs | Uncommon | Block (5 → 7) |
| Compost | Uncommon | Energy per trigger (1 → 2) |
| Enrapture | Uncommon | Vigor (6 → 9), Stretch (6 → 9), given to the target ally |
| EpicRetrieval | Uncommon | Damage (15 → 20), Energy gained (3 → 4) |
| HiddenRoots | Uncommon | Stretch per trigger (3 → 4) |
| HobblingShot | Uncommon | Damage (13 → 14), Weak + Vulnerable on hit (1 → 2) |
| KiRest | Uncommon | Energy next turn (5 → 6) |
| PunchOfJoy | Uncommon | Damage (13 → 17) |
| RapidShot | Uncommon | Damage (3), HitCount (3 → 4) |
| Rummage | Uncommon | Cards drawn (3 → 4) |
| SongPath | Uncommon | Block per trigger (5 → 7) |
| Timber | Uncommon | Damage (20 → 25), damage to all on kill (30 → 35) |
| TrickShot | Uncommon | Damage (12 → 15) |
| Uproot | Uncommon | Vulnerable on enemy (3 → 4) |
| ViolentChord | Uncommon | Damage to all per trigger (9 → 12) |
| Vitriol | Uncommon | Weak on enemy (2 → 3) |
| WahBlast | Uncommon | Damage (14 → 18) |
| WahCho | Uncommon | Damage (3 → 5), Block (3 → 5) |
| WahKi | Uncommon | Damage (2 → 4), HitCount (2), Cards drawn (1), Energy next turn (1) |
| WhistlingArrows | Uncommon | Block (3 → 4) |
| Wrest | Uncommon | Cards drawn (2 → 3) |
| Yoga | Uncommon | Stretch to allies (6 → 8) |
| AllIsWell | Uncommon | Block per trigger (4 → 5) |
| Bounce | Rare | StretchPower (10 → 13) |
| ChoChorus | Rare | Block (12 → 15) |
| Coda | Rare | Damage (40 → 55) |
| CoveringFire | Rare | Block to allies per trigger (8 → 11) |
| ExplosiveArrows | Rare | Damage to all per trigger (6 → 8) |
| Nail | Rare | Damage (20 → 25), Weak + Vulnerable (2 → 3) |
| RepeatShot | Rare | Damage (15 → 20) |
| SkyShot | Rare | Damage (30 → 40) |

Also in this group: **DivineShot** (Ancient): Damage (40 → 50); **WahChoKi** (Rare): Damage (9 → 12),
Block (9 → 12), Cards drawn (1 → 2); **Yah** (Rare): Energy per trigger (1 → 2).

## B. Percentages and strong scaling (+, special cost category): 9 cards

These are "+"; they get a cost category that matches how big one upgrade unit is (percentage, multiplier,
repeat) instead of the default for their name.

| Card | Rarity | Value | Why it needs care |
|---|---|---|---|
| Honing | Rare | HoningPower (50) | a % bonus; one unit = +1 % |
| SerratedArrows | Uncommon | SerratedArrowsPower (25 → 50) | a % bonus; one unit = +1 % |
| DislikeYou | Uncommon | Damage (8 → 9), DislikeYouPower (50 → 75) | enemy takes +% damage; one unit = +1 % |
| Lullaby | Rare | Block per trigger (20) | large base, fine |
| HeavyShot | Common | Damage (20 → 25), AimMultiplier (×2) | one unit turns ×2 into ×3: multiplier category |
| KiVerse | Rare | KiVersePower (1 → 2) | each point replays a Wah/Cho/Ki card |
| Motif | Uncommon | MotifPower (1) | multiplies a draw |
| LostLute | Ancient | LostLutePower (1) | auto-plays cards from your draw pile |
| Simpleton | Rare | SimpletonPower (1) | repeats per point |

## C. Needs a decision: 21 cards

**Sadness-adding cards: decrease only (implemented).** Sadness is an unplayable Status card, so the Workshop
only offers "Sadness added (decrease)", down to 0. It's priced as a drawback, like other "lower is better" values:

| Card | Value | Proposal |
|---|---|---|
| SorrowfulStrike | Damage (13 → 17) +, Sadness added (2) | Sadness − (decrease only) |
| Jest | Block (14 → 18) +, Sadness added (1) | Sadness − (decrease only) |
| BrightenUp | Joy (2 → 3) +, Sadness added (2) | Joy +, Sadness − (decrease only) |
| SelfDeprecate | Joy (2 → 3) +, Sadness added (1) | Joy +, Sadness − (decrease only) |
| Ennui | EnnuiPower (2 → 3) | + (benefit per Sadness) |
| Ecstasy | Sadness converted per trigger (1) | + |

**Formula inputs (proposal: +).** These cards compute damage, block or hits from a formula. More is better,
but the formula decides how strong one unit is:

| Card | Values |
|---|---|
| MelodicShot | Damage (12 → 13), CalculationBase, CalculationExtra (hits) |
| RhythmStrike | Damage (4 → 5), CalculationBase, CalculationExtra (hits) |
| SteadyCadence | Damage (10 → 13), CalculationBase, CalculationExtra |
| Anguish | CalculationBase (6 → 7), ExtraDamage (6 → 7) |
| WahSymphony | CalculationBase (6), ExtraDamage (4 → 6) |
| Growth | CalculationExtra (4 → 6): Stretch per card |
| HarmonicShield | CalculationExtra (5 → 7): block per card |

**Added after review (1.3.0):**

| Card | Values | Notes |
|---|---|---|
| ChoBarbs | Block +, ChoBarbsPower + | the power is temporary Thorns |
| KiChant | Cards + | how many top cards you choose from (selection category) |
| Quietus | QuietusPower + | multiplies arrow damage (multiplier category) |
| Quiver | Repeat + | arrow slots gained |
| Tune | free cards per turn + | persistent energy category |
| Ensemble | free cards + | energy category |
| Encore | Energy +, Cards +, EncorePower + | EncorePower caps how many more cards you can play, so higher is better |
| Growth, HarmonicShield | CalculationBase + | flat amount on top of the per-card value |

**ShowOff:** its `AimPower` isn't offered directly: the card computes it as BaseAim + Aim gained from kills. Betting on `BaseAim` raises it, and `CardAttributes.ApplyRaw` recomputes the Aim right away.
## D. No numbers in the game's code: added as special effects (1.3.0)

These cards hard-code their amounts, so they use Card Upgrader's special effects (`ExpandedEffects`) instead
of a value:

| Card | Effect | How |
|---|---|---|
| LockedIn | Shoot cards retrieved from your draw pile + | existing "retrieve" effect |
| Recall | cards put on top of your draw pile + | new "retrieveTop" handler (`ExpandedEffectPatches.RetrieveToTop`) |
| BlotOutTheSky | free Shoot cards created + | existing "generate" effect, filtered to Shoot cards |
| MythicForm | cards added each turn + | existing "power" effect on MythicFormPower |
| Florescence | damage per exhausted card on The Shot it creates + (10, upgraded 13) | new "shotDamage" handler (`ExpandedEffectPatches.BuffShotFromFlorescence`) |

If the game changes one of these cards, its special effect switches itself off and logs a `[Tinker Card]` warning
instead of breaking.
## Relics

| Relic | Rarity | Value | Proposal | Family |
|---|---|---|---|---|
| SongBow | Starter | AimPower (6) at combat start | + | power |
| Storyteller | Starter (upgraded SongBow) | AimPower (12) +, EndOfTurnAim (3) + | + | power |
| WhittlingKnife | Uncommon | AimPower (5) on shuffle | + | power |
| ExerciseMat | Common | StretchPower (6) each turn | + | power |
| Fertilizer | Rare | StrengthPower (1) per generated card | + | scaling |
| Twine | Rare | Block (4) when an arrow is dislodged | + | block |
| BizarreLamp, ConductorsBaton, Ocarina | | no numbers | skip | |

## Also needed

- Labels for new value names in `Lines.cs` (cards: Aim, Stretch, Joy, Sadness, OnKillDamage, …) and
  `Text.Attribute` (relics: AimPower, StretchPower, EndOfTurnAim), otherwise the raw name shows.
- Card values are priced by name in `BalanceFamilies.Of`. Names ending in `Power` get the power curve;
  `Joy`, `Sadness`, `OnKillDamage`, `AimMultiplier` and `BaseAim` fall to `generic` unless mapped.
