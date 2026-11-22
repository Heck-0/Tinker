using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;

namespace Tinker.Common;

/// <summary>
/// The mod's text system:
/// - which language is active (Spanish / Simplified Chinese / English),
/// - <see cref="T"/>: Spanish or English text, with Simplified Chinese looked up in <see cref="Chinese"/>;
///   <see cref="Zh"/> does the Chinese step for English text picked elsewhere (data tables, labels),
/// - merging each half's translation tables into the game, and running them when the game loads or
///   changes language (see <see cref="LanguageHooks"/>). Each half registers its tables at startup.
/// </summary>
public static class Lang
{
    [ThreadStatic]
    private static string _installing;

    private static readonly List<(Regex Pattern, string Zh)> ChineseTemplates = Chinese.Templates
        .Select(t => (new Regex("^" + Regex.Replace(Regex.Escape(t.En), "\\\\\\{(\\d+)}", "(.+?)") + "$", RegexOptions.Singleline), t.Zh))
        .ToList();

    private static readonly List<Action<LocManager>> LanguageInstallers = new List<Action<LocManager>>();

    private static readonly List<Action<Dictionary<string, LocTable>, string>> TableInstallers = new List<Action<Dictionary<string, LocTable>, string>>();

    /// <summary>Game language code ("eng", "spa", "zhs", ...). While tables install, the language being installed.</summary>
    public static string Current => _installing ?? LocManager.Instance?.Language ?? "eng";

    public static bool IsSpanish(string language) => language == "spa" || language == "esp";

    public static bool IsSimplifiedChinese(string language) => language == "zhs";

    public static bool Spanish => IsSpanish(Current);

    public static bool SimplifiedChinese => IsSimplifiedChinese(Current);

    /// <summary>Spanish text in Spanish; otherwise English, translated when the game is in Simplified Chinese.</summary>
    public static string T(string spanish, string english, bool? isSpanish = null)
    {
        if (isSpanish ?? Spanish)
        {
            return spanish;
        }
        return SimplifiedChinese ? Translate(english) : english;
    }

    /// <summary>English text, translated when the game is in Simplified Chinese. For text that isn't Spanish.</summary>
    public static string Zh(string english)
    {
        return (english != null && SimplifiedChinese) ? Translate(english) : english;
    }

    /// <summary>A translation table's values (English) translated, when <paramref name="language"/> is Simplified Chinese.</summary>
    public static Dictionary<string, Dictionary<string, string>> Zh(Dictionary<string, Dictionary<string, string>> tables, string language)
    {
        if (IsSimplifiedChinese(language))
        {
            foreach (Dictionary<string, string> table in tables.Values)
            {
                Zh(table, language);
            }
        }
        return tables;
    }

    public static Dictionary<string, string> Zh(Dictionary<string, string> table, string language)
    {
        if (IsSimplifiedChinese(language))
        {
            foreach (string key in table.Keys.ToList())
            {
                table[key] = Translate(table[key]);
            }
        }
        return table;
    }

    /// <summary>
    /// Simplified Chinese for an English string: an exact entry, then a "{0}" template whose pieces are
    /// translated in turn (so "{0} Total: +{1}." translates both the sentence and the number); else the English.
    /// </summary>
    public static string Translate(string english)
    {
        if (string.IsNullOrEmpty(english))
        {
            return english;
        }
        if (Chinese.Exact.TryGetValue(english, out string exact))
        {
            return exact;
        }
        foreach ((Regex pattern, string zh) in ChineseTemplates)
        {
            Match match = pattern.Match(english);
            if (match.Success)
            {
                return Regex.Replace(zh, "\\{(\\d+)\\}", m =>
                {
                    string part = match.Groups[int.Parse(m.Groups[1].Value) + 1].Value;
                    return part.Length < english.Length ? Translate(part) : part;
                });
            }
        }
        return english;
    }

    /// <summary>Makes <see cref="Current"/> report <paramref name="language"/> until disposed.</summary>
    public static IDisposable Installing(string language)
    {
        string previous = _installing;
        _installing = language;
        return new Restore(() => _installing = previous);
    }

    /// <summary>Merges translation tables (table name → key → text) into the game's tables.</summary>
    public static void Merge(LocManager manager, IEnumerable<KeyValuePair<string, Dictionary<string, string>>> tables, Action<string> warn = null)
    {
        foreach (KeyValuePair<string, Dictionary<string, string>> table in tables)
        {
            try
            {
                manager.GetTable(table.Key).MergeWith(table.Value);
            }
            catch (Exception ex)
            {
                warn?.Invoke("Could not merge table " + table.Key + ": " + ex.Message);
            }
        }
    }

    /// <summary>Merges translation tables into a set of tables the game is loading (only tables that exist).</summary>
    public static void Merge(Dictionary<string, LocTable> loading, IEnumerable<KeyValuePair<string, Dictionary<string, string>>> tables)
    {
        if (loading == null)
        {
            return;
        }
        foreach (KeyValuePair<string, Dictionary<string, string>> table in tables)
        {
            if (loading.TryGetValue(table.Key, out LocTable target))
            {
                target.MergeWith(table.Value);
            }
        }
    }

    /// <summary>Runs after the game sets its language, in registration order.</summary>
    public static void OnLanguageSet(Action<LocManager> installer) => LanguageInstallers.Add(installer);

    /// <summary>Runs after the game loads a language's tables from disk (tables, language).</summary>
    public static void OnTablesLoaded(Action<Dictionary<string, LocTable>, string> installer) => TableInstallers.Add(installer);

    internal static void LanguageSet(LocManager manager)
    {
        foreach (Action<LocManager> installer in LanguageInstallers)
        {
            Run(() => installer(manager));
        }
    }

    internal static void TablesLoaded(Dictionary<string, LocTable> tables, string language)
    {
        foreach (Action<Dictionary<string, LocTable>, string> installer in TableInstallers)
        {
            Run(() => installer(tables, language));
        }
    }

    private static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Godot.GD.PushWarning("[Tinker] Text install failed: " + ex.Message);
        }
    }

    private sealed class Restore : IDisposable
    {
        private readonly Action _undo;

        public Restore(Action undo) => _undo = undo;

        public void Dispose() => _undo();
    }
}
