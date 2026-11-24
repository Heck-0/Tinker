# Tinker

A Slay the Spire 2 mod for upgrading cards and relics with gold. It grew out of Card Upgrader 0.9.2 and Relic
Upgrader 0.5.1 (Workshop mods), rebuilt from decompiled source as one mod, `tinker`. Formerly
*Upgrader Combined* (`upgradercombined`).

This is a personal local build. Much of the code under `src/Card` and `src/Relic`, and the files under
`content/`, come from the original mods' authors; don't redistribute it without their permission.

## How it plays

Every run starts with **Tinkerer's Dial**. Click it any time outside combat to open the **Workshop**:

- pick a card from your deck or one of your relics;
- the top selector picks the upgrade (Damage, Block, Healing…), the bottom one its size (+1, +3…);
- the price is shown under it, green if you can afford it, red if not. Nothing in the Workshop is random.

Bigger upgrades, and upgrades on cards or relics that are already upgraded, cost more (no ceiling). The panel
on the right sells the game's **enchantments** for cards (all 22: Instinct, Glam, Sharp…), each at its own
price from `content/balance.jsonc` (80-400 gold, the same on every preset), one per card like in the game; a bought enchantment's amount can then be upgraded like any other value. In co-op every purchase is sent to the
other players. Design: [docs/TINKER_DESIGN.md](docs/TINKER_DESIGN.md).

## Build

Needs the .NET 8 SDK, the game, and BaseLib (Workshop). Paths are set at the top of
`Tinker.csproj` (`StsDir`, `BaseLibDir`).

    dotnet build -c Release              # output in bin\Release\
    dotnet build -c Release -t:Install   # also copies to <game>\mods\tinker\

**Disable the separate Card Upgrader and Relic Upgrader mods, and delete any old `mods\relicupgrader` or
`mods\upgradercombined` folder, before enabling this one.** Loading both would register everything twice.

## Docs and tools

- [docs/TINKER_DESIGN.md](docs/TINKER_DESIGN.md) - how the Workshop works and why.
- [docs/ADDING_CARDS_AND_RELICS.md](docs/ADDING_CARDS_AND_RELICS.md) - how to make more cards and relics upgradable, in VS Code.
- [docs/COVERAGE_GAPS.md](docs/COVERAGE_GAPS.md) - what the current game has that isn't covered yet.
- [docs/MODIFICATIONS.md](docs/MODIFICATIONS.md) - every upgrade the mod can make, by how many cards it fits.
- `tools/coverage.ps1` - regenerates the coverage lists (VS Code task *Coverage report*).
- `tools/divergence.py` - the generic upgrade rules on the decompiled game source, compared with the mod's
  overrides; writes `tools/coverage/DIVERGENCE.md` (VS Code task *Divergence report*).
- `tools/rulecheck` - runs the mod's own rule engine on the game's compiled code (Mono.Cecil) and compares
  it with `divergence.py` (`dotnet run --project tools/rulecheck`, arguments in its project file).

## Layout

- `src/Entry.cs` - the mod initializer; runs each half's initializer, then registers the settings menu.
- `src/Config.cs`, `src/Settings.cs`, `src/Text.cs` - the settings menu (`Tinker.cfg`), the values shared by
  both halves, and the menu's labels.
- `src/HostSettingsMessages.cs` - co-op: the host's settings, sent to the other players.
- `src/Card/Workshop/` - the Workshop: page (`WorkshopScreen`), purchases (`WorkshopFlow`), prices
  (`WorkshopOptions`, `CardCost`), co-op message, the Dial click and handing out the Dial.
- `src/Common/Rules/` - the rule engine: which card and relic values can be upgraded, and which way, read from
  the game's compiled code at run time (`RuleEngine` = the rules, `GameRules` = reading the game through Harmony).
- `src/Card/` (`Tinker.Card`) - card upgrades: how values change (`Upgrades/`; direction overrides in
  `VarDirections.cs`, fractional values in `SteppedValues.cs`), card state saves, card texts.
- `src/Relic/` (`Tinker.Relic`) - relic upgrades (`Upgrades/Catalog.cs`: engine + overrides, cost categories,
  fractional `Stepped` values; `PaperRelics.cs`), relic state saves, relic settings and texts.
- `src/Common/` (`Tinker.Common`) - shared rules: price curve (`Upgrades/RiskCurve.cs`, `UpgradeCost.cs`), balance
  (`BalanceConfig`, loaded from `content/balance.jsonc`), the Workshop price, card state, text system,
  `Patching`.
- `content/` - `balance.jsonc`: built into the DLL as the defaults, and also copied next to it, where edits
  override the defaults without a rebuild (the next install overwrites that copy).

## Saves

Upgrades are stored on the cards and relics themselves:

