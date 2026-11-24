# Adding cards and relics yourself (VS Code)

How to make more cards and relics upgradable in Tinker. For what's missing right now, see
[COVERAGE_GAPS.md](COVERAGE_GAPS.md).

## One-time setup

1. **Install VS Code** and open the folder `C:\Users\Sean\Repos\Tinker`
   (*File → Open Folder*). Accept the prompt to install the recommended **C# Dev Kit** extension. It gives
   you autocomplete, go-to-definition and error squiggles.
2. **.NET 8 SDK**: already installed. Check with `dotnet --list-sdks` in VS Code's terminal (*Ctrl+`*).
3. **Decompiled game code** to look things up in:
   `C:\Users\Sean\Documents\StS2ModSource\_sts2_full\`. Open it in a second VS Code window
   (*File → New Window*, then *Open Folder*). After a game update, regenerate it:

   ```bash
   ilspycmd -p -o "C:\Users\Sean\Documents\StS2ModSource\_sts2_full" -r "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64" "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll"
   ```

   Delete the old folder first. Then run the **Coverage report** task (below) to see what changed.

## Build and install

**Close the game first**: Windows locks the mod DLL while the game runs.

- **Ctrl+Shift+B** runs *Build & install*: compiles and copies everything into
  `…\Slay the Spire 2\mods\tinker\`.
- *Terminal → Run Task…* also has **Build only** and **Coverage report**.
- Compile errors appear in the *Problems* panel (*Ctrl+Shift+M*); click one to jump to the line.

The same from a terminal:

```bash
dotnet build -c Release -t:Install
```

## Step 1: find the card or relic's values

Every card and relic stores its numbers as named **values** (the game calls them *DynamicVars*). That name is
what the mod uses. In the decompiled game, open:

- cards: `MegaCrit.Sts2.Core.Models.Cards\<CardName>.cs`
- relics: `MegaCrit.Sts2.Core.Models.Relics\<RelicName>.cs`

Tip: *Ctrl+P* and type the name. Look for `CanonicalVars`:

```csharp
// Anchor.cs
protected override IEnumerable<DynamicVar> CanonicalVars => ... new BlockVar(10m, ValueProp.Unpowered) ...;
```

How to read the value name:

| In the code | Value name |
|---|---|
| `new DamageVar(...)`, `BlockVar`, `CardsVar`, `EnergyVar`, `HealVar`, `GoldVar`, `MaxHpVar`, `HitCountVar`, `RepeatVar`, `StarsVar`, `ForgeVar`, `SummonVar` | the class name minus `Var`: `Damage`, `Block`, `Cards`, … |
| `new PowerVar<StrengthPower>(2m)` | the power's name: `StrengthPower` |
| `new DynamicVar("Turns", 3m)` | the string: `Turns` |
| `BoolVar`, `StringVar`, `Calculated...Var` | not a plain number, skip |

### Most values need nothing: the rule engine

Since the rule engine (`src/Common/Rules`), the mod reads each card's and relic's compiled code when the
game runs and decides, per value, whether it can be upgraded and in which direction. The rules, first match
wins:

1. **derived** (`Calculated…Var`) or a pickup-only relic: not upgradable.
2. **not read**: the code never uses the value (it only shows in the text): not upgradable.
3. **only on pickup**: a relic uses it only in `AfterObtained`, so a later upgrade would do nothing.
4. **upgrade**: the game's own `OnUpgrade` raises (+1) or lowers (−1) it.
5. **every N / threshold**: it divides something (`x % N`, `x / N`) or is a `…Threshold` property: −1.
6. **hurts you**: named `Lose…`, passed to `PlayerCmd.Lose…`, or the amount of damage dealt to the owner: −1.
7. **subtracted** (`x − N`) or **compared** (`x >= N`, not a loop bound): not upgradable, a cost and a
   limit look the same.
8. **type**: Damage, Block, Cards, Energy, Heal, Gold, Max HP, Stars, Forge, Summon, hits: +1; HpLoss: −1.
9. **power**: a buff on yourself or a debuff on enemies: +1; the reverse: −1.

So a new card or relic is usually covered with no change. Check it in the game (Step 3), and add an
**override** only when the engine is wrong or can't tell.

To see what the engine decides for every value without starting the game, run the **rulecheck** tool (it
also compares with `tools/divergence.py`):

```
dotnet run --project tools/rulecheck -c Release -- "<game>/data_sts2_windows_x86_64/sts2.dll" "C:/Users/Sean/Documents/StS2ModSource/_sts2_full" tools/coverage out.tsv
```

In the game, put an empty file named `rules_dump` in `mods/tinker/`: opening the Workshop then writes the
engine's verdicts to `mods/tinker/rules_dump.tsv`.

## Step 2a: a relic override

Open `src/Relic/Upgrades/Catalog.cs`, `BuildOverrides`. One line per relic value:

```csharp
Override("Relic", "ValueName", "family", direction, maximum, labelKey);
//                                      optional: 1        10000    null
Override("IvoryTile", "EnergyThreshold", "threshold", -1);        // lower threshold is better
Override("MeatOnTheBone", "HpThreshold", "percentage", 1, 100);   // cap at 100 (%)
Override("BlessedAntler", "Cards", "drawback", -1);               // the rules said +1: wrong
```

Values the engine handles get a cost **family** from the value's name and type (`Catalog.DefaultFamily`):
Damage → damage, Block → block, Heal/Gold/Max HP/Stars/Forge/Summon → resource, Cards → draw, Energy →
energy, hits → repeat, Strength/Dexterity/Focus → scaling, other powers → power, lower-is-better →
threshold or drawback. Add an override when a value needs a different family. For example, energy every
turn is `persistentEnergy`, not `energy`.

| Family | Use for |
|---|---|
| `damage`, `block` | damage, block (prices rise slowly) |
| `resource` | heal, gold, forge, stars |
| `power` | debuffs/buffs like Weak, Vulnerable, Vigor |
| `scaling` | Strength, Dexterity, Focus |
| `draw` | cards drawn |
| `energy` | energy each turn |
| `persistentEnergy` / `persistentDraw` | permanent energy/draw (boss relics; prices rise fast) |
| `repeat` | extra triggers, orbs, shivs |
| `generation`, `selection` | cards created / cards to choose from |
| `threshold` (direction -1) | "every N cards/turns", requirements |
| `duration` | number of turns an effect lasts |
| `percentage` | % values (set a maximum, normally 100) |
| `protection` | damage/HP-loss reduction |
| `permanentGrowth` | max HP and similar |
| `drawback` (direction -1) | values that hurt you |
| `generic` | anything else |

**Fractions** (multipliers like Bowler Hat's ×1.25) are only upgraded through a `Stepped(...)` line, which
sets the step (0.01), the "no effect" value that prices count from, and the limits.

**Label**: the Workshop shows a name for the value from `Text.Attribute` in `src/Relic/Text/Text.cs`.
Common names already have labels. For a new value name, add a `case` there (Spanish and English) and the
Chinese in `src/Common/Text/Chinese.cs`.

## Step 2b: a card override

Card values work the same way: the engine decides, and `Overrides` in `src/Card/Upgrades/VarDirections.cs`
holds the exceptions, each with a reason:

```csharp
[("Debt", "Gold")]          = (-1, "gold you lose"),               // the rules say +1 (it's a Gold value)
[("Encore", "EncorePower")] = (1, "a cap on cards you may play"),  // looks like a debuff on you
[("TheScythe", "Damage")]   = (0, "rewritten when played"),        // 0 = never offer this value
```

The cost **family** for card values comes from the value's name (`src/Common/Upgrades/BalanceFamilies.cs`,
`Of`); per-card exceptions are in `CardAttributes.RiskFamily`. Fractional card values (Tank's damage
multipliers) are listed in `src/Card/Upgrades/SteppedValues.cs`.

The tuning numbers for each family (weight, curve) live in `content/balance.jsonc`, so you can adjust prices
there without recompiling.

Multi-hit attacks: `VarDirections.Hits` lists cards that hit several times, which makes Damage upgrades
pricier; the card's own hit count is used when it has one.

## Step 3: test it

1. Build & install (*Ctrl+Shift+B*) with the game closed, then start the game.
2. Start a run with the card or relic (or add it with the game's console), click Tinkerer's Dial and check
   that the Workshop offers the new upgrade, with the right direction and label.
3. If something's wrong, check `%APPDATA%\SlayTheSpire2\logs\godot.log` for lines starting `[Tinker Card]`,
   `[Tinker Relic]` or `[Tinker]`.

## Tips

- Commit before a batch of changes (`git init` once, then *Source Control* panel, *Ctrl+Shift+G*), so you
  can always go back.
- Change a few entries at a time and test them; a wrong direction makes upgrades backfire silently.
- Co-op: everyone needs the same build, because what's upgradable is part of what gets synced.
