// Runs Tinker's RuleEngine on the game's compiled code (via Cecil), for looking into what it decides.
// Usage (from the repository root):
//   dotnet run --project tools/rulecheck -c Release -- <sts2.dll> [out.tsv]
// Reads the values to check from tools/tests/data/rules.tsv (the tests' baseline), prints the ones whose result
// differs from it, and writes every result to out.tsv if given. Also:
//   RULE_DEBUG=Card1,Relic2   prints the IL the engine sees for those items
//   RULE_POWER_TIERS=P1,P2    prints the trigger tier of those powers
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tinker.Common.Rules;
using Tinker.Tools;

string dll = args[0];
string outPath = args.Length > 1 ? args[1] : null;
string baseline = Path.Combine("tools", "tests", "data", "rules.tsv");
var code = new CecilCode(dll);

string debugItem = Environment.GetEnvironmentVariable("RULE_DEBUG");
if (debugItem != null)
{
	foreach (var n in debugItem.Split(','))
	{
		var tt = code.Card(n) ?? code.Relic(n);
		foreach (var m in CecilCode.Item(tt).Methods)
		{
			if (!m.Body.Any(i => (i.Type == "DynamicVarSet") || (i.Type == "PowerCmd"))) continue;
			Console.WriteLine($"== {n} {m.Owner}/{m.Name}");
			for (int i = 0; i < m.Body.Count; i++) Console.WriteLine($"  {i,3} {m.Body[i]} {(m.Body[i].Target.HasValue ? "->" + m.Body[i].Target : "")} [{string.Join(",", m.Body[i].GenericArgs)}] p{m.Body[i].Pops}/{m.Body[i].Pushes} {m.Body[i].Str}");
		}
	}
	return;
}

var powerTiers = new Dictionary<string, string>();
string PowerTier(string p)
{
	if (!powerTiers.TryGetValue(p, out string tier))
	{
		var pt = code.Power(p);
		powerTiers[p] = tier = (pt == null) ? null : RuleEngine.PowerTriggerFamily(CecilCode.Item(pt));
	}
	return tier;
}
string powerList = Environment.GetEnvironmentVariable("RULE_POWER_TIERS");
if (powerList != null)
{
	foreach (string p in powerList.Split(','))
		Console.WriteLine($"POWER\t{p}\t{(code.Power(p) != null ? PowerTier(p) : "missing")}");
	return;
}

if (!File.Exists(baseline))
{
	Console.WriteLine("Run from the repository root (needs " + baseline + ").");
	return;
}
var rows = File.ReadAllLines(baseline).Skip(1).Where(l => l.Length > 0).Select(l => l.Split('\t')).ToList();
var dump = new List<string> { "kind\titem\tvalue\ttype\tsign\twhy\tcategory\ttier" };
int total = 0, changed = 0;
foreach (var group in rows.GroupBy(r => (r[0], r[1])))
{
	(string kind, string name) = group.Key;
	var type = (kind == "card") ? code.Card(name) : code.Relic(name);
	if (type == null) { Console.WriteLine($"gone: {kind} {name}"); continue; }
	var item = CecilCode.Item(type);
	foreach (var r in group)
	{
		var tm = Regex.Match(r[3], @"^(\w+?)(?:<(\w+)>)?$");
		item.Vars.Add(new VarInfo { Name = r[2], Class = tm.Groups[1].Value, Power = tm.Groups[2].Success ? tm.Groups[2].Value : null });
	}
	foreach (var r in group)
	{
		var v = item.Vars.First(x => x.Name == r[2]);
		Verdict e = RuleEngine.Decide(item, v, code.PowerType);
		string tier = (v.Power != null) ? PowerTier(v.Power) : (kind == "relic") ? RuleEngine.TriggerFamily(item, v.Name) : null;
		string now = $"{e.Sign}\t{e.Why}\t{RuleEngine.Category(item, v, e)}\t{tier}";
		string was = string.Join("\t", r.Skip(4));
		total++;
		if (now != was)
		{
			changed++;
			Console.WriteLine($"{kind} {name}.{v.Name}: was [{was.Replace('\t', '|')}] now [{now.Replace('\t', '|')}]");
		}
		dump.Add($"{kind}\t{name}\t{r[2]}\t{r[3]}\t{now}");
	}
}
Console.WriteLine($"{total} values, {changed} differ from the baseline");
if (outPath != null) File.WriteAllLines(outPath, dump);
