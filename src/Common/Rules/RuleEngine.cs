using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common.Rules;

/// <summary>One IL instruction, reduced to what the rules need. Built by an adapter (Harmony in game, Cecil in tools).</summary>
public sealed class Instr
{
	/// <summary>Opcode name, as IL spells it: "ldstr", "callvirt", "blt.s", ...</summary>
	public string Op = "";

	/// <summary>ldstr text; for ldfld/stfld/ldsfld the field name; for ldarg the parameter name.</summary>
	public string Str;

	/// <summary>Integer / float constant (ldc.*).</summary>
	public double? Num;

	/// <summary>Called method or accessed field: declaring type name (no namespace, no generic arity).</summary>
	public string Type;

	/// <summary>Called method name.</summary>
	public string Method;

	/// <summary>Generic method arguments' type names (e.g. PowerCmd.Apply&lt;StrengthPower&gt; = ["StrengthPower"]).</summary>
	public string[] GenericArgs = Array.Empty<string>();

	/// <summary>Called method's parameter type names.</summary>
	public string[] ParamTypes = Array.Empty<string>();

	/// <summary>Branch target, as an index into the same method's instructions.</summary>
	public int? Target;

	/// <summary>Values popped and pushed (for finding a call's arguments).</summary>
	public int Pops;

	public int Pushes;

	public bool IsCall => Op == "call" || Op == "callvirt" || Op == "newobj";

	public override string ToString() => Op + " " + (Method != null ? Type + "::" + Method : Str ?? Num?.ToString() ?? "");
}

/// <summary>A method body: its name (e.g. "OnUpgrade", "get_FooThreshold", "MoveNext" of an async OnPlay) and instructions.</summary>
public sealed class MethodCode
{
	public string Name = "";

	/// <summary>For a compiler-generated state machine or closure: the method it belongs to (e.g. "OnPlay").</summary>
	public string Owner = "";

	public List<Instr> Body = new List<Instr>();
}

/// <summary>A value on a card or relic, as its CanonicalVars declare it.</summary>
public sealed class VarInfo
{
	public string Name = "";

	/// <summary>The DynamicVar class name: "DamageVar", "PowerVar", "DynamicVar", ...</summary>
	public string Class = "";

	/// <summary>For PowerVar&lt;T&gt;: T's name.</summary>
	public string Power;
}

/// <summary>A card or relic class's code: every method of the class and its nested (compiler-generated) types.</summary>
public sealed class ItemCode
{
	public string Name = "";

	public List<MethodCode> Methods = new List<MethodCode>();

	public List<VarInfo> Vars = new List<VarInfo>();
}

/// <summary>A rule's answer: +1 higher is better, -1 lower is better, 0 don't upgrade; and which rule decided.</summary>
public readonly record struct Verdict(int Sign, string Why);

/// <summary>
/// The generic upgrade rules: from a card's or relic's own code, which way each of its values should be upgraded.
/// A port of tools/divergence.py working on compiled IL instead of decompiled source, so it follows game updates.
/// Rules, first match wins:
/// <list type="number">
/// <item>derived: computed values (CalculatedDamage, ...) are never upgraded.</item>
/// <item>not read: the code never touches the value (it only shows in the text).</item>
/// <item>only on pickup: a relic reads it only when obtained (AfterObtained), so a later upgrade does nothing.</item>
/// <item>upgrade: the game's own OnUpgrade raises (+1) or lowers (-1) it (cards).</item>
/// <item>every N / threshold: it divides something (x % N, x / N) or is a *Threshold property: -1.</item>
/// <item>hurts you: named Lose*, passed to PlayerCmd.Lose*, or the amount of damage dealt to the owner: -1.</item>
/// <item>subtracted (x - N) or compared (x &gt;= N, not a loop counter): 0, a cost or a limit can't be told apart.</item>
/// <item>type: Damage, Block, Cards, Energy, Heal, Gold, ... +1; HpLoss -1.</item>
/// <item>power: a buff applied to yourself or a debuff to enemies +1, the reverse -1.</item>
/// </list>
/// </summary>
public static class RuleEngine
{
	public static readonly IReadOnlyDictionary<string, int> TypeSign = new Dictionary<string, int>
	{
		["DamageVar"] = 1, ["BlockVar"] = 1, ["CardsVar"] = 1, ["EnergyVar"] = 1, ["HealVar"] = 1, ["GoldVar"] = 1,
		["MaxHpVar"] = 1, ["StarsVar"] = 1, ["ForgeVar"] = 1, ["SummonVar"] = 1, ["OstyDamageVar"] = 1,
		["ExtraDamageVar"] = 1, ["HitCountVar"] = 1, ["RepeatVar"] = 1, ["HpLossVar"] = -1,
		["CalculationBaseVar"] = 1, ["CalculationExtraVar"] = 1
	};

