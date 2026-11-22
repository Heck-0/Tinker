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

Then check **how the value is used** in the same file (*Ctrl+F* for `DynamicVars`):

- Used in a combat or turn hook (`BeforeCombatStart`, `AfterSideTurnStart`, `ModifyHandDraw`, …): good,
  upgrading it changes play.
- Used only in `AfterObtained`: it happens once on pickup, so upgrading it later does nothing. Skip it.
- Is a higher number **good** or **bad** for you? That's the *direction*: `1` if higher is better, `-1` if
  lower is better (thresholds like "every 3 turns", costs, things that help enemies).
- Is it a fraction (`0.5m`, `1.25m`)? The Workshop sells whole units, so skip it.

## Step 2a: add a relic (Relic Upgrader)

Open `src/Relic/Upgrades/Catalog.cs`. Each `Group(...)` line makes one value upgradable on one or more relics:

```csharp
Group("RelicNames separated by spaces", "ValueName", "family", direction, maximum, labelKey);
//                                                              optional: 1        10000    null
```

Examples already in the file:

```csharp
Group("Anchor HornCleat Orichalcum", "Block", "block");            // more block is better
Group("IvoryTile", "EnergyThreshold", "threshold", -1);            // lower threshold is better
Group("MeatOnTheBone RedSkull", "HpThreshold", "percentage", 1, 90); // cap at 90 (%)
```

Adding Twine and The Abacus would be:

```csharp
Group("Twine TheAbacus", "Block", "block");
```

Pick the **family** that matches the kind of value. It sets how quickly the odds drop as you bet more
units (`src/Relic/Upgrades/Rules.cs`, `Families`). There is no per-family bet limit any more (removed in
1.3.0): the bet stepper just stops once the odds would fall to the minimum chance.

| Family | Use for |
|---|---|
| `damage`, `block` | damage, block (odds drop slowly) |
| `resource` | heal, gold, forge, stars |
| `power` | debuffs/buffs like Weak, Vulnerable, Vigor |
| `scaling` | Strength, Dexterity, Focus |
| `draw` | cards drawn |
| `energy` | energy each turn |
| `persistentEnergy` / `persistentDraw` | permanent energy/draw (boss relics; odds drop fast) |
| `repeat` | extra triggers, orbs, shivs |
| `generation`, `selection` | cards created / cards to choose from |
| `threshold` (direction -1) | "every N cards/turns", requirements |
| `duration` | number of turns an effect lasts |
| `percentage` | % values (set a maximum, normally 100) |
| `protection` | damage/HP-loss reduction |
| `permanentGrowth` | max HP and similar |
| `drawback` (direction -1) | values that hurt you |
| `generic` | anything else |

**Label**: the Workshop shows a name for the value from `Text.Attribute` in `src/Relic/Text/Text.cs`.
Common names (Block, Damage, Cards, Energy, Strength, …) already have labels. For a new value name, add a
`case` there with Spanish and English text, otherwise the raw name shows.

## Step 2b: add a card value (Card Upgrader)

Card Upgrader finds a card's values automatically. It only offers a value if it knows the direction, which
comes from `src/Card/Upgrades/VarDirections.cs`:

- `Overrides`: hand-written entries with a reason. **Add yours here.**
- `Generated`: a big table made by the original author's tool. Leave it alone.

```csharp
[("TwinStrike", "HitCount")] = (1, "hit count"),        // higher is better
[("Pendulum", "Turns")]      = (-1, "every N turns"),     // lower is better
[("SomeCard", "Damage")]     = (0, "rewritten when played"), // 0 = never offer this value
```

The **family** (risk curve) for card values is picked by value name in
`src/Common/Upgrades/BalanceFamilies.cs`, method `Of`. `Damage` → damage, `Block` → block,
`Cards` → draw, `Energy` → energy, anything ending in `Power` → power, unknown names → generic. Per-card exceptions go in `CardAttributes.RiskFamily`. Add a
`case` there if a new value name needs a better family (for example `"HitCount"` → `"repeat"`).

The tuning numbers for each family (weight, curve) live in the shipped `content/balance.jsonc`, so you can
adjust card balance there without recompiling. Its `maxUnits` entries are no longer used (1.3.0 removed bet
limits).

Multi-hit attacks: `VarDirections.Hits` lists how many times a card hits, which makes Damage bets
proportionally riskier. `2` means a fixed 2 hits; `-1` means "read it from the card's hit-count value".

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
