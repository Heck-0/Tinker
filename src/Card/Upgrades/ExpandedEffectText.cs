using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Tinker.Card;

/// <summary>
/// Rewrites a card's English description for an expanded effect's new value ("Draw 2 cards" -> "Draw 4 cards").
/// Each effect has its own patterns; text the patterns don't find (another language, a changed card) is left as is.
/// </summary>
internal static class ExpandedEffectText
{
	private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(50);

	private static readonly string[] Multiples = { "", "", "double", "triple", "quadruple", "quintuple" };

	private static string Plural(int n, string one, string many) => (n == 1) ? one : many;

	private static string Times(int n) => (n == 1) ? "once" : n + " times";

	private static string Factor(int n) => (n >= 2 && n <= 5) ? Multiples[n] : n + " times";

	private static string Verb(int n, bool capital)
	{
		string verb = (n >= 2 && n <= 5) ? Multiples[n] : "multiply by " + n;
		return capital ? char.ToUpperInvariant(verb[0]) + verb.Substring(1) : verb;
	}

	/// <summary>The line rewritten for <paramref name="n"/>, or null if none of the effect's patterns matched.</summary>
	internal static string RewriteLine(string text, string card, string key, string kind, int n)
	{
		bool matched = false;
		string cards = n + " " + Plural(n, "card", "cards");
		string optional = "up to " + cards;
		Apply();
		if (!matched)
		{
			return null;
		}
		return Regex.Replace(text, "\\n{2,}", "\n").Trim();

		void Apply()
		{
			switch (key)
			{
			case "extra:statusOrbs":
				Set("(?<=\\[/gold\\] )1 random Orb", (n + " random " + Plural(n, "Orb", "Orbs")));
				return;
			case "extra:shadowTurns":
				Set("Next turn", ("For the next " + n + " turns"));
				return;
			case "extra:shadowMultiplier":
			case "extra:conditionalMultiplier":
				DoubleWord();
				return;
			}
			if (kind == "hangGrowth" || kind == "energyMultiplier" || kind == "vulnerableMultiplier" || card == "Entrench")
			{
				DoubleWord();
				return;
			}
			if (card == "Shadowmeld")
			{
				string factor = ((n < 31) ? (1L << n).ToString(CultureInfo.InvariantCulture) : ("2^" + n));
				Sub("Double ([^\\n.]+)", (Match m) => "Multiply " + m.Groups[1].Value + " by " + factor);
				return;
			}
			switch (key)
			{
			case "extradown:targetCost":
				Sub("( to )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= this)", (Match m) => m.Groups[1].Value + n);
				return;
			case "extra:sharedForge":
				Set("all allies \\[gold\\]Forge\\[/gold\\] as well", ("all allies [gold]Forge[/gold] " + n + "% of that amount"));
				return;
			case "extra:explosionDamage":
				Sub("(deal )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= damage to ALL)", (Match m) => m.Groups[1].Value + n);
				return;
			case "extradown:poisonThreshold":
				Set("Every (?:time|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* times) you apply", ("Every " + ((n == 1) ? "time" : (n + " times")) + " you apply"));
				return;
			case "extra:scareWeak":
				Sub("(Apply )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= \\[gold\\]Weak)", (Match m) => m.Groups[1].Value + n);
				return;
			}
			if (card == "Reflect")
			{
				Set("(?:Blocked attack damage|\\d+% of blocked attack damage) is reflected to your attacker this turn", (n + "% of blocked attack damage is reflected to your attacker this turn"));
				return;
			}
			switch (key)
			{
			case "extra:gambleDraw":
				Set("draw that many cards", ("draw that many cards plus " + cards));
				return;
			case "extra:allowedDraw":
				Set("You cannot draw additional cards this turn", ("You may only draw up to " + n + " additional " + Plural(n, "card", "cards") + " this turn"));
				return;
			case "extra:drawTolerance":
				Set("Can only be played if there are no cards in your", ("Can only be played with " + optional + " in your"));
				return;
			case "extra:nonAttackTolerance":
				Set("Can only be played if every card in your \\[gold\\]Hand\\[/gold\\] is an Attack", ("Can only be played with up to " + n + " non-Attack " + Plural(n, "card", "cards") + " in your [gold]Hand[/gold]"));
				return;
			case "extra:killReplays":
				Set("Repeat this effect", ("Repeat this effect " + Times(n)));
				return;
			}
			if (kind == "reduceDiscard")
			{
				Sub("\\bDiscard (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards?\\.?", (Match _) => (n != 0) ? ("Discard " + cards + ".") : "");
				return;
			}
			if (kind == "reduceHand" || card == "PhotonCut")
			{
				Sub("Put (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards? from your \\[gold\\]Hand\\[/gold\\] on top of your \\[gold\\]Draw Pile\\[/gold\\]\\.", (Match _) => (n != 0) ? (("Put " + cards + " from your [gold]Hand[/gold] on top of your [gold]Draw Pile[/gold].")) : "");
				return;
			}
			if (kind == "exhaustHand" || kind == "mixedExhaust" || kind == "exhaustPile" || kind == "randomExhaust")
			{
				Sub("\\[gold\\]Exhaust\\[/gold\\] (?:a|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*) cards?(?: at random)?(?: from your \\[gold\\]Draw Pile\\[/gold\\])?\\.", delegate(Match m)
				{
					if (n == 0)
					{
						return "";
					}
					bool random = kind == "randomExhaust" || m.Value.Contains("at random");
					return "[gold]Exhaust[/gold] " + (random ? cards : optional) + (random ? " at random" : "") + ((kind == "exhaustPile") ? " from your [gold]Draw Pile[/gold]" : "") + ".";
				});
				return;
			}
			if (kind == "putBack" || kind == "retrieve")
			{
				Sub("Put (a card|an Attack|a Skill)", delegate(Match m)
				{
					string what = (m.Groups[1].Value.Contains("Attack") ? Plural(n, "Attack", "Attacks") : (m.Groups[1].Value.Contains("Skill") ? Plural(n, "Skill", "Skills") : Plural(n, "card", "cards")));
					return "Put up to " + n + " " + what;
				});
				if (card == "SeekerStrike")
				{
					Set("Choose 1 of", ("Choose up to " + n + " of"));
				}
				if (card == "Tutor")
				{
					Set("chooses a card", ("chooses " + optional));
					Set("to add", "to add");
				}
				return;
			}
			if (key == "extra:returnCount")
			{
				if (card == "Rebound")
				{
					Set("the next card you play", ("the next " + cards + " you play"));
				}
				else
				{
					Set("The first Attack or Skill you play", ("The first " + n + " Attacks or Skills you play"));
					Set("is placed", "are placed");
				}
				if (n == 0 && matched)
				{
					Set("(?:Put the next|The first) 0 [^\\n.]+\\.", "No cards are returned to the [gold]Draw Pile[/gold].");
				}
				return;
			}
			if (kind == "hits" || kind == "conditionalHits")
			{
				Set("\\btwice\\b", Times(n));
				return;
			}
			if (kind == "x")
			{
				Sub("\\bX(?:\\+(\\d+))?\\b", (Match m) => "X+" + checked(n + (m.Groups[1].Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0)));
				return;
			}
			if (kind == "draw" || key == "extra:draw" || card == "DarkEmbrace" || card == "Predator")
			{
				Draw();
				return;
			}
			if (kind == "discount" || kind == "surcharge")
			{
				Sub("(Costs )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
				Sub("((?:reduce|Reduce|Increase) (?:this card's cost|its cost) by )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
				if (kind == "surcharge" && n == 0)
				{
					Set("Increase this card's cost by 0\\.", "");
				}
				return;
			}
			if (kind == "chooseGenerated" || kind == "options")
			{
				Sub("(Choose )(\\d+)( of )(\\d+)", (Match m) => m.Groups[1].Value + ((kind == "chooseGenerated") ? n.ToString() : m.Groups[2].Value) + m.Groups[3].Value + ((kind == "options") ? n.ToString() : m.Groups[4].Value));
				if (kind == "chooseGenerated" && n > 1)
				{
					Set("It's free", "They're free");
				}
				return;
			}
			if (kind == "darkPassive" || kind == "teslaPassive")
			{
				Sub("(Trigger [^\\n.]+?)(?: twice)?\\.", (Match m) => m.Groups[1].Value + " " + Times(n) + ".");
				return;
			}
			if (key == "extra:channels" && card == "LightningRod")
			{
				Sub("(\\[gold\\]Channel\\[/gold\\] )1", (Match m) => m.Groups[1].Value + n);
				return;
			}
			if (key == "extra:replays" || card == "TagTeam")
			{
				Set("an (?:extra|additional) time", (n + " additional " + Plural(n, "time", "times")));
				if (kind == "autoReplays" || kind == "autoDrawReplays")
				{
					Set("play it(?=\\.)", ("play it " + Times(n)));
				}
				if (card == "Hellraiser")
				{
					Set("it is played against", ("it is played " + Times(n) + " against"));
				}
				if (card == "KnifeTrap")
				{
					Set("on the enemy\\.", ("on the enemy " + Times(n) + "."));
				}
				if (card == "Eidolon")
				{
					Sub("(Play ALL[^\\n.]+)\\.", (Match m) => m.Groups[1].Value + " " + Times(n) + ".");
					Sub("([Gg]ain )1(?= .*Intangible)", (Match m) => m.Groups[1].Value + n);
				}
				if (kind == "grantReplays")
				{
					Sub("\\[gold\\]Replay\\[/gold\\]", (Match m) => n + " " + m.Value);
				}
				return;
			}
			// Multipliers written as "equal to damage" / "that much".
			if (kind == "blockMultiplier" || kind == "groupResource" || card == "BlightStrike" || card == "Prolong")
			{
				Set("equal to damage", ("equal to " + n + " times damage"));
				Set("equal to your", ("equal to " + n + " times your"));
				return;
			}
			if (card == "Underworld" || card == "ReaperForm" || card == "NecroMastery")
			{
				Set("that much(?= \\[gold\\]Doom)", (n + " times that much"));
				Set("that much HP", (n + " times that much HP"));
				return;
			}
			switch (key)
			{
			case "extra:protectionPercent":
				Sub("((?:You receive|Allies take) )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*% less", (Match m) => m.Groups[1].Value + n + "% less");
				Set("half that much", (n + "% of that"));
				Set("Allies take half damage", ("Allies take " + n + "% less damage"));
				return;
			case "extra:normalityLimit":
				Sub("(more than )3", (Match m) => m.Groups[1].Value + n);
				return;
			case "extradown:threshold":
				Sub("(Every |Every time you play )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= cards)", (Match m) => m.Groups[1].Value + n);
				if (card == "Juggling")
				{
					Set("third(?= \\s*(?:\\[gold\\])?Attack)", ((n == 1) ? "first" : ((n == 2) ? "second" : "third")));
				}
				if (card == "Orbit")
				{
					Set("Every [^\\n]+ you spend", ("Every " + n + " Energy you spend"));
				}
				return;
			}
			if (kind == "keywordHand" || kind == "selectHand" || kind == "upgradeHand" || kind == "transformHand")
			{
				Set("a card(?= in your \\[gold\\]Hand)", optional);
				Set("a Skill(?= in your \\[gold\\]Hand)", "up to " + n + " " + Plural(n, "Skill", "Skills"));
				if (kind == "transformHand")
				{
					Set("Choose a card", ("Choose " + optional));
				}
				if (card == "Transfigure" && matched)
				{
					Set("It costs", "They cost");
				}
				return;
			}
			switch (card)
			{
			case "Conqueror":
			case "Convergence":
			case "Salvo":
				Set("this turn", ("for " + n + " turns"));
				return;
			case "Unmovable":
				Set("The first time you gain", ("The first " + n + " times you gain"));
				return;
			}
			if (key == "extra:coverage")
			{
				if (card == "Iteration")
				{
					Set("The first time you draw", ("The first " + n + " times you draw"));
					return;
				}
				Set("The first Attack", ("The first " + n + " Attacks"));
				Set("The first \\[gold\\]Shiv\\[/gold\\]", ("The first " + n + " [gold]Shivs[/gold]"));
				Set("deals", "deal");
				return;
			}
			if (card == "Tracking")
			{
				Sub("(take )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*% more", (Match m) => m.Groups[1].Value + n + "% more");
				Set("double damage", (Factor(n) + " damage"));
				return;
			}
			if (kind == "splashPercent")
			{
				Set("equal to the damage dealt", ("equal to " + n + "% of the damage dealt"));
				return;
			}
			if (card == "Flanking")
			{
				DoubleWord();
				return;
			}
			if (kind == "potions")
			{
				Set("Procure a random potion", ("Procure " + n + " random " + Plural(n, "potion", "potions")));
				return;
			}
			// Cards that add cards to your hand: "Add a random Power" -> "Add 3 random Powers".
			if (kind == "generate" || kind == "souls" || kind == "shivMultiplier" || kind == "generationMultiplier" || card == "InfiniteBlades" || card == "HelloWorld" || card == "Calamity" || card == "Soulbound")
			{
				Sub("((?:[Aa]dd|adds) )(?:a|1) (random )?(?:\\[gold\\])?(Power|Skill|Attack|Soul|Shiv|Common card|Colorless card)(?:\\[/gold\\])?", delegate(Match m)
				{
					string one = m.Groups[3].Value;
					string many = one.Contains("card") ? one.Replace("card", "cards") : one + "s";
					string word = Plural(n, one, many);
					if (m.Value.Contains("[gold]"))
					{
						word = "[gold]" + word + "[/gold]";
					}
					return m.Groups[1].Value + n + " " + (m.Groups[2].Value) + word;
				});
				Sub("(Add )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? Colorless) card", (Match m) => m.Groups[1].Value + n + " " + ((m.Groups[2].Value + " " + Plural(n, "card", "cards"))));
				Sub("(adds )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? Colorless) card", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + " " + Plural(n, "card", "cards"));
				Sub("(Add )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? )card", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + Plural(n, "card", "cards"));
				if (matched && n > 1)
				{
					Set("It's free", "They're free");
				}
				return;
			}
			if (kind == "attackAutoPlay")
			{
				Set("Play a random Attack", ("Play " + n + " random Attacks"));
				return;
			}
			if (kind == "autoPlay" || card == "Mayhem")
			{
				Set("(?:Play|play) the top card", (((card == "Mayhem") ? "play" : "Play") + " the top " + cards));
				if (matched)
				{
					Set("Exhaust\\[/gold\\] it", "Exhaust[/gold] them");
				}
				return;
			}
			switch (card)
			{
			case "Nightmare":
				Sub("(add )3 copies", (Match m) => m.Groups[1].Value + n + " " + Plural(n, "copy", "copies"));
				return;
			case "Juggling":
				Set("a copy of the", (n + " copies of the"));
				return;
			case "Aggression":
				Set("put a random Attack", ("put up to " + n + " random Attacks"));
				if (matched)
				{
					Set("Upgrade\\[/gold\\] it", "Upgrade[/gold] them");
				}
				return;
			case "Stratagem":
				Set("choose a card from it", ("choose " + optional + " from it"));
				return;
			case "ForbiddenGrimoire":
				Set("remove a card", ("remove " + optional));
				return;
			}
			if (kind == "huntRewards")
			{
				Set("an additional card reward", (n + " additional card rewards"));
				return;
			}
			switch (card)
			{
			case "Pounce":
			case "Synthesis":
			case "Unrelenting":
			case "Veilpiercer":
				Sub("The next (Skill|Power|Attack|\\[gold\\]Ethereal\\[/gold\\] card) you play costs", delegate(Match m)
				{
					string what = m.Groups[1].Value;
					return "The next " + n + " " + (what.Contains("card") ? what.Replace("card", "cards") : what + "s") + " you play cost";
				});
				return;
			case "Subroutine":
			case "TheSealedThrone":
				Sub("(gain )([^\\n.]+)\\.", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + ".");
				return;
			}
			if (kind == "spreadDebuffs")
			{
				Set("Apply any debuffs on the enemy", ("Apply " + n + " times each debuff on the enemy"));
			}
			else if (key == "extradown:quest")
			{
				Sub("((?:after|After entering) )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
			}
			else if (key == "extra:debilitatePercent")
			{
				Set("are twice as effective", ("are " + (100L + (long)n) + "% as effective"));
				if (matched && n >= 260)
				{
					text += "\nWeak retains at least 10% damage.";
				}
			}
			else if (kind == "selfPercent")
			{
				Set("lose 1 HP for each card in your \\[gold\\]Hand\\[/gold\\]", ("lose HP equal to " + n + "% of the number of cards in your [gold]Hand[/gold]"));
			}
			else if (kind == "statuses")
			{
				Sub("Add (?:a|\\d+) \\[gold\\]([^\\[]+)\\[/gold\\] into your \\[gold\\](Discard Pile|Hand)\\[/gold\\]\\.", delegate(Match m)
				{
					if (n == 0)
					{
						return "";
					}
					string status = m.Groups[1].Value;
					if (n > 1 && status == "Wound")
					{
						status = "Wounds";
					}
					if (n == 1 && status == "Wounds")
					{
						status = "Wound";
					}
					return "Add " + n + " [gold]" + status + "[/gold]" + " into your [gold]" + m.Groups[2].Value + "[/gold].";
				});
			}
			else if (kind == "enchant")
			{
				Sub("\\[purple\\]Inky\\[/purple\\]", (Match m) => ((n + " " + m.Value)));
			}
			else if (kind == "initialOrb")
			{
				Sub("^\\[gold\\]Channel\\[/gold\\] 1", (Match _) => "[gold]Channel[/gold] " + n);
			}
			else if (key == "enchanteffect:goopyGrowth")
			{
				// Goopy's growth sits inside the enchantment's [purple] text.
				text = Regex.Replace(text, "\\[purple\\]([^\\n]*?)\\[/purple\\]", delegate(Match m)
				{
					string pattern = "(Permanently increase this card's \\[gold\\]Block\\[/gold\\] by )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*";
					return "[purple]" + Regex.Replace(m.Groups[1].Value, pattern, delegate(Match growth)
					{
						matched = true;
						return growth.Groups[1].Value + n;
					}, RegexOptions.None, Timeout) + "[/purple]";
				}, RegexOptions.None, Timeout);
			}
		}

		void DoubleWord()
		{
			Sub("\\b([Dd]ouble) ([^\\n.]+)", (Match m) => (n > 5) ? ((char.IsUpper(m.Value[0]) ? "M" : "m") + "ultiply " + m.Groups[2].Value + " by " + n) : (Verb(n, char.IsUpper(m.Value[0])) + " " + m.Groups[2].Value));
			Set("\\bdouble(?= (?:damage|attack damage))", Factor(n));
		}

		void Draw()
		{
			Sub("\\b([Dd]raw) (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards?", (Match m) => m.Groups[1].Value + " " + cards);
		}

		void Set(string pattern, string value)
		{
			Sub(pattern, (Match _) => value);
		}

		void Sub(string pattern, MatchEvaluator replace)
		{
			text = Regex.Replace(text, pattern, delegate(Match m)
			{
				matched = true;
				return replace(m);
			}, RegexOptions.None, Timeout);
		}
	}
}