	/// <summary>
	/// Values an opcode pops / pushes, from its StackBehaviour names (same in System.Reflection.Emit and Cecil).
	/// Varpop / Varpush (calls, ret) return -1: the adapter works those out from the method's signature.
	/// </summary>
	public static int StackCount(string behaviour)
	{
		switch (behaviour)
		{
		case "Pop0":
		case "Push0":
			return 0;
		case "Varpop":
		case "Varpush":
			return -1;
		case "Push1_push1":
		case "Pop1_pop1":
		case "Popi_pop1":
		case "Popi_popi":
		case "Popi_popi8":
		case "Popi_popr4":
		case "Popi_popr8":
		case "Popref_pop1":
		case "Popref_popi":
			return 2;
		case "Popi_popi_popi":
		case "Popref_popi_popi":
		case "Popref_popi_popi8":
		case "Popref_popi_popr4":
		case "Popref_popi_popr8":
		case "Popref_popi_popref":
		case "Popref_popi_pop1":
			return 3;
		default:
			return 1;
		}
	}

	private static readonly HashSet<string> Derived = new HashSet<string> { "CalculatedDamageVar", "CalculatedBlockVar", "IfUpgradedVar" };

	/// <summary>Formula parts the game's calculated damage / block read by name, outside the card's own code.</summary>
	private static readonly HashSet<string> ReadByFormula = new HashSet<string> { "CalculationBaseVar", "CalculationExtraVar", "ExtraDamageVar" };

	private const string VarSet = "DynamicVarSet";

	/// <param name="powerType">+1 buff, -1 debuff, 0 unknown, for a power class name.</param>
	public static Verdict Decide(ItemCode item, VarInfo v, Func<string, int> powerType)
	{
		if (Derived.Contains(v.Class))
		{
			return new Verdict(0, "derived");
		}
		HashSet<string> names = new HashSet<string>(item.Vars.Select(x => x.Name));
		List<Read> reads = Reads(item, names).Where(r => r.Var == v.Name).ToList();
		if (!ReadByFormula.Contains(v.Class) && reads.All(r => r.Method.Name == "OnUpgrade"))
		{
			return new Verdict(0, "not read");
		}
		// Relics: used only when picked up (AfterObtained, or its async body), so upgrading it later changes nothing.
		List<Read> uses = reads.Where(r => r.Method.Name != "OnUpgrade").ToList();
		if (uses.Count > 0 && uses.All(r => r.Method.Owner == "AfterObtained" || r.Method.Name == "AfterObtained"))
		{
			return new Verdict(0, "only on pickup");
		}
		int upgrade = UpgradeSign(item, names, v.Name);
		if (upgrade != 0)
		{
			return new Verdict(upgrade, "upgrade");
		}
		if (reads.Any(r => NextOp(r, IsDivide)) || reads.Any(r => r.Method.Name.StartsWith("get_", StringComparison.Ordinal) && r.Method.Name.EndsWith("Threshold", StringComparison.Ordinal)))
		{
			return new Verdict(-1, "every N / threshold");
		}
		if (v.Name.StartsWith("Lose", StringComparison.Ordinal) || reads.Any(HurtsOwner))
		{
			return new Verdict(-1, "hurts you");
		}
		if (reads.Any(r => NextOp(r, IsSubtract)))
		{
			return new Verdict(0, "subtracted (cost or discount?)");
		}
		if (reads.Any(IsCompared))
		{
			return new Verdict(0, "compared (threshold or limit?)");
		}
		if (TypeSign.TryGetValue(v.Class, out int sign))
		{
			return new Verdict(sign, "type");
		}
		if (v.Power != null)
		{
			int kind = powerType(v.Power);
			string target = PowerTarget(item, v.Power);
			if (kind != 0 && target != null)
			{
				bool good = (kind == 1) == (target == "self");
				return new Verdict(good ? 1 : -1, "power (" + (kind == 1 ? "buff" : "debuff") + " on " + target + ")");
			}
			return new Verdict(0, kind != 0 ? "power (target unclear)" : "power (type unclear)");
		}
		return new Verdict(0, "unknown");
	}

