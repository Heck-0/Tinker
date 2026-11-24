using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Tinker.Common.Rules;

/// <summary>
/// The rule engine in the game: reads a card's or relic's compiled code (through Harmony) and its values, and
/// asks <see cref="RuleEngine"/> which way each value should be upgraded. Results are cached per class.
/// </summary>
public static class GameRules
{
	private static readonly Dictionary<Type, ItemCode> Code = new Dictionary<Type, ItemCode>();

	private static readonly Dictionary<(Type, string), Verdict> Verdicts = new Dictionary<(Type, string), Verdict>();

	private static Dictionary<string, int> _powerTypes;

	/// <summary>+1 higher is better, -1 lower is better, 0 don't upgrade, for a value of this card / relic.</summary>
	public static Verdict Decide(AbstractModel model, string var)
	{
		if (model == null || string.IsNullOrEmpty(var))
		{
			return new Verdict(0, "none");
		}
		Type type = model.GetType();
		lock (Code)
		{
			if (Verdicts.TryGetValue((type, var), out Verdict cached))
			{
				return cached;
			}
			Verdict verdict;
			try
			{
				ItemCode item = ItemFor(model);
				VarInfo info = item.Vars.FirstOrDefault(v => v.Name == var);
				verdict = (info == null) ? new Verdict(0, "not a value") : RuleEngine.Decide(item, info, PowerType);
			}
			catch (Exception ex)
			{
				verdict = new Verdict(0, "error");
				Godot.GD.PushWarning($"[Tinker] Rules failed for {type.Name}.{var}: {ex.Message}");
			}
			Verdicts[(type, var)] = verdict;
			return verdict;
		}
	}