- cards: the `CardUpgraderState` saved property, card-state format 2 (declared on `TinkerersDial` so co-op
  gives it a network id, see `Card/ModEntry.cs`);
- relics: `relicupgrader_state_v1`; per-run relic rules: `relicupgrader_roulette_v2`.

**Since 1.6.0, Tinker data from older saves isn't loaded.** Cards from older runs lose their upgrades (format 1
is rejected and the card stays normal), and a run holding Seal of Fate, Oracle's Inkwell or Ink of Fortune
may not load at all. Relic upgrades still load. The save key names stay as they are, since renaming them
removes no code.

## History

- **1.8.0** - Enchantments replace traits and boons. The Workshop's right-hand panel sells all 22 of the
  game's enchantments for cards, one per card, only on the cards each fits, each with its own price and amount
  (`enchantments` in `content/balance.jsonc`, the same on every preset). Clone is offered only with Pael's Growth (its rest-site copy needs that relic). The mod's card traits (Inexhaustible, Echo, Awakening, Legacy, Venomous,
  Lifesteal) and relic boons are gone, with their settings. Saves: upgrades load as before; a trait or boon
  bought earlier is dropped. Enchantments are saved by the game itself. Every co-op player needs 1.8.0.
- **1.7.0** - The rule engine: which values are upgradable, and which way, now comes from the game's own
  compiled code at run time (a port of `tools/divergence.py`'s rules to IL, with three new rules: not read,
  only on pickup, loop bounds aren't limits). The hand-written tables shrank to overrides: cards from
  ~1,000 lines (`VarDirections` Overrides + Generated) to 52 entries; relics from the full catalog to 61
  overrides. Every previously upgradable value keeps its exact direction, category and limits (checked
  value by value); 59 card values and 28 relic values become upgradable. Fractional values are bought in
  steps of 0.01: Bowler Hat (gold ×1.25), Undying Sigil (×0.5), Tank (×1.5 / ×0.5, now copied onto its
  powers), and Paper Phrog / Paper Krane, whose hard-coded bonus became a value. Cost categories also come
  from the code: per-turn energy / draw and max-HP growth by where a relic value is used, and **trigger
  tiers** for power amounts and relic hooks (per play / per turn / occasional / per combat / per run, from the
  151 hooks the game uses), so e.g. Afterimage (per card) is priced above Dark Embrace (per Exhaust).
  Text: the code is English only; Spanish moved into a translation table like Chinese
  (`Common/Text/Spanish.cs`, same output as before). The rewrites of the game's own Spanish card text are
  gone, so Spanish card descriptions keep the game's wording, as Chinese already did. Log and error messages
  are in English.
- **1.6.0** - Dropped old-save support: the retired item classes and their icons, the roulette's loss and
  drawback-repair bookkeeping (card state is now just units bought per value), the original Card Upgrader's
  save key, the Dial offer for runs started without it, the settings copy from `UpgraderCombined.cfg`, and
  unused Chinese translations. About 720 more lines; DLL 543 KB → 517 KB.
- **1.5.0** - The Workshop replaces the after-combat roulettes (card scrolls and the relic roulette). Removed
  with them: drawbacks, the support items, the Cardsharp and Illuminator events, the run summary screen,
  the test tools, and the menu sections for all of these. About 9,000 lines of code went; a reachability
  check on the compiled DLL confirms nothing unreachable is left. Fixed five cards whose own downside was
  upgraded upwards (Biased Cognition, Wraith Form, Neurosurge, Mind Rot, Disintegration), found by
  `tools/divergence.py`.
- **1.4.0** - Renamed to Tinker: mod id `tinker`, namespaces `Tinker.Card` / `Tinker.Relic` /
  `Tinker.Common`, settings file `Tinker.cfg` (copied from `UpgraderCombined.cfg`).
- **1.3.0** - The Fool's 90 cards and 6 relics ([docs/FOOL_CANDIDATES.md](docs/FOOL_CANDIDATES.md)); no
  upgrade-size or value caps (percentages top out at 100 %); card looks and telemetry removed; Inexhaustible
  no longer on Powers; one odds engine and one text system for both halves.
- **1.2.1** - Hit counts: the game renamed `Repeat` to `HitCount`; ten multi-hit cards had lost that
  upgrade ([docs/COVERAGE_GAPS.md](docs/COVERAGE_GAPS.md)).
- **1.2.0** - Shared code between the halves: one Harmony instance, one settings menu, one co-op settings
  message, shared language detection.
- **1.1.0** - Both halves used one set of support items.

Changes from the original mods for game v0.113: `RelicModel.IsAllowed` is no longer virtual (relics override
`IsAllowedInternal`); `HoverTip.Id` / `IsDebuff` are read-only (`GameCompat113.WithIdentity`);
`CardSelectorPrefs` properties are init-only; `CreatureCmd.LoseMaxHp` takes a `CardModel? cardSource`.