	// ------------------------------------------------------------------ cost category

	/// <summary>Turn hooks: a value read here pays out every turn (unless the hook checks for turn 1).</summary>
	private static readonly HashSet<string> TurnHooks = new HashSet<string> { "AfterSideTurnStart", "BeforeSideTurnStart", "AfterPlayerTurnStart", "BeforeHandDraw" };

	/// <summary>
	/// The cost category the value's use implies, or null to fall back to the name-based default. How often a bonus
	/// pays out is what the pricier categories are for:
	/// <list type="bullet">
	/// <item>energy read in ModifyMaxEnergy: energy every turn → persistentEnergy;</item>
	/// <item>a draw bonus read in ModifyHandDraw: draw every turn → persistentDraw;</item>
	/// <item>cards read in a turn hook: cards each turn → generation, or selection if the hook asks you to choose;</item>
	/// <item>(a hook that checks the turn number or counts turns pays out only on some turns, so it doesn't count);</item>
	/// <item>max HP gained outside pickup (after combats, when gold comes in, ...) → permanentGrowth.</item>
	/// </list>
	/// </summary>
	public static string Category(ItemCode item, VarInfo v, Verdict verdict)
	{
		if (verdict.Sign <= 0)
		{
			return null;
		}
		HashSet<string> names = new HashSet<string>(item.Vars.Select(x => x.Name));
		List<Read> uses = Reads(item, names).Where(r => r.Var == v.Name && r.Method.Name != "OnUpgrade" && !r.Method.Name.StartsWith("get_", StringComparison.Ordinal)).ToList();
		if (uses.Count == 0)
		{
			return null;
		}
		// "Every turn" only when the hook doesn't check which turn it is (first turn only, every N turns, ...).
		HashSet<string> hooks = new HashSet<string>(uses.Where(r => !ChecksTurn(r.Method)).Select(r => r.Method.Owner));
		bool energy = v.Name == "Energy" || v.Class == "EnergyVar" || v.Name.EndsWith("Energy", StringComparison.Ordinal);
		bool cards = v.Name == "Cards" || v.Class == "CardsVar";
		if (energy && hooks.Contains("ModifyMaxEnergy"))
		{
			return "persistentEnergy";
		}
		if (cards && hooks.Contains("ModifyHandDraw"))
		{
			return "persistentDraw";
		}
		if (cards && hooks.Overlaps(TurnHooks))
		{
			bool choose = uses.Where(r => TurnHooks.Contains(r.Method.Owner) && !ChecksTurn(r.Method)).Any(r => r.Method.Body.Any(x => x.IsCall && x.Method != null
				&& (x.Type == "CardSelectCmd" || x.Method.IndexOf("Choose", StringComparison.Ordinal) >= 0)));
			return choose ? "selection" : "generation";
		}
		if ((v.Name == "MaxHp" || v.Class == "MaxHpVar") && uses.Any(r => r.Method.Owner != "AfterObtained" && r.Method.Body.Any(x => x.IsCall && x.Method == "GainMaxHp")))
		{
			return "permanentGrowth";
		}
		return null;
	}

	/// <summary>The method looks at the turn number or a turns-seen counter: its effect is limited to some turns.</summary>
	private static bool ChecksTurn(MethodCode m)
	{
		return m.Body.Any(x => (x.IsCall && x.Method != null && (x.Method == "get_TurnNumber" || x.Method == "get_RoundNumber" || x.Method.IndexOf("TurnsSeen", StringComparison.Ordinal) >= 0))
			|| (x.Str != null && x.Str.IndexOf("TurnsSeen", StringComparison.Ordinal) >= 0));
	}

	// ------------------------------------------------------------------ reading values

	/// <summary>Where the code reaches a value: DynamicVars["X"] or a typed accessor like DynamicVars.Damage.</summary>
	public sealed class Read
	{
		public MethodCode Method;

		public string Var;

		/// <summary>First instruction of the accessor (the ldstr, or the typed getter).</summary>
		public int Start;

		/// <summary>Last instruction: the .BaseValue / .IntValue getter if there is one, else the accessor.</summary>
		public int End;
	}

