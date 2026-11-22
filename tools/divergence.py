"""Divergence report: generic upgrade rules vs. the mod's hand-written tables.

A small "mock engine" decides, from the decompiled game alone, which way each card and relic value should
be upgraded (+1 = higher is better, -1 = lower is better, 0 = can't tell). It then compares that with what the
mod actually does, so we can see how much of the hand-written tables generic rules could replace.

Rules, in order (the first that applies wins):
  1. upgrade  - the game's own upgrade (OnUpgrade) raises or lowers the value      (cards only)
  2. type     - the value's type: Damage/Block/Heal/Gold/Cards/Energy/... up, HpLoss down
  3. power    - a power amount: buff on yourself or debuff on enemies = up, the reverse = down
  otherwise unknown. Relics whose effect only happens on pickup (HasUponPickupEffect and no other hook)
  are marked "pickup" and not upgraded.

The mod's answer:
  cards  - VarDirections.Overrides, then VarDirections.Generated (confidence >= 1)   (src/Card/Upgrades)
  relics - Catalog.cs Group(...) lines                                              (src/Relic/Upgrades)

Usage:  python tools/divergence.py [game_src] [mod_src] [out_dir]
Output: <out_dir>/divergence_cards.csv, divergence_relics.csv, DIVERGENCE.md
"""
import collections, csv, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser(r"~\Documents\StS2ModSource\_sts2_full")
MOD = sys.argv[2] if len(sys.argv) > 2 else os.path.join(HERE, "..", "src")
OUT = sys.argv[3] if len(sys.argv) > 3 else os.path.join(HERE, "coverage")
os.makedirs(OUT, exist_ok=True)

CARDS = os.path.join(GAME, "MegaCrit.Sts2.Core.Models.Cards")
RELICS = os.path.join(GAME, "MegaCrit.Sts2.Core.Models.Relics")
POWERS = os.path.join(GAME, "MegaCrit.Sts2.Core.Models.Powers")

# ---------------------------------------------------------------------------------------- game source

TYPE_NAME = {  # typed var class -> default name
    "DamageVar": "Damage", "BlockVar": "Block", "CardsVar": "Cards", "EnergyVar": "Energy", "HealVar": "Heal",
    "GoldVar": "Gold", "MaxHpVar": "MaxHp", "HpLossVar": "HpLoss", "StarsVar": "Stars", "ForgeVar": "Forge",
    "SummonVar": "Summon", "OstyDamageVar": "OstyDamage", "ExtraDamageVar": "ExtraDamage", "HitCountVar": "HitCount",
    "RepeatVar": "Repeat", "CalculationBaseVar": "CalculationBase", "CalculationExtraVar": "CalculationExtra",
    "CalculatedDamageVar": "CalculatedDamage", "CalculatedBlockVar": "CalculatedBlock", "IfUpgradedVar": "IfUpgraded",
}
TYPE_SIGN = {  # rule 2
    "DamageVar": 1, "BlockVar": 1, "CardsVar": 1, "EnergyVar": 1, "HealVar": 1, "GoldVar": 1, "MaxHpVar": 1,
    "StarsVar": 1, "ForgeVar": 1, "SummonVar": 1, "OstyDamageVar": 1, "ExtraDamageVar": 1, "HitCountVar": 1,
    "RepeatVar": 1, "HpLossVar": -1,
    "CalculationBaseVar": 1, "CalculationExtraVar": 1,  # the two parts of a formula value (base + per-X)
}
NOT_UPGRADABLE_TYPES = {"CalculatedDamageVar", "CalculatedBlockVar", "IfUpgradedVar"}


def read(path):
    with open(path, encoding="utf-8", errors="replace") as f:
        return f.read()


def block(text, start):
    """The {...} block that starts at or after index start."""
    i = text.find("{", start)
    if i < 0:
        return ""
    depth = 0
    for j in range(i, len(text)):
        if text[j] == "{":
            depth += 1
        elif text[j] == "}":
            depth -= 1
            if depth == 0:
                return text[i:j + 1]
    return text[i:]


