using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace Tinker.Card;

public static class Loc
{
	private static Dictionary<string, Dictionary<string, string>> English()
	{
		return new Dictionary<string, Dictionary<string, string>>
		{
			["relics"] = new Dictionary<string, string>
			{
				["TINKERERS_DIAL.title"] = "Tinkerer's Dial",
				["TINKERERS_DIAL.description"] = "Click to open the [gold]Workshop[/gold]: buy upgrades for your cards and relics with gold. Not during combat.",
				["TINKERERS_DIAL.flavor"] = "Nobody remembers who calibrated it. Nobody dares to do it again."
			},
			["card_selection"] = new Dictionary<string, string>
			{
				["TO_TINKER"] = "Choose a card to [gold]Tinker[/gold] with.",
				["UPGRADER_GENERATED_CARDS"] = "Choose {Amount} cards.",
				["UPGRADER_GENERATED_OPTIONAL"] = "Choose up to {Amount} cards."
			}
		};
	}

	internal static Dictionary<string, Dictionary<string, string>> TablesFor(string language)
	{
		Dictionary<string, Dictionary<string, string>> english = English();
		FinalCardEffects.AddTexts(english);
		return Lang.T(english, language);
	}

	public static void Install()
	{
		MergeInto(LocManager.Instance);
	}

	private static void MergeInto(LocManager manager)
	{
		if (manager != null)
		{
			Lang.Merge(manager, TablesFor(manager.Language), ModEntry.Warn);
		}
	}

	internal static void MergeIntoTables(Dictionary<string, LocTable> tables, string language)
	{
		Lang.Merge(tables, TablesFor(language));
	}
}