	public static IEnumerable<Read> Reads(ItemCode item, ISet<string> names)
	{
		foreach (MethodCode m in item.Methods)
		{
			List<Instr> b = m.Body;
			for (int i = 0; i < b.Count; i++)
			{
				string name = null;
				int start = i;
				if (b[i].IsCall && b[i].Type == VarSet && b[i].Method == "get_Item" && i > 0 && b[i - 1].Op == "ldstr")
				{
					name = b[i - 1].Str;
					start = i - 1;
				}
				else if (b[i].IsCall && b[i].Type == VarSet && b[i].Method.StartsWith("get_", StringComparison.Ordinal) && b[i].Method != "get_Item" && b[i].Method != "get_Keys" && b[i].Method != "get_Values" && b[i].Method != "get_Count")
				{
					name = Resolve(b[i].Method.Substring(4), names);
				}
				if (name == null)
				{
					continue;
				}
				int end = i;
				if (i + 1 < b.Count && b[i + 1].IsCall && (b[i + 1].Method == "get_BaseValue" || b[i + 1].Method == "get_IntValue"))
				{
					end = i + 1;
				}
				yield return new Read { Method = m, Var = name, Start = start, End = end };
			}
		}
	}

	/// <summary>An accessor or UpgradeValueBy target name to the value's name: as is, or with "Power" / "Var" added.</summary>
	private static string Resolve(string name, ISet<string> names)
	{
		if (names.Contains(name))
		{
			return name;
		}
		foreach (string candidate in new[] { name + "Power", name + "Var" })
		{
			if (names.Contains(candidate))
			{
				return candidate;
			}
		}
		return name;
	}

	// ------------------------------------------------------------------ rule: the game's own upgrade

	private static int UpgradeSign(ItemCode item, ISet<string> names, string var)
	{
		foreach (MethodCode m in item.Methods.Where(x => x.Name == "OnUpgrade"))
		{
			List<Instr> b = m.Body;
			List<Read> reads = Reads(new ItemCode { Methods = new List<MethodCode> { m } }, names).ToList();
			for (int i = 0; i < b.Count; i++)
			{
				if (!(b[i].IsCall && b[i].Method == "UpgradeValueBy"))
				{
					continue;
				}
				Read r = reads.LastOrDefault(x => x.End < i);
				if (r == null || r.Var != var)
				{
					continue;
				}
				int sign = DecimalSign(b, r.End + 1, i);
				if (sign != 0)
				{
					return sign;
				}
			}
		}
		return 0;
	}

	/// <summary>The sign of a decimal constant built between from (inclusive) and to (exclusive).</summary>
	private static int DecimalSign(List<Instr> b, int from, int to)
	{
		List<double> numbers = new List<double>();
		for (int i = from; i < to; i++)
		{
			Instr x = b[i];
			if (x.Op == "ldsfld" && x.Type == "Decimal")
			{
				return x.Str == "One" ? 1 : (x.Str == "MinusOne" ? -1 : 0);
			}
			if (x.Num.HasValue)
			{
				numbers.Add(x.Num.Value);
			}
			if (x.Op == "newobj" && x.Type == "Decimal")
			{
				if (x.ParamTypes.Length == 5 && numbers.Count >= 5)
				{
					// decimal(lo, mid, hi, isNegative, scale)
					List<double> n = numbers.Skip(numbers.Count - 5).ToList();
					bool zero = n[0] == 0 && n[1] == 0 && n[2] == 0;
					return zero ? 0 : (n[3] != 0 ? -1 : 1);
				}
				return numbers.Count > 0 ? Math.Sign(numbers[numbers.Count - 1]) : 0;
			}
			if (x.IsCall && x.Type == "Decimal" && x.Method == "op_Implicit" && numbers.Count > 0)
			{
				return Math.Sign(numbers[numbers.Count - 1]);
			}
		}
		return numbers.Count > 0 ? Math.Sign(numbers[numbers.Count - 1]) : 0;
	}

	// ------------------------------------------------------------------ rules: how the value is used

	private static bool IsConversion(Instr x)
	{
		return x.Op.StartsWith("conv.", StringComparison.Ordinal)
			|| (x.IsCall && (x.Type == "Decimal" || x.Type == "Convert") && (x.Method == "op_Explicit" || x.Method == "op_Implicit" || x.Method == "ToInt32" || x.Method == "ToDecimal"));
	}

	/// <summary>True if, right after the value is read (skipping casts), the next operation matches: the value is its right-hand operand.</summary>
	private static bool NextOp(Read r, Func<Instr, bool> test)
	{
		List<Instr> b = r.Method.Body;
		int i = r.End + 1;
		while (i < b.Count && IsConversion(b[i]))
		{
			i++;
		}
		return i < b.Count && test(b[i]);
	}