def canonical_vars(src):
    """[(name, type, power)] from CanonicalVars."""
    m = re.search(r"CanonicalVars\s*=>", src)
    if not m:
        return []
    end = src.find(";\n", m.end())
    body = src[m.end():end if end > 0 else None]
    out = []
    for vm in re.finditer(r"new\s+(\w+?)(?:<(\w+)>)?\(\s*(\"(\w+)\")?", body):
        cls, power, _, literal = vm.group(1), vm.group(2), vm.group(3), vm.group(4)
        if not cls.endswith("Var") or cls == "DynamicVar" and not literal:
            if cls != "DynamicVar":
                continue
        if cls == "PowerVar" and power:
            name = literal or power
        elif cls == "DynamicVar":
            name = literal
        else:
            name = literal or TYPE_NAME.get(cls) or cls[:-3]
        if name:
            out.append((name, cls, power))
    return out


_power_types = {}


def power_type(power):
    if power not in _power_types:
        path = os.path.join(POWERS, power + ".cs")
        t = 0
        if os.path.exists(path):
            m = re.search(r"PowerType\.(Buff|Debuff)", read(path))
            if m:
                t = 1 if m.group(1) == "Buff" else -1
        _power_types[power] = t
    return _power_types[power]


def power_target(src, power):
    """'self', 'enemy' or '' from the PowerCmd.Apply<power>(...) calls."""
    targets = set()
    for m in re.finditer(r"PowerCmd\.Apply<" + re.escape(power) + r">\(([^;]*)\);", src):
        parts = m.group(1).split(",")
        if len(parts) < 2:
            continue
        # Apply<P>(choiceContext, target, amount, source, card); some calls leave out the choice context.
        target = parts[1] if re.search(r"[Cc]ontext", parts[0]) else parts[0]
        if re.search(r"Owner\.Creature|\bOwner\b\s*$|player\.Creature", target):
            targets.add("self")
        elif re.search(r"Target|[Ee]nem|\bitem\b|\be\b", target):
            targets.add("enemy")
        else:
            targets.add("?")  # e.g. a hook's "creature" parameter: could be anyone
    if len(targets) == 1 and "?" not in targets:
        return targets.pop()
    return ""


def upgrade_signs(src, var_names):
    """{var name: sign} from OnUpgrade()."""
    m = re.search(r"override\s+void\s+OnUpgrade\s*\(\s*\)", src)
    if not m:
        return {}
    body = block(src, m.end())
    out = {}
    for um in re.finditer(r"DynamicVars(?:\.(\w+)|\[\"(\w+)\"\])\.UpgradeValueBy\(\s*(-?[\d.]+)m?\s*\)", body):
        accessor, literal, amount = um.group(1), um.group(2), float(um.group(3))
        name = literal or accessor
        if name not in var_names:
            for candidate in (name + "Power", name + "Var"):
                if candidate in var_names:
                    name = candidate
                    break
        if amount != 0:
            out[name] = 1 if amount > 0 else -1
    return out


def accessor_pattern(name):
    """How the code reads this value: DynamicVars.Cards, DynamicVars.Vulnerable (for VulnerablePower) or DynamicVars["X"]."""
    names = {name}
    if name.endswith("Power"):
        names.add(name[:-5])
    alts = "|".join(re.escape(n) for n in names)
    return r"DynamicVars(?:\.(?:" + alts + r")|\[\"(?:" + alts + r")\"\])\.(?:IntValue|BaseValue)"


CAST = r"\(?\s*(?:\((?:int|decimal)\))?\s*base\."


def uses(src, name, before):
    """True if the value is read right after the regex `before` (e.g. a % sign)."""
    return re.search(before + r"\s*" + CAST + accessor_pattern(name), src) is not None


def is_every_n(src, name):
    """'Every N': the value divides something (x % N, x / N), so lower is better."""
    return uses(src, name, r"(?:%|/)")


def is_subtracted(src, name):
    """Something minus the value (e.g. cards drawn - N): lower is better."""
    return uses(src, name, r"(?<![\w)])\w[\w.]*\s*-")


def is_compared(src, name):
    """Compared against (x >= N, x < N, ...) by something other than a loop counter: a threshold or an
    allowance, and the code alone can't tell which (Kunai-like threshold vs. Velvet Choker-like limit)."""
    return uses(src, name, r"(?<![\w.])(?!i\s)[A-Za-z_][\w.()]*\s*(?:>=|<=|>|<|==|!=)")


