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

	private static string Times(int n, bool es)
	{
		if (n != 1)
		{
			return $"{n} " + (es ? "veces" : "times");
		}
		if (!es)
		{
			return "once";
		}
		return "una vez";
	}

	private static string Factor(int n, bool es)
	{
		string[] array = ((!es) ? new string[6] { "", "", "double", "triple", "quadruple", "quintuple" } : new string[6] { "", "", "doble", "triple", "cuádruple", "quíntuple" });
		if (n < 2 || n > 5)
		{
			return n + (es ? " veces" : " times");
		}
		return array[n];
	}

	private static string Verb(int n, bool es, bool capital)
	{
		string[] array = ((!es) ? new string[6] { "", "", "double", "triple", "quadruple", "quintuple" } : new string[6] { "", "", "duplica", "triplica", "cuadruplica", "quintuplica" });
		string text = ((n >= 2 && n <= 5) ? array[n] : (es ? ("multiplica por " + n) : ("multiply by " + n)));
		if (!capital)
		{
			return text;
		}
		return char.ToUpperInvariant(text[0]) + text.Substring(1);
	}

	internal static string RewriteLine(string text, string card, string key, string kind, int n, bool es)
	{
		bool matched = false;
		string cards = n + " " + Plural(n, es ? "carta" : "card", es ? "cartas" : "cards");
		string optional = (es ? "hasta " : "up to ") + cards;
		switch (key)
		{
		case "extra:statusOrbs":
			Set(es ? "(?<=\\[/gold\\] )1 Orbe aleatorio" : "(?<=\\[/gold\\] )1 random Orb", es ? (n + " " + Plural(n, "Orbe aleatorio", "Orbes aleatorios")) : (n + " random " + Plural(n, "Orb", "Orbs")));
			break;
		case "extra:shadowTurns":
			Set(es ? "Durante tu siguiente turno" : "Next turn", es ? ("Durante tus siguientes " + n + " turnos") : ("For the next " + n + " turns"));
			break;
		default:
			if (!(kind == "hangGrowth") && !(kind == "energyMultiplier") && !(kind == "vulnerableMultiplier") && !(card == "Entrench"))
			{
				if (card == "Shadowmeld")
				{
					string factor = ((n < 31) ? (1L << n).ToString(CultureInfo.InvariantCulture) : ("2^" + n));
					Sub(es ? "Duplica ([^\\n.]+)" : "Double ([^\\n.]+)", (Match m) => (es ? "Multiplica " : "Multiply ") + m.Groups[1].Value + (es ? " por " : " by ") + factor);
					break;
				}
				switch (key)
				{
				case "extradown:targetCost":
					Sub(es ? "( a )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= durante este)" : "( to )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= this)", (Match m) => m.Groups[1].Value + n);
					break;
				case "extra:sharedForge":
					Set(es ? "todos los aliados también \\[gold\\]Forjan\\[/gold\\]" : "all allies \\[gold\\]Forge\\[/gold\\] as well", es ? ("todos los aliados [gold]Forjan[/gold] un " + n + " % de esa cantidad") : ("all allies [gold]Forge[/gold] " + n + "% of that amount"));
					break;
				case "extra:explosionDamage":
					Sub(es ? "(inflige )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= de daño a TODOS)" : "(deal )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= damage to ALL)", (Match m) => m.Groups[1].Value + n);
					break;
				case "extradown:poisonThreshold":
					Set(es ? "Cada (?:vez|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* veces) que apliques" : "Every (?:time|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* times) you apply", es ? ("Cada " + ((n == 1) ? "vez" : (n + " veces")) + " que apliques") : ("Every " + ((n == 1) ? "time" : (n + " times")) + " you apply"));
					break;
				case "extra:scareWeak":
					Sub(es ? "(Aplica )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= de \\[gold\\]Debilidad)" : "(Apply )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= \\[gold\\]Weak)", (Match m) => m.Groups[1].Value + n);
					break;
				default:
					if (card == "Reflect")
					{
						Set(es ? "(?:El daño|Un \\d+ % del daño) de ataque bloqueado durante este turno se devuelve al atacante" : "(?:Blocked attack damage|\\d+% of blocked attack damage) is reflected to your attacker this turn", es ? ("Un " + n + " % del daño de ataque bloqueado durante este turno se devuelve al atacante") : (n + "% of blocked attack damage is reflected to your attacker this turn"));
						break;
					}
					switch (key)
					{
					case "extra:gambleDraw":
						Set(es ? "roba el mismo número de cartas" : "draw that many cards", es ? ("roba el mismo número de cartas y " + cards + " adicionales") : ("draw that many cards plus " + cards));
						break;
					case "extra:allowedDraw":
						Set(es ? "No puedes robar más cartas este turno" : "You cannot draw additional cards this turn", es ? ("Solo puedes robar " + optional + " adicionales este turno") : ("You may only draw up to " + n + " additional " + Plural(n, "card", "cards") + " this turn"));
						break;
					case "extra:drawTolerance":
						Set(es ? "Solo puede jugarse si no tienes cartas en la" : "Can only be played if there are no cards in your", es ? ("Solo puede jugarse si tienes " + optional + " en la") : ("Can only be played with " + optional + " in your"));
						break;
					case "extra:nonAttackTolerance":
						Set(es ? "Solo se puede jugar si todas las cartas de tu \\[gold\\]Mano\\[/gold\\] son Ataques" : "Can only be played if every card in your \\[gold\\]Hand\\[/gold\\] is an Attack", es ? ("Solo se puede jugar si tienes " + optional + ((n == 1) ? " que no sea un Ataque" : " que no sean Ataques") + " en tu [gold]Mano[/gold]") : ("Can only be played with up to " + n + " non-Attack " + Plural(n, "card", "cards") + " in your [gold]Hand[/gold]"));
						break;
					case "extra:killReplays":
						Set(es ? "Repite este efecto" : "Repeat this effect", es ? ("Repite este efecto " + Times(n, es: true)) : ("Repeat this effect " + Times(n, es: false)));
						break;
					default:
						{
							if (kind == "reduceDiscard")
							{
								Sub(es ? "\\bDescarta (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cartas?\\.?" : "\\bDiscard (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards?\\.?", (Match _) => (n != 0) ? ((es ? "Descarta " : "Discard ") + cards + ".") : "");
								break;
							}
							if (kind == "reduceHand" || card == "PhotonCut")
							{
								Sub(es ? "Coloca (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cartas? de tu \\[gold\\]Mano\\[/gold\\] en la parte superior de tu \\[gold\\]Pila de robo\\[/gold\\]\\." : "Put (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards? from your \\[gold\\]Hand\\[/gold\\] on top of your \\[gold\\]Draw Pile\\[/gold\\]\\.", (Match _) => (n != 0) ? ((!es) ? ("Put " + cards + " from your [gold]Hand[/gold] on top of your [gold]Draw Pile[/gold].") : ("Coloca " + cards + " de tu [gold]Mano[/gold] en la parte superior de tu [gold]Pila de robo[/gold].")) : "");
								break;
							}
							if (kind == "exhaustHand" || kind == "mixedExhaust" || kind == "exhaustPile" || kind == "randomExhaust")
							{
								Sub(es ? "\\[gold\\]Agota\\[/gold\\] (?:una|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*) cartas?(?: aleatorias?)?(?: de tu \\[gold\\]Pila de robo\\[/gold\\])?\\." : "\\[gold\\]Exhaust\\[/gold\\] (?:a|(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*) cards?(?: at random)?(?: from your \\[gold\\]Draw Pile\\[/gold\\])?\\.", delegate(Match m)
								{
									if (n == 0)
									{
										return "";
									}
									bool flag = kind == "randomExhaust" || m.Value.Contains("aleatoria") || m.Value.Contains("at random");
									string text2 = (flag ? cards : optional);
									return (!es) ? ("[gold]Exhaust[/gold] " + text2 + (flag ? " at random" : "") + ((kind == "exhaustPile") ? " from your [gold]Draw Pile[/gold]" : "") + ".") : ("[gold]Agota[/gold] " + text2 + (flag ? (" " + Plural(n, "aleatoria", "aleatorias")) : "") + ((kind == "exhaustPile") ? " de tu [gold]Pila de robo[/gold]" : "") + ".");
								});
								break;
							}
							if (kind == "putBack" || kind == "retrieve")
							{
								Sub(es ? "Coloca (una carta|un Ataque|una Habilidad)" : "Put (a card|an Attack|a Skill)", delegate(Match m)
								{
									string text2 = ((!es) ? (m.Groups[1].Value.Contains("Attack") ? Plural(n, "Attack", "Attacks") : (m.Groups[1].Value.Contains("Skill") ? Plural(n, "Skill", "Skills") : Plural(n, "card", "cards"))) : (m.Groups[1].Value.Contains("Ataque") ? Plural(n, "Ataque", "Ataques") : (m.Groups[1].Value.Contains("Habilidad") ? Plural(n, "Habilidad", "Habilidades") : Plural(n, "carta", "cartas"))));
									return (es ? "Coloca hasta " : "Put up to ") + n + " " + text2;
								});
								if (card == "SeekerStrike")
								{
									Set(es ? "Elige 1 de entre" : "Choose 1 of", es ? ("Elige hasta " + n + " de entre") : ("Choose up to " + n + " of"));
								}
								if (card == "Tutor")
								{
									Set(es ? "elige una carta" : "chooses a card", es ? ("elige " + optional) : ("chooses " + optional));
									Set(es ? "y la añade" : "to add", es ? "y las añade" : "to add");
								}
								if (es && card == "SeekerStrike")
								{
									Set("y añádela", "y añádelas");
								}
								break;
							}
							if (key == "extra:returnCount")
							{
								if (card == "Rebound")
								{
									Set(es ? "la siguiente carta que juegues" : "the next card you play", es ? ("las siguientes " + cards + " que juegues") : ("the next " + cards + " you play"));
								}
								else
								{
									Set(es ? "El primer Ataque o Habilidad que juegues" : "The first Attack or Skill you play", es ? ("Los primeros " + n + " Ataques o Habilidades que juegues") : ("The first " + n + " Attacks or Skills you play"));
									Set(es ? "se coloca" : "is placed", es ? "se colocan" : "are placed");
								}
								if (n == 0 && matched)
								{
									Set(es ? "(?:Coloca las siguientes|Los primeros) 0 [^\\n.]+\\." : "(?:Put the next|The first) 0 [^\\n.]+\\.", es ? "No devuelve cartas a la [gold]Pila de robo[/gold]." : "No cards are returned to the [gold]Draw Pile[/gold].");
								}
								break;
							}
							if (kind == "hits" || kind == "conditionalHits")
							{
								Set(es ? "\\bdos veces\\b" : "\\btwice\\b", Times(n, es));
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
								Sub(es ? "(Cuesta )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*" : "(Costs )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
								Sub(es ? "((?:reduce|Reduce|Aumenta) (?:el coste de esta carta|su coste) en )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*" : "((?:reduce|Reduce|Increase) (?:this card's cost|its cost) by )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
								if (kind == "surcharge" && n == 0)
								{
									Set(es ? "Aumenta el coste de esta carta en 0\\." : "Increase this card's cost by 0\\.", "");
								}
								break;
							}
							if (kind == "chooseGenerated" || kind == "options")
							{
								Sub(es ? "(Elige )(\\d+)( de entre )(\\d+)" : "(Choose )(\\d+)( of )(\\d+)", (Match m) => m.Groups[1].Value + ((kind == "chooseGenerated") ? n.ToString() : m.Groups[2].Value) + m.Groups[3].Value + ((kind == "options") ? n.ToString() : m.Groups[4].Value));
								if (kind == "chooseGenerated" && n > 1)
								{
									Set(es ? "y añád(?:ela|elo)" : "It's free", es ? "y añádelas" : "They're free");
									Set("Puedes jugar(?:la|lo) gratis", "Puedes jugarlas gratis");
								}
								break;
							}
							if (kind == "darkPassive" || kind == "teslaPassive")
							{
								Sub(es ? "(Activa [^\\n.]+?)(?: dos veces)?\\." : "(Trigger [^\\n.]+?)(?: twice)?\\.", (Match m) => m.Groups[1].Value + " " + Times(n, es) + ".");
								break;
							}
							if (key == "extra:channels" && card == "LightningRod")
							{
								Sub(es ? "(\\[gold\\]Invoca\\[/gold\\] )1" : "(\\[gold\\]Channel\\[/gold\\] )1", (Match m) => m.Groups[1].Value + n);
								break;
							}
							if (key == "extra:replays" || card == "TagTeam")
							{
								Set(es ? "una vez más" : "an (?:extra|additional) time", es ? (Times(n, es: true) + " más") : (n + " additional " + Plural(n, "time", "times")));
								if (kind == "autoReplays" || kind == "autoDrawReplays")
								{
									Set(es ? "se juega(?=\\.)" : "play it(?=\\.)", es ? ("se juega " + Times(n, es: true)) : ("play it " + Times(n, es: false)));
									if (es)
									{
										Set("juégala(?=\\.)", "juégala " + Times(n, es: true));
									}
								}
								if (card == "Hellraiser")
								{
									Set(es ? "se juega contra" : "it is played against", es ? ("se juega " + Times(n, es: true) + " contra") : ("it is played " + Times(n, es: false) + " against"));
								}
								if (card == "KnifeTrap")
								{
									Set(es ? "contra el enemigo\\." : "on the enemy\\.", es ? ("contra el enemigo " + Times(n, es: true) + ".") : ("on the enemy " + Times(n, es: false) + "."));
								}
								if (card == "Eidolon")
								{
									Sub(es ? "(Juega TODAS[^\\n.]+)\\." : "(Play ALL[^\\n.]+)\\.", (Match m) => m.Groups[1].Value + " " + Times(n, es) + ".");
									Sub(es ? "([Gg]ana )1(?= .*Intangible)" : "([Gg]ain )1(?= .*Intangible)", (Match m) => m.Groups[1].Value + n);
								}
								if (kind == "grantReplays")
								{
									Sub(es ? "\\[gold\\]Repetición\\[/gold\\]" : "\\[gold\\]Replay\\[/gold\\]", (Match m) => n + " " + m.Value);
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
							Set(es ? "igual al daño" : "equal to damage", es ? ("igual a " + n + " veces el daño") : ("equal to " + n + " times damage"));
							Set(es ? "igual a tu" : "equal to your", es ? ("igual a " + n + " veces tu") : ("equal to " + n + " times your"));
							break;
						}
						IL_1113:
						Set(es ? "la misma cantidad de" : "that much(?= \\[gold\\]Doom)", es ? (n + " veces esa cantidad de") : (n + " times that much"));
						Set(es ? "los mismos PV" : "that much HP", es ? (n + " veces esos PV") : (n + " times that much HP"));
						break;
						IL_11b4:
						switch (key)
						{
						case "extra:protectionPercent":
							Sub(es ? "((?:Recibes|Los aliados reciben) un )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*\\s*% menos" : "((?:You receive|Allies take) )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*% less", (Match m) => m.Groups[1].Value + n + (es ? " % menos" : "% less"));
							Set(es ? "la mitad de ese" : "half that much", es ? ("un " + n + " % de ese") : (n + "% of that"));
							Set(es ? "Los aliados reciben la mitad de daño" : "Allies take half damage", es ? ("Los aliados reciben un " + n + " % menos de daño") : ("Allies take " + n + "% less damage"));
							break;
						case "extra:normalityLimit":
							Sub(es ? "(más de )3" : "(more than )3", (Match m) => m.Groups[1].Value + n);
							break;
						case "extradown:threshold":
							Sub(es ? "(Por cada |por cada |Cada vez que juegues )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= cartas)" : "(Every |Every time you play )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*(?= cards)", (Match m) => m.Groups[1].Value + n);
							if (card == "Juggling")
							{
								Set(es ? "tercer(?= \\s*(?:\\[gold\\])?Ataque)" : "third(?= \\s*(?:\\[gold\\])?Attack)", (!es) ? ((n == 1) ? "first" : ((n == 2) ? "second" : "third")) : ((n == 1) ? "primer" : ((n == 2) ? "segundo" : "tercer")));
							}
							if (card == "Orbit")
							{
								Set(es ? "Por cada [^\\n]+ que gastes" : "Every [^\\n]+ you spend", es ? ("Por cada " + n + " de Energía que gastes") : ("Every " + n + " Energy you spend"));
							}
							break;
						default:
							if (kind == "keywordHand" || kind == "selectHand" || kind == "upgradeHand" || kind == "transformHand")
							{
								Set(es ? "una carta(?= de tu \\[gold\\]Mano)" : "a card(?= in your \\[gold\\]Hand)", optional);
								Set(es ? "una Habilidad(?= de tu \\[gold\\]Mano)" : "a Skill(?= in your \\[gold\\]Hand)", (es ? "hasta " : "up to ") + n + " " + Plural(n, es ? "Habilidad" : "Skill", es ? "Habilidades" : "Skills"));
								if (kind == "transformHand")
								{
									Set(es ? "Transfórmala" : "Choose a card", es ? "Transfórmalas" : ("Choose " + optional));
								}
								if (card == "Transfigure" && matched)
								{
									Set(es ? "Cuesta 1 " : "It costs", es ? "Cuestan 1 " : "They cost");
								}
								break;
							}
							switch (card)
							{
							case "Conqueror":
							case "Convergence":
							case "Salvo":
								Set(es ? "este turno" : "this turn", es ? ("durante " + n + " turnos") : ("for " + n + " turns"));
								break;
							case "Unmovable":
								Set(es ? "La primera vez que ganes" : "The first time you gain", es ? ("Las primeras " + n + " veces que ganes") : ("The first " + n + " times you gain"));
								break;
							default:
								{
									if (key == "extra:coverage")
									{
										if (card == "Iteration")
										{
											Set(es ? "La primera vez que robes" : "The first time you draw", es ? ("Las primeras " + n + " veces que robes") : ("The first " + n + " times you draw"));
											break;
										}
										Set(es ? "El primer Ataque" : "The first Attack", es ? ("Los primeros " + n + " Ataques") : ("The first " + n + " Attacks"));
										Set(es ? "La primera \\[gold\\]Navaja\\[/gold\\]" : "The first \\[gold\\]Shiv\\[/gold\\]", es ? ("Las primeras " + n + " [gold]Navajas[/gold]") : ("The first " + n + " [gold]Shivs[/gold]"));
										Set(es ? "inflige" : "deals", es ? "infligen" : "deal");
										break;
									}
									if (card == "Tracking")
									{
										Sub(es ? "(reciben un )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*\\s*% más" : "(take )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*% more", (Match m) => m.Groups[1].Value + n + (es ? " % más" : "% more"));
										Set(es ? "el doble de daño" : "double damage", (!es) ? (Factor(n, es: false) + " damage") : ((n <= 5) ? ("el " + Factor(n, es: true) + " de daño") : (n + " veces el daño")));
										break;
									}
									if (kind == "splashPercent")
									{
										Set(es ? "Inflige el mismo daño" : "equal to the damage dealt", es ? ("Inflige un " + n + " % de ese daño") : ("equal to " + n + "% of the damage dealt"));
										break;
									}
									if (card == "Flanking")
									{
										DoubleWord();
										break;
									}
									if (kind == "potions")
									{
										Set(es ? "Obtén una poción aleatoria" : "Procure a random potion", es ? ("Obtén " + n + " " + Plural(n, "poción aleatoria", "pociones aleatorias")) : ("Procure " + n + " random " + Plural(n, "potion", "potions")));
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
									Sub(es ? "((?:[Aa]ñade) )(?:una|un|otro|1) (?:\\[gold\\])?(carta|Habilidad|Poder|Ataque|Alma|Navaja|Desorientación|Escombros|Quemadura|Viscoso|Vacío)(?:\\[/gold\\])?" : "((?:[Aa]dd|adds) )(?:a|1) (random )?(?:\\[gold\\])?(Power|Skill|Attack|Soul|Shiv|Common card|Colorless card)(?:\\[/gold\\])?", delegate(Match m)
									{
										string value = m.Groups[es ? 2 : 3].Value;
										string many = value switch
										{
											"Poder" => "Poderes", 
											"Habilidad" => "Habilidades", 
											"Alma" => "Almas", 
											_ => value + "s", 
										};
										if (value.Contains("card"))
										{
											many = value.Replace("card", "cards");
										}
										string text2 = Plural(n, value, many);
										if (m.Value.Contains("[gold]"))
										{
											text2 = "[gold]" + text2 + "[/gold]";
										}
										return m.Groups[1].Value + n + " " + (es ? "" : m.Groups[2].Value) + text2;
									});
									Sub(es ? "(añade )1 (carta incolora)" : "(Add )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? Colorless) card", (Match m) => m.Groups[1].Value + n + " " + (es ? Plural(n, "carta incolora", "cartas incoloras") : (m.Groups[2].Value + " " + Plural(n, "card", "cards"))));
									if (!es)
									{
										Sub("(adds )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? Colorless) card", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + " " + Plural(n, "card", "cards"));
										Sub("(Add )1 (random(?: \\[gold\\]Upgraded\\[/gold\\])? )card", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + Plural(n, "card", "cards"));
									}
									if (matched && n > 1)
									{
										object obj;
										switch (card)
										{
										default:
											obj = "as";
											break;
										case "InfernalBlade":
										case "WhiteNoise":
										case "Calamity":
											obj = "os";
											break;
										}
										Set("\\baleatori[oa]\\b", "aleatori" + (string?)obj);
										Set("\\bComún\\b", "Comunes");
										Set("\\bincolora\\b", "incoloras");
										Set("\\[gold\\]Mejorada\\[/gold\\]", "[gold]Mejoradas[/gold]");
										Set("It's free", "They're free");
										Set("Puedes jugar[lao]{2} gratis", "Puedes jugarlas gratis");
									}
									break;
								}
								IL_1dc9:
								Set(es ? "elige una carta y colócala" : "choose a card from it", es ? ("elige " + optional + " y colócalas") : ("choose " + optional + " from it"));
								break;
								IL_1cc3:
								Set(es ? "una copia del" : "a copy of the", es ? (n + " copias del") : (n + " copies of the"));
								break;
								IL_1b39:
								if (kind == "attackAutoPlay")
								{
									Set(es ? "Juega un Ataque aleatorio" : "Play a random Attack", es ? ("Juega " + n + " Ataques aleatorios") : ("Play " + n + " random Attacks"));
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
								Set(es ? "(?:Juega|juega) la primera carta" : "(?:Play|play) the top card", es ? (((card == "Mayhem") ? "juega" : "Juega") + " las primeras " + cards) : (((card == "Mayhem") ? "play" : "Play") + " the top " + cards));
								if (matched)
								{
									Set(es ? "Agótala" : "Exhaust\\[/gold\\] it", es ? "Agótalas" : "Exhaust[/gold] them");
								}
								break;
								IL_1e72:
								if (kind == "huntRewards")
								{
									Set(es ? "una recompensa de cartas adicional" : "an additional card reward", es ? (n + " recompensas de cartas adicionales") : (n + " additional card rewards"));
									break;
								}
								switch (card)
								{
								case "Pounce":
								case "Synthesis":
								case "Unrelenting":
								case "Veilpiercer":
									Sub(es ? "(?:La|El) siguiente (Habilidad|Poder|Ataque|carta \\[gold\\]Etérea\\[/gold\\]) que juegues cuesta" : "The next (Skill|Power|Attack|\\[gold\\]Ethereal\\[/gold\\] card) you play costs", delegate(Match m)
									{
										string value = m.Groups[1].Value;
										string text2 = value switch
										{
											"carta [gold]Etérea[/gold]" => "cartas [gold]Etéreas[/gold]", 
											"Poder" => "Poderes", 
											"Habilidad" => "Habilidades", 
											_ => value + "s", 
										};
										return (!es) ? ("The next " + n + " " + (value.Contains("card") ? value.Replace("card", "cards") : text2) + " you play cost") : ("L" + ((value == "Ataque" || value == "Poder") ? "os" : "as") + " siguientes " + n + " " + text2 + " que juegues cuestan");
									});
									break;
								case "Subroutine":
								case "TheSealedThrone":
									Sub(es ? "(gana )([^\\n.]+)\\." : "(gain )([^\\n.]+)\\.", (Match m) => m.Groups[1].Value + n + " " + m.Groups[2].Value + ".");
									break;
								default:
									if (kind == "spreadDebuffs")
									{
										Set(es ? "Aplica cualquier efecto negativo del enemigo" : "Apply any debuffs on the enemy", es ? ("Aplica " + n + " veces cada efecto negativo del enemigo") : ("Apply " + n + " times each debuff on the enemy"));
									}
									else if (key == "extradown:quest")
									{
										Sub(es ? "((?:tras|Tras entrar a) )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*" : "((?:after|After entering) )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*", (Match m) => m.Groups[1].Value + n);
									}
									else if (key == "extra:debilitatePercent")
									{
										Set(es ? "son el doble de efectivos" : "are twice as effective", es ? ("tienen un " + (100L + (long)n) + " % de eficacia") : ("are " + (100L + (long)n) + "% as effective"));
										if (matched && n >= 260)
										{
											text += (es ? "\nDébil conserva al menos un 10 % de daño." : "\nWeak retains at least 10% damage.");
										}
									}
									else if (kind == "selfPercent")
									{
										Set(es ? "pierde 1 PV por cada carta en tu \\[gold\\]Mano\\[/gold\\]" : "lose 1 HP for each card in your \\[gold\\]Hand\\[/gold\\]", es ? ("pierde PV equivalentes al " + n + " % del número de cartas en tu [gold]Mano[/gold]") : ("lose HP equal to " + n + "% of the number of cards in your [gold]Hand[/gold]"));
									}
									else if (kind == "statuses")
									{
										Sub(es ? "Añade (?:una?|\\d+) \\[gold\\]([^\\[]+)\\[/gold\\] a tu \\[gold\\](Pila de descarte|Mano)\\[/gold\\]\\." : "Add (?:a|\\d+) \\[gold\\]([^\\[]+)\\[/gold\\] into your \\[gold\\](Discard Pile|Hand)\\[/gold\\]\\.", delegate(Match m)
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
											return (es ? "Añade " : "Add ") + n + " [gold]" + text2 + "[/gold]" + (es ? " a tu [gold]" : " into your [gold]") + m.Groups[2].Value + "[/gold].";
										});
									}
									else if (kind == "enchant")
									{
										Sub(es ? "con \\[purple\\]Tinta\\[/purple\\]" : "\\[purple\\]Inky\\[/purple\\]", (Match m) => (!es) ? (n + " " + m.Value) : ("con " + n + " [purple]Tinta[/purple]"));
									}
									else if (kind == "initialOrb")
									{
										Sub(es ? "^\\[gold\\]Invoca\\[/gold\\] 1" : "^\\[gold\\]Channel\\[/gold\\] 1", (Match _) => (es ? "[gold]Invoca[/gold] " : "[gold]Channel[/gold] ") + n);
									}
									else
									{
										if (!(key == "enchanteffect:goopyGrowth"))
										{
											break;
										}
										text = Regex.Replace(text, "\\[purple\\]([^\\n]*?)\\[/purple\\]", delegate(Match m)
										{
											string pattern = (es ? "(Aumenta permanentemente el \\[gold\\]Bloqueo\\[/gold\\] obtenido mediante esta carta en )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*" : "(Permanently increase this card's \\[gold\\]Block\\[/gold\\] by )(?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])*");
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
								Set(es ? "coloca un Ataque aleatorio" : "put a random Attack", es ? ("coloca hasta " + n + " Ataques aleatorios") : ("put up to " + n + " random Attacks"));
								if (matched)
								{
									Set(es ? "Mejóralo" : "Upgrade\\[/gold\\] it", es ? "Mejóralos" : "Upgrade[/gold] them");
								}
								break;
								IL_1c8b:
								Sub(es ? "(añade )3 copias" : "(add )3 copies", (Match m) => m.Groups[1].Value + n + " " + Plural(n, es ? "copia" : "copy", es ? "copias" : "copies"));
								break;
								IL_1e29:
								Set(es ? "eliminar una carta" : "remove a card", es ? ("eliminar " + optional) : ("remove " + optional));
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
			Sub(es ? "\\b([Dd]uplica) ([^\\n.]+)" : "\\b([Dd]ouble) ([^\\n.]+)", (Match m) => (n > 5) ? ((char.IsUpper(m.Value[0]) ? "M" : "m") + (es ? "ultiplica " : "ultiply ") + m.Groups[2].Value + (es ? " por " : " by ") + n) : (Verb(n, es, char.IsUpper(m.Value[0])) + " " + m.Groups[2].Value));
			Set(es ? "\\bel doble de\\b" : "\\bdouble(?= (?:damage|attack damage))", (!es) ? Factor(n, es: false) : ((n <= 5) ? ("el " + Factor(n, es: true) + " de") : (n + " veces el")));
		}
		void Draw()
		{
			Sub(es ? "\\b([Rr]oba) (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cartas?" : "\\b([Dd]raw) (?:\\[[a-z]+\\])*\\d+(?:\\[/[a-z]+\\])* cards?", (Match m) => m.Groups[1].Value + " " + cards);
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