	/// <summary>The cost category the value's use implies (see <see cref="RuleEngine.Category"/>), or null.</summary>
	public static string Category(AbstractModel model, string var, Verdict verdict)
	{
		lock (Code)
		{
			try
			{
				ItemCode item = ItemFor(model);
				VarInfo info = item.Vars.FirstOrDefault(v => v.Name == var);
				return (info == null) ? null : RuleEngine.Category(item, info, verdict);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}

	private static readonly Dictionary<string, string> PowerTiers = new Dictionary<string, string>();

	/// <summary>The trigger-tier category of a relic value, from the hooks it's read in (see <see cref="RuleEngine.TriggerFamily(ItemCode, string)"/>), or null.</summary>
	public static string TriggerFamily(AbstractModel model, string var)
	{
		lock (Code)
		{
			try
			{
				return RuleEngine.TriggerFamily(ItemFor(model), var);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}

	/// <summary>The trigger-tier category of a power's amount, from the hooks where it reads Amount, or null.</summary>
	public static string PowerTriggerFamily(string powerName)
	{
		if (string.IsNullOrEmpty(powerName))
		{
			return null;
		}
		lock (Code)
		{
			if (PowerTiers.TryGetValue(powerName, out string cached))
			{
				return cached;
			}
			string family = null;
			try
			{
				PowerModel power = ModelDb.AllPowers.FirstOrDefault(p => p.GetType().Name == powerName);
				if (power != null)
				{
					ItemCode item = new ItemCode { Name = powerName };
					AddMethods(power.GetType(), null, item.Methods);
					family = RuleEngine.PowerTriggerFamily(item);
				}
			}
			catch (Exception)
			{
				family = null;
			}
			PowerTiers[powerName] = family;
			return family;
		}
	}

	/// <summary>The class's values as its DynamicVars hold them.</summary>
	public static IReadOnlyList<VarInfo> Vars(AbstractModel model)
	{
		lock (Code)
		{
			return ItemFor(model).Vars;
		}
	}

	private static bool _dumped;

	/// <summary>
	/// Diagnostics: if a file named "rules_dump" sits next to the mod's DLL, writes the engine's verdict for every
	/// game card and relic value to "rules_dump.tsv" there (once per session), to compare with tools' results.
	/// </summary>
	public static void DumpIfRequested()
	{
		if (_dumped)
		{
			return;
		}
		_dumped = true;
		string dir = System.IO.Path.GetDirectoryName(typeof(GameRules).Assembly.Location) ?? ".";
		if (!System.IO.File.Exists(System.IO.Path.Combine(dir, "rules_dump")))
		{
			return;
		}
		List<string> lines = new List<string> { "kind\titem\tvalue\tengine\tengine_why" };
		foreach ((string kind, IEnumerable<AbstractModel> models) in new (string, IEnumerable<AbstractModel>)[2]
		{
			("card", ModelDb.AllCards.Cast<AbstractModel>()),
			("relic", ModelDb.AllRelics.Cast<AbstractModel>())
		})
		{
			foreach (AbstractModel model in models.GroupBy(m => m.GetType()).Select(g => g.First()).OrderBy(m => m.GetType().Name, StringComparer.Ordinal))
			{
				foreach (VarInfo v in Vars(model))
				{
					Verdict verdict = Decide(model, v.Name);
					lines.Add($"{kind}\t{model.GetType().Name}\t{v.Name}\t{verdict.Sign}\t{verdict.Why}");
				}
			}
		}
		System.IO.File.WriteAllLines(System.IO.Path.Combine(dir, "rules_dump.tsv"), lines);
		Godot.GD.Print("[Tinker] Wrote rules_dump.tsv (", lines.Count - 1, " values).");
	}

	private static ItemCode ItemFor(AbstractModel model)
	{
		Type type = model.GetType();
		if (Code.TryGetValue(type, out ItemCode item))
		{
			return item;
		}
		item = new ItemCode { Name = type.Name };
		AddMethods(type, null, item.Methods);
		DynamicVarSet vars = (model as CardModel)?.DynamicVars ?? (model as RelicModel)?.DynamicVars;
		if (vars != null)
		{
			foreach (string key in vars.Keys)
			{
				Type varType = vars[key].GetType();
				item.Vars.Add(new VarInfo
				{
					Name = key,
					Class = Clean(varType.Name),
					Power = (varType.IsGenericType && Clean(varType.Name) == "PowerVar") ? varType.GetGenericArguments()[0].Name : null
				});
			}
		}
		Code[type] = item;
		return item;
	}

	private static int PowerType(string name)
	{
		if (_powerTypes == null)
		{
			_powerTypes = new Dictionary<string, int>();
			foreach (PowerModel power in ModelDb.AllPowers)
			{
				_powerTypes[power.GetType().Name] = power.Type switch
				{
					MegaCrit.Sts2.Core.Entities.Powers.PowerType.Buff => 1,
					MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff => -1,
					_ => 0
				};
			}
		}
		return _powerTypes.TryGetValue(name, out int t) ? t : 0;
	}

	// ------------------------------------------------------------------ IL through Harmony

	private static string Clean(string name)
	{
		int i = name.IndexOf('`');
		return (i < 0) ? name : name.Substring(0, i);
	}

	private static void AddMethods(Type type, string owner, List<MethodCode> into)
	{
		const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		foreach (MethodBase m in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
		{
			if (m.IsAbstract || m.GetMethodBody() == null)
			{
				continue;
			}
			try
			{
				into.Add(new MethodCode { Name = m.Name, Owner = owner ?? m.Name, Body = Convert(m) });
			}
			catch (Exception)
			{
				// A body Harmony can't read just isn't looked at.
			}
		}
		foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
		{
			Match om = Regex.Match(nested.Name, "^<(\\w+)>");
			AddMethods(nested, om.Success ? om.Groups[1].Value : owner, into);
		}
	}

	private static List<Instr> Convert(MethodBase method)
	{
		List<CodeInstruction> code = PatchProcessor.GetOriginalInstructions(method);
		Dictionary<Label, int> labels = new Dictionary<Label, int>();
		for (int i = 0; i < code.Count; i++)
		{
			foreach (Label l in code[i].labels)
			{
				labels[l] = i;
			}
		}
		ParameterInfo[] parameters = method.GetParameters();
		List<Instr> list = new List<Instr>(code.Count);
		foreach (CodeInstruction ci in code)
		{
			OpCode op = ci.opcode;
			Instr x = new Instr { Op = op.Name };
			switch (ci.operand)
			{
			case string s:
				x.Str = s;
				break;
			case sbyte n:
				x.Num = n;
				break;
			case byte n:
				x.Num = n;
				break;
			case short n:
				x.Num = n;
				break;
			case int n:
				x.Num = n;
				break;
			case long n:
				x.Num = n;
				break;
			case float n:
				x.Num = n;
				break;
			case double n:
				x.Num = n;
				break;
			case Label l:
				x.Target = labels.TryGetValue(l, out int t) ? t : null;
				break;
			case Label[] ls:
				x.Target = (ls.Length > 0 && labels.TryGetValue(ls[0], out int t2)) ? t2 : null;
				break;
			case FieldInfo f:
				x.Str = f.Name;
				x.Type = Clean(f.DeclaringType?.Name ?? "");
				break;
			case MethodBase mb:
				x.Type = Clean(mb.DeclaringType?.Name ?? "");
				x.Method = mb.Name;
				x.ParamTypes = mb.GetParameters().Select(p => Clean(p.ParameterType.Name)).ToArray();
				if (mb is MethodInfo mi && mi.IsGenericMethod)
				{
					x.GenericArgs = mi.GetGenericArguments().Select(a => Clean(a.Name)).ToArray();
				}
				break;
			}
			// Short forms: ldc.i4.3, ldc.i4.m1; ldarg.1 (and ldarg.s with an index) name the parameter.
			Match lc = Regex.Match(op.Name, "^ldc\\.i4\\.(m1|\\d)$");
			if (lc.Success)
			{
				x.Num = (lc.Groups[1].Value == "m1") ? -1 : int.Parse(lc.Groups[1].Value);
			}
			int arg = -1;
			Match la = Regex.Match(op.Name, "^ldarg\\.(\\d)$");
			if (la.Success)
			{
				arg = int.Parse(la.Groups[1].Value);
			}
			else if ((op.Name == "ldarg.s" || op.Name == "ldarg") && x.Num.HasValue)
			{
				arg = (int)x.Num.Value;
				x.Num = null;
			}
			if (ci.operand is ParameterInfo pi)
			{
				x.Str = pi.Name;
			}
			else if (arg >= 0)
			{
				int index = arg - (method.IsStatic ? 0 : 1);
				if (index >= 0 && index < parameters.Length)
				{
					x.Str = parameters[index].Name;
				}
			}
			int pops = RuleEngine.StackCount(op.StackBehaviourPop.ToString());
			int pushes = RuleEngine.StackCount(op.StackBehaviourPush.ToString());
			if (ci.operand is MethodBase sig)
			{
				bool ctor = op == OpCodes.Newobj;
				int count = sig.GetParameters().Length;
				if (ctor)
				{
					pops = count;
					pushes = 1;
				}
				else
				{
					if (pops < 0)
					{
						pops = count + (sig.IsStatic ? 0 : 1);
					}
					if (pushes < 0)
					{
						pushes = (sig is MethodInfo r && r.ReturnType != typeof(void)) ? 1 : 0;
					}
				}
			}
			if (pops < 0)
			{
				pops = (op == OpCodes.Ret && method is MethodInfo rm && rm.ReturnType != typeof(void)) ? 1 : 0;
			}
			x.Pops = pops;
			x.Pushes = Math.Max(0, pushes);
			list.Add(x);
		}
		return list;
	}
}