def hurts_owner(src, name):
    """Gold or energy taken from the owner, or damage dealt to the owner (the target argument), using this value."""
    acc = r"DynamicVars(?:\." + re.escape(name) + r"|\[\"" + re.escape(name) + r"\"\])"
    if re.search(r"PlayerCmd\.Lose\w+\([^;]*" + acc, src):
        return True
    for m in re.finditer(r"CreatureCmd\.Damage\(([^;]*)\);", src):
        parts = [p.strip() for p in m.group(1).split(",")]
        # Damage(choiceContext, target, amount, ...)
        if len(parts) >= 3 and re.search(r"Owner\.Creature$", parts[1]) and re.search(acc, parts[2]):
            return True
    return False


def own_type(src):
    """The card's own type from its constructor: base(cost, CardType.X, ...)."""
    m = re.search(r":\s*base\([^)]*CardType\.(\w+)", src)
    return m.group(1) if m else ""


def rule_for(src, name, cls, power, upgrades):
    if cls in NOT_UPGRADABLE_TYPES:
        return 0, "derived"
    if name in upgrades:
        return upgrades[name], "upgrade"
    if is_every_n(src, name) or uses(src, name, r"\w*Threshold\s*=>"):
        return -1, "every N / threshold"
    if name.startswith("Lose") or hurts_owner(src, name):
        return -1, "hurts you"
    if is_subtracted(src, name):
        return 0, "subtracted (cost or discount?)"
    if is_compared(src, name):
        return 0, "compared (threshold or limit?)"
    if cls in TYPE_SIGN and not (cls != "DamageVar" and cls != "BlockVar" and name != TYPE_NAME.get(cls)):
        return TYPE_SIGN[cls], "type"
    if cls in TYPE_SIGN:  # a typed var with a custom name (e.g. DamageVar("OnKillDamage")): still the type's direction
        return TYPE_SIGN[cls], "type"
    if power:
        kind, target = power_type(power), power_target(src, power)
        if kind and target:
            good = (kind == 1) == (target == "self")
            return (1 if good else -1), f"power ({'buff' if kind == 1 else 'debuff'} on {target})"
        return 0, "power (target unclear)" if kind else "power (type unclear)"
    return 0, "unknown"


# ---------------------------------------------------------------------------------------- mod tables

def mod_card_signs():
    text = read(os.path.join(MOD, "Card", "Upgrades", "VarDirections.cs"))
    def table(name):
        start = text.index(name + " = new Dictionary")
        return block(text, start)
    signs = {}
    for m in re.finditer(r'\[\("(\w+)",\s*"(\w+)"\)\]\s*=\s*\((-?\d+),\s*(-?\d+)\)', table("Generated")):
        if int(m.group(4)) >= 1:
            signs[(m.group(1), m.group(2))] = (int(m.group(3)), "Generated")
    for m in re.finditer(r'\[\("(\w+)",\s*"(\w+)"\)\]\s*=\s*\((-?\d+),', table("Overrides")):
        signs[(m.group(1), m.group(2))] = (int(m.group(3)), "Overrides")
    return signs


def mod_relic_signs():
    text = read(os.path.join(MOD, "Relic", "Upgrades", "Catalog.cs"))
    signs = {}
    for m in re.finditer(r'Group\("([^"]+)",\s*"(\w+)",\s*"(\w+)"(?:,\s*(-?\d+))?', text):
        for relic in m.group(1).split():
            signs[(relic, m.group(2))] = (int(m.group(4) or 1), m.group(3))
    return signs


# ---------------------------------------------------------------------------------------- compare

def verdict(rule, mod):
    if mod == 0 and rule == 0:
        return "neither"
    if mod == 0:
        return "rules only"
    if rule == 0:
        return "mod only"
    return "agree" if rule == mod else "DISAGREE"


