# Cleanup plan: Card Upgrader (`Ruleta.*`) and Relic Upgrader code

**Status: step 0 and phases 1–3 done; phases 4–5 optional.** Goal: fewer, better-organised files without changing how the game
plays or breaking saves, settings, co-op or Roulette Peek.

## Where things stand

| Folder | Files | Size | Notes |
|---|---|---|---|
| `src/Card/Ruleta.Game` | 61 | 567 KB | everything game-facing in one flat folder; 11 files under 1 KB |
| `src/Card/Ruleta.Core` | 39 | 114 KB | rules and data types; 22 files under 1 KB |
| `src/RelicUpgrader` | 51 | 174 KB | flat; 20 files are a single small Harmony patch each |

Three files are huge: `MachineScreen.cs` (57 KB, the card roulette screen), `ExpandedEffectPatches.cs`
(50 KB) and `VarDirections.cs` (49 KB, mostly data tables).

## Rules every step must follow

These are what make the cleanup safe. Breaking any of them breaks saves, co-op or Roulette Peek.

1. **Don't rename model classes.** The game saves relics, potions and events by class name:
   `FateSeal`, `OracleInkwell`, `InkOfFortune`, `TinkerersDial`, `Cardsharp`, `Illuminator`, and Relic
   Upgrader's `AncientWard`, `OracleTablet`, `FateElixir`. Moving them to another file or namespace is fine.
2. **Don't rename saved fields or keys:** `FateSeal.RescuedFloor` / `CardUpgraderState`, the
   `relicupgrader_*` save keys in `Store.cs`, the `cardupgrader` / `relicupgrader` RNG and reward ids.
3. **Roulette Peek finds these by full name:** `Tinker.Card.MachineScreen`, `MachineFlow`, `FateItems`,
   `TinkerRewards`, `Lines`, `Tinker.Common.SpinOutcome`, `Tinker.Relic.RouletteScreen`, `Flow`,
   `SupportItems`. If a namespace changes, update Roulette Peek in the same step.
4. **Harmony patches are found by namespace** (`Patching.PatchNamespace("Tinker.Card")`, `("Tinker.Relic")`,
   and `("Tinker", …)` for the rest). New namespaces must stay under those roots, or the call must change
   with them.
5. **Keep behaviour identical** unless a step says otherwise.

## Step 0: version control — done

The repo isn't under git yet. A refactor this size needs it: `git init`, commit the current state, then one
commit per step below so any step can be undone on its own.

## Phase 1: organise into folders (no code changes) — done

Move files into feature folders. C# doesn't care about folders, and the namespace stays `Ruleta.Game` /
`RelicUpgrader`, so nothing can break. This is the biggest readability win for the least risk.

**`src/Card/`**

| Folder | Files |
|---|---|
| `Roulette/` | MachineScreen, MachineFlow, MachineChoice, SpinContext, SpinResult, ScrollTier, ScrollButtonLayout, WheelTextures, LegendTips, Nodes |
| `Upgrades/` | CardAttributes, VarDirections, FixedEffectPatches, ExpandedEffectPatches, ExpandedEffectText, RewrittenCardValues, PowerParameters, FinalCardEffects, EnchantmentUpgrades, AdvancedEnchantmentUpgrades, TextFixes, HoloNumbers |
| `TraitsAndDrawbacks/` | ExceptionalEffects, NegativeEffects |
| `CardLooks/` | *(removed later along with the card looks; ModTextures moved to `Items/`)* |
| `Items/` | FateItems, FateSeal, OracleInkwell, InkOfFortune, TinkerersDial |
| `RewardsAndEvents/` | TinkerReward, TinkerRewards, Cardsharp, Illuminator, PermanentUpgradeSelection |
| `Settings/` | Settings, CardUpgraderConfig, ConfigGraphs, BalanceFile, HostSettingsSync |
| `Saves/` | PersistencePatches, CardStateStore |
| `Text/` | Loc, Lines, LocTablesPatch |
| `RunSummary/` | RunSummaryScreen, RunStatsLog, Telemetry |
| `DevTools/` | TestBench, RuletaConsoleCmd, SelfTest, IntegrationChecks, NoPadForTests |
| (root) | ModEntry, GameCompat |

**`src/Common/`**: `Upgrades/` (attribute keys, effects, balance, odds), `TraitsAndDrawbacks/`
(penalty and exceptional catalogs), `State/` (CardMachineState, ledgers, save identity), `RunLog/` (run
summary and spin records).

