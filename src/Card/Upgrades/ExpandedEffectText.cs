using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Tinker.Card;

internal static class ExpandedEffectText
{
	private const string Number = "(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*";

	private static string Plural(int n, string one, string many)
	{
		if (n != 1)
		{
			return many;
		}
		return one;
	}

	private static string Times(int n)
	{
		if (n != 1)
		{
			return $"{n} " + "times";
		}
		return "once";
	}

	private static string Factor(int n)
	{
		string[] array = (new string[6] { "", "", "double", "triple", "quadruple", "quintuple" });
		if (n < 2 || n > 5)
		{
			return n + " times";
		}
		return array[n];
	}

	private static string Verb(int n, bool capital)
	{
		string[] array = (new string[6] { "", "", "double", "triple", "quadruple", "quintuple" });
		string text = ((n >= 2 && n <= 5) ? array[n] : (("multiply by " + n)));
		if (!capital)
		{
			return text;
		}
		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	internal static string RewriteLine(string text, string card, string key, string kind, int n)
	{
		bool matched = false;
		string cards = n + " " + Plural(n, "card", "cards");
		string optional = "up to " + cards;
		switch (key)
		{
		case "extra:statusOrbs":
			Set("(?<=\\[/gold\\] )1 random Orb", (n + " random " + Plural(n, "Orb", "Orbs")));
			break;
		case "extra:shadowTurns":
			Set("Next turn", ("For the next " + n + " turns"));
			break;
		default:
			if (!(kind == "hangGrowth") && !(kind == "energyMultiplier") && !(kind == "vulnerableMultiplier") && !(card == "Entrench"))
			{
				if (card == "Shadowmeld")
				{
					string factor = ((n < 31) ? (1L << n).ToString(CultureInfo.InvariantCulture) : ("2^" + n));
					Sub("Double ([^\\n.]+)", (Match m) => "Multiply " + m.Groups[1].Value + " by " + factor);
					break;
				}
				switch (key)
				{
				case "extradown:targetCost":
					Sub("( to )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= this)", (Match m) => m.Groups[1].Value + n);
					break;
				case "extra:sharedForge":
					Set("all allies \\[gold\\]Forge\\[/gold\\] as well", ("all allies [gold]Forge[/gold] " + n + "% of that amount"));
					break;
				case "extra:explosionDamage":
					Sub("(deal )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= damage to ALL)", (Match m) => m.Groups[1].Value + n);
					break;
				case "extradown:poisonThreshold":
					Set("Every (?:time|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* times) you apply", ("Every " + ((n == 1) ? "time" : (n + " times")) + " you apply"));
					break;
				case "extra:scareWeak":
					Sub("(Apply )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= \\[gold\\]Weak)", (Match m) => m.Groups[1].Value + n);
					break;
				default:
					if (card == "Reflect")
					{
						Set("(?:Blocked attack damage|\\d+% of blocked attack damage) is reflected to your attacker this turn", (n + "% of blocked attack damage is reflected to your attacker this turn"));
						break;
					}
					switch (key)
					{
					case "extra:gambleDraw":
						Set("draw that many cards", ("draw that many cards plus " + cards));
						break;
					case "extra:allowedDraw":
						Set("You cannot draw additional cards this turn", ("You may only draw up to " + n + " additional " + Plural(n, "card", "cards") + " this turn"));
						break;
					case "extra:drawTolerance":
						Set("Can only be played if there are no cards in your", ("Can only be played with " + optional + " in your"));
						break;
					case "extra:nonAttackTolerance":
						Set("Can only be played if every card in your \\[gold\\]Hand\\[/gold\\] is an Attack", ("Can only be played with up to " + n + " non-Attack " + Plural(n, "card", "cards") + " in your [gold]Hand[/gold]"));
						break;
					case "extra:killReplays":
						Set("Repeat this effect", ("Repeat this effect " + Times(n)));
						break;
					default:
						{
							if (kind == "reduceDiscard")
							{
								Sub("\\bDiscard (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards?\\.?", (Match _) => (n != 0) ? ("Discard " + cards + ".") : "");
								break;
							}
							if (kind == "reduceHand" || card == "PhotonCut")
							{
								Sub("Put (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards? from your \\[gold\\]Hand\\[/gold\\] on top of your \\[gold\\]Draw Pile\\[/gold\\]\\.", (Match _) => (n != 0) ? (("Put " + cards + " from your [gold]Hand[/gold] on top of your [gold]Draw Pile[/gold].")) : "");
								break;
							}
							if (kind == "exhaustHand" || kind == "mixedExhaust" || kind == "exhaustPile" || kind == "randomExhaust")
							{
								Sub("\\[gold\\]Exhaust\\[/gold\\] (?:a|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*) cards?(?: at random)?(?: from your \\[gold\\]Draw Pile\\[/gold\\])?\\.", delegate(Match m)
								{
									if (n == 0)
									{
										return "";
									}
									bool flag = kind == "randomExhaust" || m.Value.Contains("aleatoria") || m.Value.Contains("at random");
									string text2 = (flag ? cards : optional);
									return ("[gold]Exhaust[/gold] " + text2 + (flag ? " at random" : "") + ((kind == "exhaustPile") ? " from your [gold]Draw Pile[/gold]" : "") + ".");
								});
								break;
							}
							if (kind == "putBack" || kind == "retrieve")
							{
								Sub("Put (a card|an Attack|a Skill)", delegate(Match m)
								{
									string text2 = (m.Groups[1].Value.Contains("Attack") ? Plural(n, "Attack", "Attacks") : (m.Groups[1].Value.Contains("Skill") ? Plural(n, "Skill", "Skills") : Plural(n, "card", "cards")));
									return "Put up to " + n + " " + text2;
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
								break;
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
								break;
							}
							if (kind == "hits" || kind == "conditionalHits")
							{
								Set("\\btwice\\b", Times(n));
								break;
							}
							if (kind == "x")
							{
								Sub("\\bX(?:\\+(\\d+))?\\b", (Match m) => "X+" + checked(n + (m.Groups[1].Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0)));
								break;
							}
							if (kind == "draw" || key == "extra:draw" || card == "DarkEmbrace" || card == "Predator")
							{
								Draw();
								break;
							}
							if (kind == "discount" || kind == "surcharge")
							{
								Sub("(Costs )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
								Sub("((?:reduce|Reduce|Increase) (?:this card's cost|its cost) by )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
								if (kind == "surcharge" && n == 0)
								{
									Set("Increase this card's cost by 0\\.", "");
								}
								break;
							}
							if (kind == "chooseGenerated" || kind == "options")
							{
								Sub("(Choose )(\\d+)( of )(\\d+)", (Match m) => m.Groups[1].Value + ((kind == "chooseGenerated") ? n.ToString() : m.Groups[2].Value) + m.Groups[3].Value + ((kind == "options") ? n.ToString() : m.Groups[4].Value));
								if (kind == "chooseGenerated" && n > 1)
								{
									Set("It's free", "They're free");
								}
								break;
							}
							if (kind == "darkPassive" || kind == "teslaPassive")
							{
								Sub("(Trigger [^\\n.]+?)(?: twice)?\\.", (Match m) => m.Groups[1].Value + " " + Times(n) + ".");
								break;
							}
							if (key == "extra:channels" && card == "LightningRod")
							{
								Sub("(\\[gold\\]Channel\\[/gold\\] )1", (Match m) => m.Groups[1].Value + n);
								break;
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
								break;
							}
							if (!(kind == "blockMultiplier") && !(kind == "groupResource"))
							{
								switch (card)
								{
								case "BlightStrike":
								case "Prolong":
									break;
								case "Underworld":
								case "ReaperForm":
								case "NecroMastery":
									goto IL_1113;
								default:
									goto IL_11b4;
								}
							}
							Set("equal to damage", ("equal to " + n + " times damage"));
							Set("equal to your", ("equal to " + n + " times your"));
							break;
						}
						IL_1113:
						Set("that much(?= \\[gold\\]Doom)", (n + " times that much"));
						Set("that much HP", (n + " times that much HP"));
						break;
						IL_11b4:
						switch (key)
						{
						case "extra:protectionPercent":
							Sub("((?:You receive|Allies take) )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*% less", (Match m) => m.Groups[1].Value + n + "% less");
							Set("half that much", (n + "% of that"));
							Set("Allies take half damage", ("Allies take " + n + "% less damage"));
							break;
						case "extra:normalityLimit":
							Sub("(more than )3", (Match m) => m.Groups[1].Value + n);
							break;
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
							break;
						default:
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
								break;
							}
							switch (card)
							{
							case "Conqueror":
							case "Convergence":
							case "Salvo":
								Set("this turn", ("for " + n + " turns"));
								break;
							case "Unmovable":
								Set("The first time you gain", ("The first " + n + " times you gain"));
								break;
							default:
								{
									if (key == "extra:coverage")
									{
										if (card == "Iteration")
										{
											Set("The first time you draw", ("The first " + n + " times you draw"));
											break;
										}
										Set("The first Attack", ("The first " + n + " Attacks"));
										Set("The first \\[gold\\]Shiv\\[/gold\\]", ("The first " + n + " [gold]Shivs[/gold]"));
										Set("deals", "deal");
										break;
									}
									if (card == "Tracking")
									{
										Sub("(take )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*% more", (Match m) => m.Groups[1].Value + n + "% more");
										Set("double damage", (Factor(n) + " damage"));
										break;
									}
									if (kind == "splashPercent")
									{
										Set("equal to the damage dealt", ("equal to " + n + "% of the damage dealt"));
										break;
									}
									if (card == "Flanking")
									{
										DoubleWord();
										break;
									}
									if (kind == "potions")
									{
										Set("Procure a random potion", ("Procure " + n + " random " + Plural(n, "potion", "potions")));
										break;
									}
									if (!(kind == "generate") && !(kind == "souls") && !(kind == "shivMultiplier") && !(kind == "generationMultiplier"))
									{
										switch (card)
										{
										case "InfiniteBlades":
										case "HelloWorld":
										case "Calamity":
										case "Soulbound":
											break;
										default:
											goto IL_1b39;
										}
									}
									Sub("((?:[Aa]dd|adds) )(?:a|1) (random )?(?:\\[gold\\])?(Power|Skill|Attack|Soul|Shiv|Common card|Colorless card)(?:\\[/gold\\])?", delegate(Match m)
									{
										string value = m.Groups[3].Value;
										string many = value + "s";
										if (value.Contains("card"))
										{
											many = value.Replace("card", "cards");
										}
										string text2 = Plural(n, value, many);
										if (m.Value.Contains("[gold]"))
										{
											text2 = "[gold]" + text2 + "[/gold]";
										}
										return m.Groups[1].Value + n + " " + (m.Groups[2].Value) + text2;
									});
									Sub("(Add )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? Colorless) card", (Match m) => m.Groups[1].Value + n + " " + ((m.Groups[2].Value + " " + Plural(n, "card", "cards"))));
									Sub("(adds )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? Colorless) card", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + " " + Plural(n, "card", "cards"));
									Sub("(Add )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? )card", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + Plural(n, "card", "cards"));
									if (matched && n > 1)
									{
										Set("It's free", "They're free");
									}
									break;
								}
								IL_1dc9:
								Set("choose a card from it", ("choose " + optional + " from it"));
								break;
								IL_1cc3:
								Set("a copy of the", (n + " copies of the"));
								break;
								IL_1b39:
								if (kind == "attackAutoPlay")
								{
									Set("Play a random Attack", ("Play " + n + " random Attacks"));
									break;
								}
								if (!(kind == "autoPlay"))
								{
									switch (card)
									{
									case "Mayhem":
										break;
									case "Nightmare":
										goto IL_1c8b;
									case "Juggling":
										goto IL_1cc3;
									case "Aggression":
										goto IL_1d26;
									case "Stratagem":
										goto IL_1dc9;
									case "ForbiddenGrimoire":
										goto IL_1e29;
									default:
										goto IL_1e72;
									}
								}
								Set("(?:Play|play) the top card", (((card == "Mayhem") ? "play" : "Play") + " the top " + cards));
								if (matched)
								{
									Set("Exhaust\\[/gold\\] it", "Exhaust[/gold] them");
								}
								break;
								IL_1e72:
								if (kind == "huntRewards")
								{
									Set("an additional card reward", (n + " additional card rewards"));
									break;
								}
								switch (card)
								{
								case "Pounce":
								case "Synthesis":
								case "Unrelenting":
								case "Veilpiercer":
									Sub("The next (Skill|Power|Attack|\\[gold\\]Ethereal\\[/gold\\] card) you play costs", delegate(Match m)
									{
										string value = m.Groups[1].Value;
										string text2 = value + "s";
										return ("The next " + n + " " + (value.Contains("card") ? value.Replace("card", "cards") : text2) + " you play cost");
									});
									break;
								case "Subroutine":
								case "TheSealedThrone":
									Sub("(gain )([^\\n.]+)\\.", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + ".");
									break;
								default:
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
											string text2 = m.Groups[1].Value;
											if (n > 1 && text2 == "Wound")
											{
												text2 = "Wounds";
											}
											if (n == 1 && text2 == "Wounds")
											{
												text2 = "Wound";
											}
											if (n > 1 && text2 == "Herida")
											{
												text2 = "Heridas";
											}
											if (n == 1 && text2 == "Heridas")
											{
												text2 = "Herida";
											}
											return "Add " + n + " [gold]" + text2 + "[/gold]" + " into your [gold]" + m.Groups[2].Value + "[/gold].";
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
									else
									{
										if (!(key == "enchanteffect:goopyGrowth"))
										{
											break;
										}
										text = Regex.Replace(text, "\\[purple\\]([^\\n]*?)\\[/purple\\]", delegate(Match m)
										{
											string pattern = "(Permanently increase this card's \\[gold\\]Block\\[/gold\\] by )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*";
											return "[purple]" + Regex.Replace(m.Groups[1].Value, pattern, delegate(Match growth)
											{
												matched = true;
												return growth.Groups[1].Value + n;
											}, RegexOptions.None, TimeSpan.FromMilliseconds(50L, 0L)) + "[/purple]";
										}, RegexOptions.None, TimeSpan.FromMilliseconds(50L, 0L));
									}
									break;
								}
								break;
								IL_1d26:
								Set("put a random Attack", ("put up to " + n + " random Attacks"));
								if (matched)
								{
									Set("Upgrade\\[/gold\\] it", "Upgrade[/gold] them");
								}
								break;
								IL_1c8b:
								Sub("(add )3 copies", (Match m) => m.Groups[1].Value + n + " " + Plural(n, "copy", "copies"));
								break;
								IL_1e29:
								Set("remove a card", ("remove " + optional));
								break;
							}
							break;
						}
						break;
					}
					break;
				}
				break;
			}
			goto case "extra:shadowMultiplier";
		case "extra:shadowMultiplier":
		case "extra:conditionalMultiplier":
			DoubleWord();
			break;
		}
		if (!matched)
		{
			return null;
		}
		return Regex.Replace(text, "\\n{2,}", "\n").Trim();
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
			}, RegexOptions.None, TimeSpan.FromMilliseconds(50L, 0L));
		}
	}
}