	private static bool IsDivide(Instr x)
	{
		return x.Op == "rem" || x.Op == "rem.un" || x.Op == "div" || x.Op == "div.un"
			|| (x.IsCall && x.Type == "Decimal" && (x.Method == "op_Division" || x.Method == "op_Modulus" || x.Method == "Divide" || x.Method == "Remainder"));
	}

	private static bool IsSubtract(Instr x)
	{
		return x.Op == "sub" || x.Op == "sub.ovf" || x.Op == "sub.ovf.un"
			|| (x.IsCall && x.Type == "Decimal" && (x.Method == "op_Subtraction" || x.Method == "Subtract"));
	}

	private static readonly HashSet<string> CompareOps = new HashSet<string>
	{
		"clt", "cgt", "ceq", "clt.un", "cgt.un", "bge", "bgt", "ble", "blt", "beq", "bne.un", "bge.un", "bgt.un", "ble.un", "blt.un",
		"bge.s", "bgt.s", "ble.s", "blt.s", "beq.s", "bne.un.s", "bge.un.s", "bgt.un.s", "ble.un.s", "blt.un.s"
	};

	private static readonly HashSet<string> CompareMethods = new HashSet<string>
	{
		"op_GreaterThan", "op_GreaterThanOrEqual", "op_LessThan", "op_LessThanOrEqual", "op_Equality", "op_Inequality", "Compare"
	};

	/// <summary>Compared against something, except as a loop's bound (a backward branch: for (i = 0; i &lt; N; i++)).</summary>
	private static bool IsCompared(Read r)
	{
		List<Instr> b = r.Method.Body;
		int i = r.End + 1;
		while (i < b.Count && IsConversion(b[i]))
		{
			i++;
		}
		if (i >= b.Count)
		{
			return false;
		}
		Instr x = b[i];
		if (CompareOps.Contains(x.Op))
		{
			return !(x.Target.HasValue && x.Target.Value <= i);
		}
		if (x.IsCall && x.Type == "Decimal" && CompareMethods.Contains(x.Method))
		{
			// The decimal comparison's result feeds a branch: a backward one is a loop bound.
			int j = i + 1;
			return !(j < b.Count && b[j].Target.HasValue && b[j].Target.Value <= j);
		}
		return false;
	}

	/// <summary>Gold, energy, ... taken from the owner (PlayerCmd.Lose*), or the amount of damage dealt to the owner.</summary>
	private static bool HurtsOwner(Read r)
	{
		List<Instr> b = r.Method.Body;
		for (int i = r.End + 1; i < b.Count && i <= r.End + 25; i++)
		{
			Instr x = b[i];
			if (!x.IsCall)
			{
				continue;
			}
			if (x.Type == "PlayerCmd" && x.Method.StartsWith("Lose", StringComparison.Ordinal))
			{
				return ArgContains(b, i, r);
			}
			if (x.Type == "CreatureCmd" && x.Method == "Damage" && x.ParamTypes.Length >= 3)
			{
				int amountArg = ArgIndex(x, 2);
				(int s, int e) target = ArgSpan(b, i, amountArg - 1);
				(int s, int e) amount = ArgSpan(b, i, amountArg);
				// The target must be the owner itself (ends in Owner.Creature), not e.g. Owner.Creature.CombatState.HittableEnemies.
				return amount.s <= r.Start && r.End <= amount.e && target.s >= 0 && b[target.e].IsCall && b[target.e].Method == "get_Creature" && IsOwnerCreature(b, target.s, target.e);
			}
		}
		return false;
	}

	/// <summary>CreatureCmd.Damage(choiceContext, target, amount, ...): the amount's position, allowing for no context.</summary>
	private static int ArgIndex(Instr call, int withContext)
	{
		return (call.ParamTypes.Length > 0 && call.ParamTypes[0].Contains("Context")) ? withContext : withContext - 1;
	}

