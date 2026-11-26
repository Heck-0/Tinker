using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tinker.Common;
using Xunit;

namespace Tinker.Tests;

/// <summary>
/// The mod's text is English in code, translated by table (Common/Text/Chinese.cs, Spanish.cs). These check the
/// tables against each other and against the code: every entry is reachable, every translated string in the code
/// has both translations, and nothing in the tables is left over.
/// </summary>
public class TextTests
{
	private static readonly (string Language, Dictionary<string, string> Exact, (string En, string Text)[] Templates)[] Tables =
	{
		("zhs", Chinese.Exact, Chinese.Templates),
		("spa", Spanish.Exact, Spanish.Templates),
	};

	/// <summary>Strings Spanish spells the same as English, so its table doesn't need them.</summary>
	private static readonly HashSet<string> SameInSpanish = new HashSet<string>
	{
		"Vulnerable", "Vigor",
	};

	private static Regex Pattern(string en) => new Regex("^" + Regex.Replace(Regex.Escape(en), "\\\\\\{(\\d+)}", "(.+?)") + "$", RegexOptions.Singleline);

	private static bool Covered(string english, Dictionary<string, string> exact, (string En, string Text)[] templates)
	{
		return exact.ContainsKey(english) || templates.Any(t => Pattern(t.En).IsMatch(english));
	}

	[Fact]
	public void EveryExactEntryIsWhatTranslateReturns()
	{
		foreach (var (language, exact, _) in Tables)
		{
			foreach (KeyValuePair<string, string> e in exact)
			{
				Assert.Equal(e.Value, Lang.Translate(e.Key, language));
			}
		}
		foreach (KeyValuePair<string, string> e in Spanish.Exact)
		{
			Assert.Equal(e.Value, Lang.Translate(e.Key, "esp"));
		}
	}

	[Fact]
	public void EveryTemplateIsReachable()
	{
		// A template listed after a more general one that also matches would never be used.
		foreach (var (language, _, templates) in Tables)
		{
			foreach (var (en, text) in templates)
			{
				string probe = Regex.Replace(en, @"\{(\d+)\}", m => "7" + m.Groups[1].Value);
				string want = Regex.Replace(text, @"\{(\d+)\}", m => "7" + m.Groups[1].Value);
				Assert.Equal(want, Lang.Translate(probe, language));
			}
		}
	}

	[Fact]
	public void OtherLanguagesShowTheEnglish()
	{
		foreach (string key in Chinese.Exact.Keys.Concat(Spanish.Exact.Keys))
		{
			Assert.Equal(key, Lang.Translate(key, "eng"));
			Assert.Equal(key, Lang.Translate(key, "fra"));
		}
	}

	/// <summary>Every English string the code passes to Lang.T (or the T wrappers) has a Chinese and a Spanish entry.</summary>
	[Fact]
	public void EveryTranslatedStringInTheCodeHasBothTranslations()
	{
		Regex call = new Regex(@"(?<![\w.])(?:Lang\.|Text\.)?T\(\s*(\$?)""((?:[^""\\]|\\.)*)""\s*\)");
		List<string> english = new List<string>();
		foreach (var (path, text) in Repo.SourceFiles())
		{
			foreach (Match m in call.Matches(text))
			{
				string body = Regex.Unescape(m.Groups[2].Value);
				if (m.Groups[1].Value == "$")
				{
					body = Regex.Replace(body, @"\{[^{}]*\}", "7");
				}
				english.Add(body);
			}
		}
		// The effect tables' labels, translated where they're shown.
		english.AddRange(ExpandedEffects.Entries.Select(e => e.English));
		english.AddRange(ExpandedEffects.Entries.Select(e => e.ExtraEnglish).Where(s => !string.IsNullOrEmpty(s)));
		english.AddRange(BidirectionalEffects.Entries.Select(e => e.English));
		english.AddRange(AdvancedEnchantmentEffects.Entries.Select(e => e.English));
		Assert.True(english.Count > 200, "found only " + english.Count + " strings; is the pattern still right?");

		List<string> missing = new List<string>();
		foreach (string s in english.Distinct().Where(s => s.Trim().Length > 0 && Regex.IsMatch(s, "[A-Za-z]")))
		{
			if (!Covered(s, Chinese.Exact, Chinese.Templates))
			{
				missing.Add("zhs: " + s);
			}
			if (!SameInSpanish.Contains(s) && !Covered(s, Spanish.Exact, Spanish.Templates))
			{
				missing.Add("spa: " + s);
			}
		}
		Assert.True(missing.Count == 0, missing.Count + " untranslated:\n" + string.Join("\n", missing));
	}

	/// <summary>Table entries whose English no longer appears anywhere in the code.</summary>
	[Fact]
	public void NoLeftoverTranslations()
	{
		string code = string.Join("\n", Repo.SourceFiles().Where(f => !f.Path.EndsWith("/Chinese.cs") && !f.Path.EndsWith("/Spanish.cs"))
			.Select(f => Regex.Unescape(string.Join("\n", Regex.Matches(f.Text, @"""((?:[^""\\]|\\.)*)""").Select(m => m.Groups[1].Value)))));
		List<string> unused = new List<string>();
		foreach (var (language, exact, templates) in Tables)
		{
			foreach (string en in exact.Keys.Concat(templates.Select(t => t.En)))
			{
				string[] pieces = Regex.Split(en, @"\{\d+\}").Where(p => p.Trim().Length > 0).ToArray();
				if (!pieces.All(p => code.Contains(p)))
				{
					unused.Add(language + ": " + en);
				}
			}
		}
		Assert.True(unused.Count == 0, unused.Count + " unused:\n" + string.Join("\n", unused));
	}
}
