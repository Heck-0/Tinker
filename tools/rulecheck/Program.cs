// Runs Tinker's RuleEngine on the game's compiled code (via Cecil) and compares with tools/divergence.py's rule column.
// Usage: rulecheck <sts2.dll> <decompiled game src> <coverage dir> [out.tsv]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Tinker.Common.Rules;

string dll = args[0], src = args[1], cov = args[2];
string outPath = args.Length > 3 ? args[3] : null;
var module = ModuleDefinition.ReadModule(dll);
var types = module.GetTypes().ToDictionary(t => t.FullName);

static string Clean(string n) { int i = n.IndexOf('`'); return i < 0 ? n : n.Substring(0, i); }

static List<Instr> Convert(MethodDefinition m)
{
	var body = m.Body.Instructions;
	var index = new Dictionary<Instruction, int>();
	for (int i = 0; i < body.Count; i++) index[body[i]] = i;
	var list = new List<Instr>();
	foreach (var ins in body)
	{
		var x = new Instr { Op = ins.OpCode.Name };
		switch (ins.Operand)
		{
			case string s: x.Str = s; break;
			case sbyte sb: x.Num = sb; break;
			case int iv: x.Num = iv; break;
			case long lv: x.Num = lv; break;
			case float fv: x.Num = fv; break;
			case double dv: x.Num = dv; break;
			case Instruction t: x.Target = index[t]; break;
			case Instruction[] ts: x.Target = ts.Length > 0 ? index[ts[0]] : null; break;
			case FieldReference f: x.Str = f.Name; x.Type = Clean(f.DeclaringType.Name); break;
			case ParameterDefinition p: x.Str = p.Name; break;
			case MethodReference mr:
				x.Type = Clean(mr.DeclaringType.Name);
				x.Method = mr.Name;
				x.ParamTypes = mr.Parameters.Select(p => Clean(p.ParameterType.Name)).ToArray();
				if (mr is GenericInstanceMethod g) x.GenericArgs = g.GenericArguments.Select(a => Clean(a.Name)).ToArray();
				break;
		}
		// ldc.i4.N / ldarg.N short forms
		var mm = Regex.Match(ins.OpCode.Name, @"^ldc\.i4\.(m1|\d)$");
		if (mm.Success) x.Num = mm.Groups[1].Value == "m1" ? -1 : int.Parse(mm.Groups[1].Value);
		var am = Regex.Match(ins.OpCode.Name, @"^ldarg\.(\d)$");
		if (am.Success)
		{
			int a = int.Parse(am.Groups[1].Value) - (m.HasThis ? 1 : 0);
			if (a >= 0 && a < m.Parameters.Count) x.Str = m.Parameters[a].Name;
		}
		int pops = RuleEngine.StackCount(ins.OpCode.StackBehaviourPop.ToString());
		int pushes = RuleEngine.StackCount(ins.OpCode.StackBehaviourPush.ToString());
		if (ins.Operand is MethodReference sig)
		{
			bool ctor = ins.OpCode.Code == Code.Newobj;
			if (pops < 0) pops = sig.Parameters.Count + (sig.HasThis && !ctor ? 1 : 0);
			if (pushes < 0) pushes = ctor ? 1 : (sig.ReturnType.FullName == "System.Void" ? 0 : 1);
			if (ctor) { pops = sig.Parameters.Count; pushes = 1; }
		}
		if (pops < 0) pops = ins.OpCode.Code == Code.Ret ? (m.ReturnType.FullName == "System.Void" ? 0 : 1) : 0;
		if (pushes < 0) pushes = 0;
		x.Pops = pops; x.Pushes = pushes;
		list.Add(x);
	}
	return list;
}

static void AddMethods(TypeDefinition t, string owner, List<MethodCode> into)
{
	foreach (var m in t.Methods.Where(m => m.HasBody))
		into.Add(new MethodCode { Name = m.Name, Owner = owner ?? m.Name, Body = Convert(m) });
	foreach (var n in t.NestedTypes)
	{
		var om = Regex.Match(n.Name, @"^<(\w+)>");
		AddMethods(n, om.Success ? om.Groups[1].Value : owner, into);
	}
}

