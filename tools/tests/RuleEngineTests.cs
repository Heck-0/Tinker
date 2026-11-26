using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tinker.Common.Rules;
using Tinker.Tools;
using Xunit;

namespace Tinker.Tests;

/// <summary>
/// The rule engine on compiled code, against baselines in data/: for every card and relic value listed, the
/// direction (+1 upgrade up, -1 down, 0 not upgradable), the rule that decided it, the cost category and the trigger
/// tier. rules.tsv is the game's own cards and relics; rules_hextech.tsv the Hextech Runes mod's (checked when the mod
/// is installed). When a game update or an engine change moves a value, this lists it; if the change is intended,
/// accept it with TINKER_UPDATE_BASELINE=1 (see README: Tests).
/// </summary>
public class RuleEngineTests
{
	private static readonly Regex TypeName = new Regex(@"^(\w+?)(?:<(\w+)>)?$");

	/// <summary>The Hextech Runes mod's DLL for the installed game version, or null.</summary>
	private static string HextechDll
	{
		get
		{
			string path = Environment.GetEnvironmentVariable("HEXTECH_DLL")
				?? @"C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747501308\lib\0.113.0\HextechRunes.dll";
			return File.Exists(path) ? path : null;
		}
	}

	[SkippableFact]
	public void MatchesBaseline()
	{
		Skip.If(Repo.GameDll == null, "Slay the Spire 2 isn't installed (set STS2_DIR)");
		Check("rules.tsv", new CecilCode(Repo.GameDll), mods: false, 1000);
	}

	[SkippableFact]
	public void MatchesHextechBaseline()
	{
		Skip.If(Repo.GameDll == null, "Slay the Spire 2 isn't installed (set STS2_DIR)");
		Skip.If(HextechDll == null, "the Hextech Runes mod isn't installed (set HEXTECH_DLL)");
		Check("rules_hextech.tsv", new CecilCode(Repo.GameDll, HextechDll), mods: true, 300);
	}

	/// <summary>
	/// Every card and relic the DLLs define (the game's, or the mods' when <paramref name="mods"/>), with the values
	/// its CanonicalVars create, against the baseline: a value that appears, disappears, changes type or changes
	/// result is listed.
	/// </summary>
	private static void Check(string file, CecilCode code, bool mods, int minimumRows)
	{
		List<string[]> rows = File.ReadAllLines(Repo.Data(file)).Skip(1).Where(l => l.Length > 0).Select(l => l.Split('\t')).ToList();
		Assert.True(rows.Count > minimumRows, file + " looks truncated");
		Dictionary<(string, string, string), string[]> baseline = rows.ToDictionary(r => (r[0], r[1], r[2]), r => r);

		Dictionary<string, string> powerTiers = new Dictionary<string, string>();
		string PowerTier(string p)
		{
			if (!powerTiers.TryGetValue(p, out string tier))
			{
				var pt = code.Power(p);
				powerTiers[p] = tier = (pt == null) ? null : RuleEngine.PowerTriggerFamily(code.ItemFor(pt));
			}
			return tier;
		}

		StringBuilder changes = new StringBuilder();
		int changed = 0;
		List<string> current = new List<string> { "kind\titem\tvalue\ttype\tsign\twhy\tcategory\ttier" };
		HashSet<(string, string, string)> seen = new HashSet<(string, string, string)>();
		foreach ((string kind, var type) in code.Items(mods))
		{
			List<VarInfo> vars = code.Vars(type);
			if (vars.Count == 0)
			{
				continue;
			}
			ItemCode item = code.ItemFor(type);
			item.Vars.AddRange(vars);
			foreach (VarInfo v in vars)
			{
				Verdict e = RuleEngine.Decide(item, v, code.PowerType);
				string category = RuleEngine.Category(item, v, e) ?? "";
				string tier = ((v.Power != null) ? PowerTier(v.Power) : (kind == "relic") ? RuleEngine.TriggerFamily(item, v.Name) : null) ?? "";
				string typeText = v.Class + (v.Power != null ? "<" + v.Power + ">" : "");
				string now = $"{e.Sign}\t{e.Why}\t{category}\t{tier}";
				current.Add($"{kind}\t{type.Name}\t{v.Name}\t{typeText}\t{now}");
				var key = (kind, type.Name, v.Name);
				seen.Add(key);
				if (!baseline.TryGetValue(key, out string[] r))
				{
					changed++;
					changes.AppendLine($"{kind} {type.Name}.{v.Name}: new value ({typeText}) [{now.Replace('\t', '|')}]");
					continue;
				}
				string was = string.Join("\t", Enumerable.Range(4, 4).Select(i => (i < r.Length) ? r[i] : ""));
				if (now != was || r[3] != typeText)
				{
					changed++;
					changes.AppendLine($"{kind} {type.Name}.{v.Name}: was [{r[3]}|{was.Replace('\t', '|')}] now [{typeText}|{now.Replace('\t', '|')}]");
				}
			}
		}
		foreach (var key in baseline.Keys.Where(k => !seen.Contains(k)))
		{
			changed++;
			changes.AppendLine($"{key.Item1} {key.Item2}.{key.Item3}: gone");
		}
		// TINKER_UPDATE_BASELINE=1 dotnet test tools/tests : accept the current results as the new baseline.
		if (Environment.GetEnvironmentVariable("TINKER_UPDATE_BASELINE") == "1")
		{
			File.WriteAllText(Path.Combine(Repo.Root, "tools", "tests", "data", file), string.Join("\n", current) + "\n");
			return;
		}
		Assert.True(changed == 0, $"{file}: {changed} values changed (if intended, rerun with TINKER_UPDATE_BASELINE=1):\n{changes}");
	}

	[Fact]
	public void StackCountsMatchCecilNames()
	{
		Assert.Equal(0, RuleEngine.StackCount("Pop0"));
		Assert.Equal(1, RuleEngine.StackCount("Pop1"));
		Assert.Equal(2, RuleEngine.StackCount("Pop1_pop1"));
		Assert.Equal(1, RuleEngine.StackCount("Push1"));
		Assert.Equal(-1, RuleEngine.StackCount("Varpop"));
	}
}