	private static bool ArgContains(List<Instr> b, int call, Read r)
	{
		for (int a = 0; a < b[call].ParamTypes.Length; a++)
		{
			(int s, int e) span = ArgSpan(b, call, a);
			if (span.s >= 0 && span.s <= r.Start && r.End <= span.e)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// The instructions that push argument <paramref name="arg"/> of the call at <paramref name="call"/>, by walking back
	/// over the stack effects. (-1, -1) if it can't be told (e.g. a branch in the middle).
	/// </summary>
	public static (int Start, int End) ArgSpan(List<Instr> b, int call, int arg)
	{
		bool instance = b[call].Op == "callvirt" || (b[call].Op == "call" && b[call].Pops > b[call].ParamTypes.Length);
		int count = b[call].ParamTypes.Length + (instance ? 1 : 0);
		int index = arg + (instance ? 1 : 0);
		if (index < 0 || index >= count)
		{
			return (-1, -1);
		}
		// Walking back from the call, "produced" counts the values the instructions from i to call-1 leave on the
		// stack. Argument k starts where produced first reaches count - k (the last argument is on top).
		int[] starts = Enumerable.Repeat(-1, count).ToArray();
		int produced = 0;
		int need = 1;
		for (int i = call - 1; i >= 0 && need <= count; i--)
		{
			Instr x = b[i];
			// Short forward branches inside an argument (x?.Y, a ? b : c) still add up on a straight walk; a return,
			// throw or backward jump means we left the argument.
			if (x.Op == "ret" || x.Op == "throw" || (x.Target.HasValue && x.Target.Value <= i))
			{
				return (-1, -1);
			}
			produced += x.Pushes - x.Pops;
			while (need <= count && produced >= need)
			{
				starts[count - need] = i;
				need++;
			}
		}
		if (starts[index] < 0)
		{
			return (-1, -1);
		}
		int end = (index + 1 < count) ? starts[index + 1] - 1 : call - 1;
		return (end < starts[index]) ? (-1, -1) : (starts[index], end);
	}

	private static bool IsOwnerCreature(List<Instr> b, int s, int e)
	{
		bool owner = false;
		for (int i = s; i <= e; i++)
		{
			if (b[i].IsCall && b[i].Method == "get_Owner")
			{
				owner = true;
			}
			if (owner && b[i].IsCall && b[i].Method == "get_Creature")
			{
				return true;
			}
		}
		return false;
	}

	// ------------------------------------------------------------------ rule: powers

	/// <summary>"self", "enemy", or null when the PowerCmd.Apply&lt;P&gt; calls disagree or can't be read.</summary>
	public static string PowerTarget(ItemCode item, string power)
	{
		HashSet<string> targets = new HashSet<string>();
		foreach (MethodCode m in item.Methods)
		{
			List<Instr> b = m.Body;
			for (int i = 0; i < b.Count; i++)
			{
				Instr x = b[i];
				if (!(x.IsCall && x.Type == "PowerCmd" && x.Method == "Apply" && x.GenericArgs.Length == 1 && x.GenericArgs[0] == power) || x.ParamTypes.Length < 2)
				{
					continue;
				}
				(int s, int e) span = ArgSpan(b, i, ArgIndex(x, 1));
				string target = span.s < 0 ? "?" : Classify(b, span.s, span.e);
				// An unnamed loop variable (foreach (Creature item in ...HittableEnemies)): an enemy if the method walks an enemy list.
				if (target == "?" && span.s >= 0 && span.s == span.e && b[span.s].Op.StartsWith("ldloc", StringComparison.Ordinal)
					&& b.Any(y => y.IsCall && y.Method != null && y.Method.IndexOf("nem", StringComparison.Ordinal) >= 0))
				{
					target = "enemy";
				}
				targets.Add(target);
			}
		}
		if (targets.Count == 1 && !targets.Contains("?"))
		{
			return targets.First();
		}
		return null;
	}

	private static string Classify(List<Instr> b, int s, int e)
	{
		if (IsOwnerCreature(b, s, e))
		{
			return "self";
		}
		for (int i = s; i <= e; i++)
		{
			if (b[i].Str != null && b[i].Str.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0 && i + 1 <= e && b[i + 1].IsCall && b[i + 1].Method == "get_Creature")
			{
				return "self";
			}
		}
		for (int i = s; i <= e; i++)
		{
			Instr x = b[i];
			if (x.IsCall && x.Method == "get_Target")
			{
				return "enemy";
			}
			// A field or local named like an enemy, or a call like CombatState.HittableEnemies / GetEnemies().
			foreach (string n in new[] { x.Str, x.Method })
			{
				if (n != null && (n.IndexOf("Target", StringComparison.Ordinal) >= 0 || n.IndexOf("nem", StringComparison.Ordinal) >= 0 || n.StartsWith("<item>", StringComparison.Ordinal) || n.StartsWith("<e>", StringComparison.Ordinal)))
				{
					return "enemy";
				}
			}
		}
		return "?";
	}
}