**`src/Relic/`**: `Roulette/` (RouletteScreen, Flow, WheelBand, Bet, RouletteReward), `Upgrades/`
(Catalog, RelicAttribute, Rules, RiskFamily, Store, RelicState, UpgradeLedger), `TraitsAndDrawbacks/`,
`Items/` (SupportItems and the legacy items), `Settings/`, `Text/` (Text, Chinese, ConfigText), `DevTools/`
(Sandbox), .

## Phase 2: merge the tiny files (no behaviour change) — done

Same classes and names, fewer files.

| New file | Merges | Why |
|---|---|---|
| `Roulette/RouletteTypes.cs` | MachineChoice, SpinContext, SpinResult, ScrollTier | small data types used only by the roulette |
| `Items/SupportItemModels.cs` | FateSeal, OracleInkwell, InkOfFortune, TinkerersDial | four short model classes |
| `RewardsAndEvents/TinkerRewards.cs` | TinkerReward + TinkerRewards | reward and its spawning logic |
| `Core/.../RunLog.cs` | RunLog, SpinRecord, DeckCardSummary, RunSummary | run-summary data |
| `Core/.../Penalties.cs` | NegativeEffectDefinition, NegativeKind, PenaltyContext, PenaltyRequirement, PenaltyResult | penalty data types |
| `Core/.../Exceptional.cs` | ExceptionalDefinition, ExceptionalRequirement (+ catalog) | trait data types |
| `Relic/Settings/SettingsPatches.cs` | SettingsLobbyPatch, SettingsRunPatch, SettingsStartPatch, SettingsEndPatch | four 1-line patches on the same flow |
| `Relic/TraitsAndDrawbacks/BoonPatches.cs` | CombatBoonPatch, DrawBoonPatch, EnergyBoonPatch | same feature |
| `Relic/Items/LegacyItemPatches.cs` | ElixirAvailability/ImagePath/OutlinePath patches, SupportRelicIcons, SupportPotionIcons | only the old Relic Upgrader items use these |
| `Relic/Saves/StatePatches.cs` | CloneStatePatch, PlayerResyncPatch, EvolutionPatch | relic state bookkeeping |
| `Relic/Upgrades/RelicTypes.cs` | Bet, Fault, RiskFamily, UpgradeLedger | small records |

Done: 41 small files became 11; src/ went from 162 to 132 files.

## Phase 3: remove leftovers from combining (small behaviour cleanups) — done

1. **Retire `CardUpgraderConfig` and `RelicConfig` as menu classes.** Since 1.2.0 they're never registered;
   `Config` just forwards to their properties. Move each property's setter logic into
   `Config` (or into plain static `CardSettings` / `RelicSettings` classes) and delete the old
   BaseLib-specific parts. Removes ~17 KB, with no visible change.
2. **Remove the telemetry.** The upload address is empty, so it never sends anything, but it still writes
   up to 50 files to a local `telemetry_queue` folder and shows a "share stats" toggle on the run summary.
   Delete `Telemetry.cs` and `TelemetryPayload.cs`, and the toggle. *(Visible change: the toggle goes away.)*
3. **Dev-only code:** keep `SelfTest`, `IntegrationChecks` and `NoPadForTests` (they only run with
   environment variables set), but move them into `DevTools/` (done in Phase 1).

## Phase 4: split the three giant files (optional)

Use `partial class`, so no names change:

- `VarDirections.cs` → `VarDirections.cs` (logic), `VarDirections.Overrides.cs` (hand-written table, where you
  add cards), `VarDirections.Generated.cs` (the original tool's table), `VarDirections.Hits.cs`.
- `ExpandedEffectPatches.cs` → the transpiler, plus one file per group of handlers (selection, generation,
  powers, …).
- `MachineScreen.cs` → layout/build, betting controls, spin/result, legend.

## Phase 5: namespaces — done (1.4.0)

`Ruleta.Game` → `Tinker.Card`, `RelicUpgrader` → `Tinker.Relic`, `UpgraderCombined(.Common)` →
`Tinker(.Common)`, with the mod id renamed to `tinker`. Roulette Peek and the patching roots were updated in
the same step. An IL comparison of every method confirmed nothing else changed.

## Not proposed

- **Renaming decompiler variables** (`num`, `num2`, `flag`, …) across 750 KB of code. Worth doing only in
  files you're actively editing.
- **Merging the two roulette screens, rewards, or drawback systems.** Those change gameplay; see the
  "Shared code" section of the README.

## How each step is checked

1. `dotnet build` with no errors.
2. The reference check (every game method the mod calls still exists) and the Roulette Peek hook check, as
   used for 1.2.0.
3. The coverage report gives the same numbers as before the step.
4. One in-game smoke test per phase: open both roulettes, spin once, open the settings menu, load a saved run.