string debugItem = Environment.GetEnvironmentVariable("RULE_DEBUG");
if (debugItem != null)
{
	foreach (var n in debugItem.Split(','))
	{
		var tt = types.Values.First(x => x.Name == n && (x.Namespace.EndsWith("Cards") || x.Namespace.EndsWith("Relics")));
		var ms = new List<MethodCode>(); AddMethods(tt, null, ms);
		foreach (var m in ms)
		{
			if (!m.Body.Any(i => (i.Type == "DynamicVarSet") || (i.Type == "PowerCmd"))) continue;
			Console.WriteLine($"== {n} {m.Owner}/{m.Name}");
			for (int i = 0; i < m.Body.Count; i++) Console.WriteLine($"  {i,3} {m.Body[i]} {(m.Body[i].Target.HasValue ? "->" + m.Body[i].Target : "")} [{string.Join(",", m.Body[i].GenericArgs)}] p{m.Body[i].Pops}/{m.Body[i].Pushes} {m.Body[i].Str}");
		}
	}
	return;
}
var powerTypes = new Dictionary<string, int>();
int PowerType(string p)
{
	if (!powerTypes.TryGetValue(p, out int t))
	{
		string path = Path.Combine(src, "MegaCrit.Sts2.Core.Models.Powers", p + ".cs");
		t = 0;
		if (File.Exists(path))
		{
			var mt = Regex.Match(File.ReadAllText(path), @"PowerType\.(Buff|Debuff)");
			if (mt.Success) t = mt.Groups[1].Value == "Buff" ? 1 : -1;
		}
		powerTypes[p] = t;
	}
	return t;
}

int total = 0, same = 0;
var diffs = new List<string>();
var powerTiers = new Dictionary<string, string>();
string PowerTier(TypeDefinition pt)
{
	var pi = new ItemCode { Name = pt.Name };
	AddMethods(pt, null, pi.Methods);
	return RuleEngine.PowerTriggerFamily(pi);
}
string powerList = Environment.GetEnvironmentVariable("RULE_POWER_TIERS");
if (powerList != null)
{
	foreach (string p in powerList.Split(','))
		Console.WriteLine($"POWER\t{p}\t{(types.TryGetValue("MegaCrit.Sts2.Core.Models.Powers." + p, out var pt) ? PowerTier(pt) : "missing")}");
	return;
}
var dump = new List<string> { "kind\titem\tvalue\tengine\tengine_why\tpython\tpython_why\tmod\tcategory\ttier" };
foreach (var (kind, ns, csv) in new[] { ("card", "MegaCrit.Sts2.Core.Models.Cards", "divergence_cards.csv"), ("relic", "MegaCrit.Sts2.Core.Models.Relics", "divergence_relics.csv") })
{
	var rows = File.ReadAllLines(Path.Combine(cov, csv)).Skip(1).Select(l => l.Split(',')).ToList();
	foreach (var group in rows.GroupBy(r => r[0]))
	{
		string name = group.Key;
		if (!types.TryGetValue(ns + "." + name, out var type)) { Console.WriteLine("missing type " + name); continue; }
		var item = new ItemCode { Name = name };
		AddMethods(type, null, item.Methods);
		foreach (var r in group)
		{
			var tm = Regex.Match(r[2], @"^(\w+?)(?:<(\w+)>)?$");
			item.Vars.Add(new VarInfo { Name = r[1], Class = tm.Groups[1].Value, Power = tm.Groups[2].Success ? tm.Groups[2].Value : null });
		}
		foreach (var r in group)
		{
			var v = item.Vars.First(x => x.Name == r[1]);
			Verdict e = RuleEngine.Decide(item, v, PowerType);
			int py = int.Parse(r[3]);
			total++;
			if (e.Sign == py) same++;
			else diffs.Add($"{kind}\t{name}.{r[1]}\tengine {e.Sign:+0;-0;0} ({e.Why})\tpython {py:+0;-0;0} ({r[4]})\tmod {r[5]}");
			string tier = null;
			if (v.Power != null)
			{
				if (!powerTiers.TryGetValue(v.Power, out tier))
				{
					tier = types.TryGetValue("MegaCrit.Sts2.Core.Models.Powers." + v.Power, out var pt) ? PowerTier(pt) : null;
					powerTiers[v.Power] = tier;
				}
			}
			else if (kind == "relic")
			{
				tier = RuleEngine.TriggerFamily(item, v.Name);
			}
			dump.Add($"{kind}\t{name}\t{r[1]}\t{e.Sign}\t{e.Why}\t{py}\t{r[4]}\t{r[5]}\t{RuleEngine.Category(item, v, e)}\t{tier}");
		}
	}
}
Console.WriteLine($"{same}/{total} same as python; {diffs.Count} differ");
foreach (var g in diffs.GroupBy(d => Regex.Match(d, @"engine [^\t]*\)\tpython [^\t]*\)").Value.Replace("engine ", "").Split('\t')[0] + " vs " + Regex.Match(d, @"python [+\-0]+ \(([^)]*)").Groups[1].Value).OrderByDescending(g => g.Count()))
	Console.WriteLine($"  {g.Count(),3}  {g.Key}");
foreach (var d in diffs.Take(400)) Console.WriteLine(d);
if (outPath != null) File.WriteAllLines(outPath, dump);