def cards():
    mod = mod_card_signs()
    rows = []
    for fn in sorted(os.listdir(CARDS)):
        if not fn.endswith(".cs"):
            continue
        card = fn[:-3]
        src = read(os.path.join(CARDS, fn))
        if not re.search(r"class\s+" + card + r"\s*:\s*CardModel", src):
            continue
        cvars = canonical_vars(src)
        names = {n for n, _, _ in cvars}
        ups = upgrade_signs(src, names)
        for name, cls, power in cvars:
            rule, why = rule_for(src, name, cls, power, ups)
            msign, mwhere = mod.get((card, name), (0, ""))
            # Damage and Block on attacks / block cards are handled generically by the mod, not by the tables.
            if msign == 0 and name in ("Damage", "Block") and cls in ("DamageVar", "BlockVar"):
                msign, mwhere = 1, "generic"
            rows.append(dict(item=card, value=name, type=cls + (f"<{power}>" if power else ""), rule=rule, rule_why=why,
                             mod=msign, mod_source=mwhere, verdict=verdict(rule, msign)))
    return rows


def relics():
    mod = mod_relic_signs()
    rows = []
    for fn in sorted(os.listdir(RELICS)):
        if not fn.endswith(".cs"):
            continue
        relic = fn[:-3]
        src = read(os.path.join(RELICS, fn))
        if not re.search(r"class\s+" + relic + r"\s*:\s*RelicModel", src):
            continue
        hooks = set(re.findall(r"public\s+override\s+(?:async\s+)?\w+(?:<[^>]+>)?\s+(\w+)\s*\(", src))
        pickup_only = "HasUponPickupEffect => true" in src and hooks <= {"AfterObtained"}
        for name, cls, power in canonical_vars(src):
            if pickup_only:
                rule, why = 0, "pickup only"
            else:
                rule, why = rule_for(src, name, cls, power, {})
            msign, family = mod.get((relic, name), (0, ""))
            rows.append(dict(item=relic, value=name, type=cls + (f"<{power}>" if power else ""), rule=rule, rule_why=why,
                             mod=msign, mod_source=family, verdict=verdict(rule, msign)))
    return rows


def write(rows, path):
    with open(path, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)


def summary(title, rows):
    lines = [f"## {title}", ""]
    count = collections.Counter(r["verdict"] for r in rows)
    lines.append(f"{len(rows)} values on {len({r['item'] for r in rows})} items.")
    lines.append("")
    lines.append("| Verdict | Values | Meaning |")
    lines.append("|---|---:|---|")
    meaning = {
        "agree": "rules and mod give the same direction",
        "DISAGREE": "rules and mod give opposite directions",
        "mod only": "the mod upgrades it; the rules can't tell (needs a list entry)",
        "rules only": "the rules would upgrade it; the mod doesn't",
        "neither": "nobody upgrades it",
    }
    for v in ("agree", "DISAGREE", "mod only", "rules only", "neither"):
        lines.append(f"| {v} | {count.get(v, 0)} | {meaning[v]} |")
    lines.append("")
    why = collections.Counter(r["rule_why"] for r in rows if r["verdict"] in ("agree", "DISAGREE", "rules only"))
    lines.append("Rules used where the rules decided: " + ", ".join(f"{k} {v}" for k, v in why.most_common()) + ".")
    lines.append("")
    dis = [r for r in rows if r["verdict"] == "DISAGREE"]
    if dis:
        lines.append(f"### Disagreements ({len(dis)})")
        lines.append("")
        lines.append("| Item | Value | Type | Rules | Why | Mod | Mod source |")
        lines.append("|---|---|---|---:|---|---:|---|")
        for r in dis:
            lines.append(f"| {r['item']} | {r['value']} | {r['type']} | {r['rule']:+d} | {r['rule_why']} | {r['mod']:+d} | {r['mod_source']} |")
        lines.append("")
    return lines


def main():
    c, r = cards(), relics()
    write(c, os.path.join(OUT, "divergence_cards.csv"))
    write(r, os.path.join(OUT, "divergence_relics.csv"))
    md = ["# Divergence: generic rules vs. the mod's tables", "",
          "Generated by `tools/divergence.py`. Full rows: `divergence_cards.csv`, `divergence_relics.csv`.", ""]
    md += summary("Cards", c) + summary("Relics", r)
    with open(os.path.join(OUT, "DIVERGENCE.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(md) + "\n")
    for title, rows in (("cards", c), ("relics", r)):
        print(title, dict(collections.Counter(x["verdict"] for x in rows)))


if __name__ == "__main__":
    main()
