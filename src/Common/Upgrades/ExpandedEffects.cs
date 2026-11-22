using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Common;

public static class ExpandedEffects
{
	public const string Hits = "extra:hits";

	public const string X = "extra:x";

	public const string Power = "extra:power";

	public const string Draw = "extra:draw";

	public const string Options = "extra:options";

	public const string Multiplier = "extra:multiplier";

	public const string Passive = "extra:passive";

	public const string Selection = "extra:selection";

	public const string Generation = "extra:generation";

	public const string Replays = "extra:replays";

	public const string Threshold = "extradown:threshold";

	public const string Channels = "extra:channels";

	public const string Coverage = "extra:coverage";

	public static readonly IReadOnlyList<ExpandedEffect> Entries = Build().ToArray();

	private static ExpandedEffect E(string card, string key, string kind, string target, int native, string family, string es, string en, string extraEs, string extraEn, int upgraded = -1, int sites = 1, string method = "OnPlay")
	{
		return new ExpandedEffect
		{
			Card = card,
			Attribute = key,
			Kind = kind,
			Target = target,
			Native = native,
			Upgraded = ((upgraded < 0) ? native : upgraded),
			Family = family,
			Spanish = es,
			English = en,
			ExtraSpanish = extraEs,
			ExtraEnglish = extraEn,
			Sites = sites,
			Method = method
		};
	}

	private static IEnumerable<ExpandedEffect> Build()
	{
		yield return E("TrashToTreasure", "extra:statusOrbs", "power", "TrashToTreasurePower", 1, "persistentOrb", "Orbes por Estado", "Orbs per Status", "Invoca {0} orbes adicionales por Estado.", "Channel {0} additional orbs per Status.");
		yield return E("ShadowStep", "extra:shadowTurns", "power", "ShadowStepPower", 1, "duration", "Turnos de daño potenciado", "Boosted damage turns", "El daño potenciado dura {0} turnos adicionales.", "Boosted damage lasts {0} additional turns.");
		yield return E("ShadowStep", "extra:shadowMultiplier", "capture", "ShadowStepPower", 2, "shadowMultiplier", "Multiplicador de daño", "Damage multiplier", "El multiplicador de daño aumenta en {0}.", "The damage multiplier increases by {0}.");
		yield return E("Enlightenment", "extradown:targetCost", "targetCost", null, 1, "cost", "Coste objetivo", "Target cost", "El coste objetivo se reduce en {0}.", "The target cost decreases by {0}.", -1, 2);
		yield return E("HammerTime", "extra:sharedForge", "capture", "HammerTimePower", 4, "sharedForge", "Forja compartida (%)", "Shared Forge (%)", "", "");
		yield return E("Barricade", "extra:blockGrowth", "capture", "BarricadePower", 0, "blockGrowth", "Crecimiento de Bloqueo (%)", "Block growth (%)", "", "");
		yield return E("Outbreak", "extra:explosionDamage", "power", "OutbreakPower", 11, "power", "Daño de explosión", "Explosion damage", "Cada explosión inflige {0} daño adicional.", "Each explosion deals {0} additional damage.", 15);
		yield return E("Outbreak", "extradown:poisonThreshold", "capture", "OutbreakPower", 3, "threshold", "Aplicaciones de Veneno necesarias", "Required Poison applications", "La explosión requiere {0} aplicaciones menos de Veneno.", "The explosion needs {0} fewer Poison applications.");
		yield return E("Scare", "extra:scareWeak", "power", "WeakPower", 1, "power", "Débil", "Weak", "Aplica {0} Débil adicional a todos los enemigos.", "Apply {0} additional Weak to all enemies.");
		yield return E("Guards", "extra:generatedBlock", "generatedStats", null, 0, "generatedStats", "Bloqueo adicional de sacrificios", "Additional sacrifice Block", "Los sacrificios generan {0} Bloqueo adicional.", "Sacrifices gain {0} additional Block.");
		yield return E("PrimalForce", "extra:generatedDamage", "generatedStats", null, 0, "generatedStats", "Daño adicional de Rocas", "Additional Rock damage", "Las Rocas infligen {0} daño adicional.", "Rocks deal {0} additional damage.");
		yield return E("CalculatedGamble", "extra:gambleDraw", "afterPlay", null, 0, "draw", "Robo adicional", "Additional draw", "Después, roba {0} cartas adicionales.", "Then draw {0} additional cards.");
		yield return E("BulletTime", "extra:allowedDraw", "capture", "NoDrawPower", 0, "draw", "Cartas que puedes robar después", "Cards you may draw afterwards", "Puedes robar {0} cartas antes de bloquear el robo.", "You may draw {0} cards before drawing is blocked.");
		yield return E("Apotheosis", "extra:permanentUpgrades", "afterPlay", null, 0, "permanentUpgrade", "Mejoras permanentes elegidas", "Chosen permanent Upgrades", "Mejora permanentemente hasta {0} cartas de tu mazo.", "Permanently Upgrade up to {0} cards in your deck.");
		yield return E("Anointed", "extra:rareDiscount", "afterPlay", null, 0, "groupResource", "Rebaja de raras recuperadas", "Retrieved Rare discount", "Las raras recuperadas cuestan {0} energía menos este turno.", "Retrieved Rares cost {0} less Energy this turn.");
		yield return E("Scrawl", "extra:redraw", "redraw", null, 0, "selection", "Cartas robadas que puedes cambiar", "Drawn cards you may replace", "Puedes descartar hasta {0} cartas robadas y robar otras tantas.", "You may discard up to {0} drawn cards and draw that many cards.");
		yield return E("MasterPlanner", "extra:initialSly", "afterPlay", null, 0, "selection", "Habilidades con Astucia inmediata", "Skills with immediate Sly", "Da Astucia a hasta {0} Habilidades de tu mano.", "Give Sly to up to {0} Skills in your hand.");
		yield return E("WellLaidPlans", "extra:planningDraw", "capture", "WellLaidPlansPower", 0, "persistentDraw", "Robo adicional por turno", "Additional draw per turn", "Roba {0} cartas adicionales al comenzar tu turno.", "Draw {0} additional cards at the start of your turn.");
		yield return E("GrandFinale", "extra:drawTolerance", "tolerance", null, 0, "threshold", "Cartas permitidas en el mazo", "Cards allowed in draw pile", "También se puede jugar con hasta {0} cartas en el mazo.", "Also playable with up to {0} cards in your draw pile.", -1, 1, "get_IsPlayable");
		yield return E("Clash", "extra:nonAttackTolerance", "tolerance", null, 0, "threshold", "Cartas que no son Ataques permitidas", "Non-Attack cards allowed", "También se puede jugar con hasta {0} cartas que no sean Ataques en la mano.", "Also playable with up to {0} non-Attack cards in your hand.", -1, 1, "get_IsPlayable");
		yield return E("EchoingSlash", "extra:killReplays", "killReplays", null, 1, "persistentRepeat", "Golpes adicionales por remate", "Additional hits per kill", "Cada remate añade {0} golpes adicionales a la cadena.", "Each kill adds {0} additional hits to the chain.");
		yield return E("Hang", "extra:hangSeed", "literal", null, 2, "multiplier", "Multiplicador inicial de daño", "Initial damage multiplier", "El multiplicador inicial aumenta en {0}, independientemente del crecimiento.", "The initial multiplier increases by {0}, independently of growth.");
		yield return E("Hang", "extra:hangGrowth", "hangGrowth", null, 2, "permanentGrowth", "Multiplicador de crecimiento del objetivo", "Target growth multiplier", "El multiplicador de crecimiento aumenta en {0}; el límite nativo del marcador se conserva.", "The growth multiplier increases by {0}; the native counter limit is preserved.");
		yield return E("Shadowmeld", "extra:power", "power", "ShadowmeldPower", 1, "persistentRepeat", "Duplicaciones de Bloqueo", "Block doublings", "Duplica tu Bloqueo {0} veces adicionales; las duplicaciones se multiplican.", "Double your Block {0} additional times; doublings multiply.");
		yield return E("Debilitate", "extra:debilitatePercent", "capture", "DebilitatePower", 100, "percentage", "Potencia adicional de Débil/Vulnerable (%)", "Additional Weak/Vulnerable potency (%)", "Aumenta la amplificación en {0} puntos porcentuales; Débil conserva al menos un 10 % de daño.", "Increase amplification by {0} percentage points; Weak retains at least 10% damage.");
		string[] array = new string[3] { "Bombardment", "HowlFromBeyond", "IAmInvincible" };
		foreach (string text in array)
		{
			yield return E(text, "extra:replays", (text == "IAmInvincible") ? "autoDrawReplays" : "autoReplays", null, 1, "persistentRepeat", "Jugadas por activación automática", "Plays per automatic trigger", "Se juega {0} veces adicionales cuando se activa automáticamente.", "Play {0} additional times when automatically triggered.", -1, 1, (text == "Bombardment") ? "AfterAutoPrePlayPhaseEnteredEarly" : "AfterAutoPostPlayPhaseEntered");
		}
		array = new string[2] { "Headbutt", "CosmicIndifference" };
		foreach (string card in array)
		{
			yield return E(card, "extra:selection", "putBack", null, 1, "selection", "Máximo de cartas recuperadas al mazo", "Maximum cards retrieved to draw pile", "Puedes poner hasta {0} cartas adicionales sobre el mazo.", "You may put up to {0} additional cards on top of your draw pile.");
		}
		yield return E("Rebound", "extra:returnCount", "power", "ReboundPower", 1, "selection", "Devoluciones este turno", "Returns this turn", "", "");
		yield return E("Nostalgia", "extra:returnCount", "power", "NostalgiaPower", 1, "persistentRepeat", "Devoluciones por turno", "Returns per turn", "", "");
		yield return E("ThinkingAhead", "extradown:putBack", "reduceHand", null, 1, "drawback", "Devoluciones obligatorias", "Required cards to put back", "Devuelve {0} cartas menos al mazo.", "Put {0} fewer cards back in your draw pile.");
		array = new string[4] { "Acrobatics", "DaggerThrow", "Survivor", "Prepared" };
		foreach (string text2 in array)
		{
			yield return E(text2, "extradown:discard", "reduceDiscard", null, 1, "drawback", "Descartes obligatorios", "Required discards", "Descarta {0} cartas menos.", "Discard {0} fewer cards.", (!(text2 == "Prepared")) ? 1 : 2);
		}
		yield return E("HiddenDaggers", "vardown:Cards", "variableSelection", null, 1, "drawback", "Descartes obligatorios", "Required discards", "", "");
		array = new string[2] { "Glimmer", "PhotonCut" };
		foreach (string card2 in array)
		{
			yield return E(card2, "vardown:PutBack", "variableSelection", null, 1, "drawback", "Devoluciones obligatorias", "Required cards to put back", "", "");
		}
		yield return E("SculptingStrike", "extra:selection", "keywordHand", null, 1, "selection", "Máximo de cartas con Etéreo", "Maximum Ethereal cards", "Puedes dar Etéreo a hasta {0} cartas adicionales.", "You may give Ethereal to up to {0} additional cards.");
		yield return E("Begone", "extra:selection", "transformHand", null, 1, "generation", "Máximo de transformaciones", "Maximum transformations", "Puedes transformar hasta {0} cartas adicionales.", "You may transform up to {0} additional cards.");
		yield return E("Cinder", "extradown:randomExhaust", "randomExhaust", null, 1, "drawback", "Agotamientos aleatorios", "Random Exhausts", "Agota {0} cartas aleatorias menos.", "Exhaust {0} fewer random cards.");
		array = new string[5] { "Brand", "BurningPact", "Scavenge", "TrueGrit", "Cleanse" };
		foreach (string text3 in array)
		{
			yield return E(text3, "extra:exhaustMaximum", (text3 == "Cleanse") ? "exhaustPile" : ((text3 == "TrueGrit") ? "mixedExhaust" : "exhaustHand"), null, 1, "selection", "Máximo de agotamientos elegidos", "Maximum chosen Exhausts", "Puedes Agotar hasta {0} cartas adicionales; el resto del efecto ocurre una vez.", "You may Exhaust up to {0} additional cards; the rest of the effect occurs once.", -1, (!(text3 == "TrueGrit")) ? 1 : 3);
		}
		array = new string[3] { "Colossus", "Tank", "BeaconOfHope" };
		foreach (string text4 in array)
		{
			yield return E(text4, "extra:protectionPercent", "capture", text4 + "Power", 50, "protection", (text4 == "BeaconOfHope") ? "Bloqueo compartido (%)" : ((text4 == "Tank") ? "Protección de aliados (%)" : "Reducción de daño (%)"), (text4 == "BeaconOfHope") ? "Shared Block (%)" : ((text4 == "Tank") ? "Ally protection (%)" : "Damage reduction (%)"), "El porcentaje aumenta en {0} puntos; máximo 90 %.", "The percentage increases by {0} points; maximum 90%.");
		}
		array = new string[2] { "Conqueror", "Unmovable" };
		foreach (string text5 in array)
		{
			yield return E(text5, "extra:conditionalMultiplier", "capture", text5 + "Power", 2, "multiplier", (text5 == "Conqueror") ? "Multiplicador de la Espada soberana" : "Multiplicador de Bloqueo", (text5 == "Conqueror") ? "Sovereign Blade multiplier" : "Block multiplier", "El multiplicador del efecto aumenta en {0}.", "The effect multiplier increases by {0}.");
		}
		yield return E("Soulbound", "extra:power", "power", "SoulboundPower", 1, "persistentGeneration", "Almas para el aliado", "Souls for the ally", "Genera {0} Almas adicionales para el aliado por activación.", "Generate {0} additional Souls for the ally per trigger.");
		array = new string[2] { "Midnight", "RocketPunch" };
		foreach (string text6 in array)
		{
			yield return E(text6, "extra:discount", "discount", null, 1, "persistentEnergy", "Rebaja por activación", "Discount per trigger", "Cada activación reduce el coste en {0} más.", "Each trigger reduces the cost by {0} more.", -1, 1, (text6 == "Midnight") ? "ReduceCostBy" : "AfterCardGeneratedForCombat");
		}
		array = new string[2] { "CrimsonMantle", "Inferno" };
		foreach (string text7 in array)
		{
			yield return E(text7, "extradown:selfDamage", "selfIncrement", null, 1, "drawback", "Incremento de daño propio", "Self-damage increment", (text7 == "Inferno") ? "El daño propio aumenta en {0} menos por aplicación; conserva al menos 1." : "El daño propio aumenta en {0} menos por aplicación.", (text7 == "Inferno") ? "Self-damage increases by {0} less per application; retains a minimum of 1." : "Self-damage increases by {0} less per application.");
		}
		yield return E("Regret", "extradown:regretPercent", "selfPercent", null, 100, "percentage", "Daño propio por carta (%)", "Self-damage per card (%)", "Reduce el daño propio en {0} puntos porcentuales.", "Reduce self-damage by {0} percentage points.", -1, 1, "OnTurnEndInHand");
		yield return E("Hellraiser", "extra:replays", "capture", "HellraiserPower", 1, "persistentRepeat", "Jugadas por Ataque automático", "Plays per automatic Attack", "Cada Ataque automático se juega {0} veces adicionales.", "Each automatic Attack is played {0} additional times.");
		yield return E("Eidolon", "extra:replays", "eidolon", null, 1, "persistentRepeat", "Potencia del efecto especial", "Special effect strength", "", "");
		yield return E("KnifeTrap", "extra:replays", "autoReplays", null, 1, "persistentRepeat", "Jugadas por Shiv", "Plays per Shiv", "Cada Shiv se juega {0} veces adicionales.", "Each Shiv is played {0} additional times.");
		yield return E("Uproar", "extra:power", "attackAutoPlay", null, 1, "autoPlay", "Ataques jugados del mazo", "Attacks played from draw pile", "Juega {0} Ataques adicionales del mazo.", "Play {0} additional Attacks from the draw pile.");
		yield return E("Misery", "extra:multiplier", "spreadDebuffs", null, 1, "multiplier", "Multiplicador de perjuicios propagados", "Spread debuff multiplier", "Propaga {0} veces más la cantidad de cada perjuicio.", "Spread {0} additional times the amount of each debuff.", -1, 2);
		yield return E("Reflect", "extra:reflectionPercent", "capture", "ReflectPower", 100, "percentage", "Daño reflejado (%)", "Reflected damage (%)", "El daño reflejado aumenta en {0} puntos porcentuales.", "Reflected damage increases by {0} percentage points.");
		yield return E("Underworld", "extra:power", "power", "UnderworldPower", 1, "multiplier", "Conversión de daño a Condenación", "Damage-to-Doom multiplier", "Aplica {0} veces más Condenación por el mismo daño.", "Apply {0} additional times the Doom for the same damage.");
		yield return E("Omnislice", "extra:splashPercent", "splashPercent", null, 100, "percentage", "Daño propagado (%)", "Splash damage (%)", "El daño propagado aumenta en {0} puntos porcentuales.", "Splash damage increases by {0} percentage points.");
		yield return E("Spinner", "extra:channels", "initialOrb", "GlassOrb", 0, "orb", "Cristales iniciales", "Initial Glass Orbs", "Invoca {0} Cristales adicionales al jugarla.", "Channel {0} additional Glass Orbs when played.", 1, 2);
		yield return E("Normality", "extra:normalityLimit", "literal", null, 3, "drawback", "Máximo de cartas por turno", "Cards per turn limit", "El límite de cartas por turno aumenta en {0}.", "The cards-per-turn limit increases by {0}.", -1, 1, "get_ShouldPreventCardPlay");
		array = new string[2] { "Guilty", "Dowsing" };
		foreach (string c in array)
		{
			string[] array2 = ((!(c == "Guilty")) ? new string[2] { "BeforeRoomEntered", "set_RoomsEntered" } : new string[2] { "AfterCombatEnd", "set_CombatsSeen" });
			string[] array3 = array2;
			foreach (string method in array3)
			{
				yield return E(c, "extradown:quest", "literal", null, 5, "threshold", (c == "Guilty") ? "Combates totales hasta desaparecer" : "Salas totales de la misión", (c == "Guilty") ? "Total combats until removal" : "Total quest rooms", "La misión requiere {0} pasos menos.", "The quest requires {0} fewer steps.", -1, 1, method);
			}
		}
		array = new string[4] { "Discovery", "Quasar", "Splash", "Abundance" };
		foreach (string card3 in array)
		{
			yield return E(card3, "extra:selection", "chooseGenerated", null, 1, "generation", "Cartas elegidas", "Selected cards", "Puedes elegir {0} cartas adicionales entre las ofrecidas.", "Choose {0} additional cards from the offered choices.");
		}
		yield return E("ForbiddenGrimoire", "extra:power", "power", "ForbiddenGrimoirePower", 1, "deckRemoval", "Eliminaciones por combate", "Removals per combat", "Obtén {0} eliminaciones de carta adicionales tras cada combate.", "Gain {0} additional card removals after each combat.");
		yield return E("TheHunt", "extra:generation", "huntRewards", null, 1, "generation", "Recompensas al rematar", "Rewards on Fatal", "Al rematar, obtén {0} recompensas de cartas adicionales.", "On Fatal, gain {0} additional card rewards.");
		yield return E("Fisticuffs", "extra:multiplier", "blockMultiplier", null, 1, "multiplier", "Bloqueo por daño", "Block per damage", "El multiplicador de Bloqueo aumenta en {0}.", "The Block multiplier increases by {0}.");
		yield return E("Prolong", "extra:multiplier", "power", "BlockNextTurnPower", 1, "groupResource", "Bloqueo conservado", "Preserved Block", "El multiplicador de Bloqueo conservado aumenta en {0}.", "The preserved Block multiplier increases by {0}.");
		yield return E("BlightStrike", "extra:multiplier", "power", "DoomPower", 1, "multiplier", "Destino funesto por daño", "Doom per damage", "El multiplicador de Destino funesto aumenta en {0}.", "The Doom multiplier increases by {0}.");
		array = new string[2] { "ReaperForm", "NecroMastery" };
		foreach (string text8 in array)
		{
			yield return E(text8, "extra:power", "power", text8 + "Power", 1, (text8 == "NecroMastery") ? "permanentGrowth" : "multiplier", (text8 == "NecroMastery") ? "Vida de Osty por PV perdido" : "Destino funesto por daño", (text8 == "NecroMastery") ? "Osty HP per HP lost" : "Doom per damage", "El factor de conversión aumenta en {0}.", "The conversion factor increases by {0}.");
		}
		yield return E("Reflect", "extra:power", "power", "ReflectPower", 1, "duration", "Turnos de reflejo", "Reflection turns", "Dura {0} turnos adicionales.", "Lasts {0} additional turns.");
		array = new string[4] { "KinglyKick", "Pinpoint", "Stomp", "UpMySleeve" };
		foreach (string text9 in array)
		{
			yield return E(text9, "extra:discount", "discount", null, 1, "persistentEnergy", "Reducción de coste por activación", "Cost discount per trigger", "Cada reducción de coste aumenta en {0}.", "Each cost discount increases by {0}.", -1, 1, (text9 == "KinglyKick") ? "AfterCardDrawn" : ((text9 == "UpMySleeve") ? "OnPlay" : "ReduceCostBy"));
		}
		yield return E("Modded", "extradown:surcharge", "surcharge", null, 1, "persistentEnergy", "Sobrecoste al jugarla", "Cost surcharge when played", "El sobrecoste por jugada se reduce en {0}.", "The cost surcharge per play is reduced by {0}.");
		array = new string[6] { "BoostAway", "FightThrough", "GunkUp", "Overclock", "Turbo", "CollisionCourse" };
		foreach (string text10 in array)
		{
			yield return E(text10, "extradown:statuses", "statuses", null, (!(text10 == "FightThrough")) ? 1 : 2, "generation", "Estados creados", "Statuses created", "Crea {0} Estados menos.", "Create {0} fewer Statuses.");
		}
		array = new string[2] { "Quasar", "Splash" };
		foreach (string card4 in array)
		{
			yield return E(card4, "extra:options", "options", null, 3, "selection", "Opciones ofrecidas", "Offered choices", "Elige entre {0} cartas adicionales.", "Choose among {0} additional cards.");
		}
		yield return E("Alchemize", "extra:generation", "potions", null, 1, "generation", "Pociones creadas", "Potions created", "Crea {0} pociones adicionales si tienes espacio.", "Create {0} additional potions if you have space.");
		yield return E("Largesse", "extra:generation", "generate", null, 1, "generation", "Cartas incoloras creadas", "Colorless cards created", "Crea {0} cartas incoloras adicionales.", "Create {0} additional Colorless cards.");
		yield return E("Abundance", "extra:options", "options", null, 3, "selection", "Opciones ofrecidas", "Offered choices", "Elige entre {0} cartas adicionales.", "Choose among {0} additional cards.");
		yield return E("SeekerStrike", "extra:selection", "retrieve", null, 1, "selection", "Cartas elegidas", "Selected cards", "Puedes recuperar {0} cartas adicionales entre las mostradas.", "Retrieve {0} additional cards from the offered choices.");
		yield return E("TheHunt", "extra:options", "rewardChoices", null, 3, "selection", "Opciones de recompensa", "Reward choices", "Su recompensa ofrece {0} cartas adicionales.", "Its reward offers {0} additional cards.");
		yield return E("Tracking", "extra:power", "power", "TrackingPower", 2, "multiplier", "Multiplicador de daño", "Damage multiplier", "Cada aplicación añade {0} al multiplicador de daño.", "Each application adds {0} to the damage multiplier.", -1, 2);
		array = new string[2] { "Tyranny", "ToolsOfTheTrade" };
		foreach (string text11 in array)
		{
			yield return E(text11, "extra:draw", "capture", text11 + "Power", 1, "persistentDraw", "Robo adicional por turno", "Additional draw per turn", "Roba {0} cartas adicionales por turno, sin aumentar su coste de cartas.", "Draw {0} additional cards each turn without increasing its card cost.");
		}
		yield return E("Lethality", "extra:coverage", "capture", "LethalityPower", 1, "persistentRepeat", "Ataques potenciados por turno", "Boosted Attacks per turn", "Potencia los primeros {0} Ataques adicionales de cada turno.", "Boost the first {0} additional Attacks each turn.");
		yield return E("Juggling", "extradown:threshold", "capture", "JugglingPower", 3, "threshold", "Ataque que se copia", "Attack to copy", "Copia un Ataque {0} posiciones antes en cada turno.", "Copy an Attack {0} positions earlier each turn.");
		yield return E("Cacophony", "extradown:threshold", "capture", "CacophonyPower", 33, "threshold", "Cartas por activación", "Cards per trigger", "Umbral de robo reducido en {0}.", "Draw threshold reduced by {0}.");
		yield return E("Dominate", "var:StrengthPerVulnerable", "varMultiplier", "StrengthPower", 1, "multiplier", "Fuerza por Vulnerable", "Strength per Vulnerable", "", "");
		yield return E("Transfigure", "vardown:Energy", "varSurcharge", null, 1, "threshold", "Sobrecoste aplicado", "Applied cost surcharge", "", "");
		yield return E("Transfigure", "extra:replays", "grantReplays", null, 1, "persistentRepeat", "Repeticiones concedidas", "Granted replays", "Concede {0} repeticiones adicionales a cada carta elegida.", "Grant {0} additional replays to each selected card.");
		array = new string[4] { "EchoForm", "SignalBoost", "Burst", "OneTwoPunch" };
		foreach (string text12 in array)
		{
			yield return E(text12, "extra:replays", "capture", text12 + "Power", 1, "persistentRepeat", "Repeticiones por carta", "Replays per card", "Cada carta afectada se repite {0} veces más.", "Each affected card replays {0} additional times.");
		}
		yield return E("LightningRod", "extra:channels", "capture", "LightningRodPower", 1, "persistentOrb", "Relámpagos por turno", "Lightning per turn", "Invoca {0} Relámpagos adicionales por turno.", "Channel {0} additional Lightning Orbs per turn.");
		array = new string[3] { "Automation", "Orbit", "Panache" };
		foreach (string text13 in array)
		{
			yield return E(text13, "extradown:threshold", "capture", text13 + "Power", (text13 == "Automation") ? 10 : ((text13 == "Orbit") ? 4 : 5), "threshold", (text13 == "Orbit") ? "Energía por activación" : "Cartas por activación", (text13 == "Orbit") ? "Energy per trigger" : "Cards per trigger", "Umbral de activación reducido en {0}.", "Trigger threshold reduced by {0}.");
		}
		yield return E("RollingBoulder", "var:IncrementAmount", "capture", "RollingBoulderPower", 5, "permanentGrowth", "Crecimiento por turno", "Growth per turn", "", "");
		yield return E("DanseMacabre", "vardown:Energy", "capture", "DanseMacabrePower", 2, "threshold", "Coste mínimo para activar", "Minimum triggering cost", "", "");
		yield return E("PaleBlueDot", "vardown:CardPlay", "capture", "PaleBlueDotPower", 5, "threshold", "Cartas necesarias", "Cards required", "", "");
		yield return E("Demesne", "var:Energy", "capture", "DemesnePower", 1, "persistentEnergy", "Energía adicional por turno", "Additional Energy per turn", "", "");
		yield return E("Iteration", "extra:coverage", "capture", "IterationPower", 1, "persistentDraw", "Estados que activan robo por turno", "Statuses triggering draw per turn", "Pueden activar el robo {0} Estados más por turno.", "{0} additional Statuses can trigger draw each turn.");
		yield return E("PhantomBlades", "extra:coverage", "capture", "PhantomBladesPower", 1, "persistentRepeat", "Shivs potenciadas por turno", "Boosted Shivs per turn", "Potencia {0} Shivs adicionales por turno.", "Boost {0} additional Shivs each turn.");
		array = new string[2] { "Convergence", "Salvo" };
		foreach (string card5 in array)
		{
			yield return E(card5, "extra:power", "power", "RetainHandPower", 1, "duration", "Turnos de retención", "Retain turns", "Retén la mano durante {0} turnos adicionales.", "Retain your hand for {0} additional turns.");
		}
		string[] freeCards = new string[4] { "Pounce", "Synthesis", "Unrelenting", "Veilpiercer" };
		string[] freePowers = new string[4] { "FreeSkillPower", "FreePowerPower", "FreeAttackPower", "VeilpiercerPower" };
		for (int i = 0; i < freeCards.Length; i++)
		{
			yield return E(freeCards[i], "extra:power", "power", freePowers[i], 1, "persistentEnergy", "Cartas gratuitas", "Free cards", "Hace gratis {0} cartas adicionales del mismo tipo.", "Makes {0} additional cards of the same type free.");
		}
		array = new string[6] { "Hologram", "Graveblast", "Wish", "SecretWeapon", "SecretTechnique", "Tutor" };
		foreach (string card6 in array)
		{
			yield return E(card6, "extra:selection", "retrieve", null, 1, "selection", "Cartas recuperadas", "Cards retrieved", "Recupera {0} cartas adicionales.", "Retrieve {0} additional cards.");
		}
		yield return E("Armaments", "extra:selection", "upgradeHand", null, 1, "selection", "Cartas mejoradas", "Cards upgraded", "Mejora {0} cartas adicionales.", "Upgrade {0} additional cards.");
		array = new string[4] { "WhiteNoise", "Distraction", "InfernalBlade", "ManifestAuthority" };
		foreach (string card7 in array)
		{
			yield return E(card7, "extra:generation", "generate", null, 1, "generation", "Cartas generadas", "Cards generated", "Genera {0} cartas adicionales con el mismo efecto.", "Generate {0} additional cards with the same effect.");
		}
		yield return E("Severance", "extra:generation", "souls", null, 1, "groupResource", "Almas por pila", "Souls per pile", "Añade {0} Almas adicionales a cada pila.", "Add {0} additional Souls to each pile.", -1, 3);
		yield return E("StormOfSteel", "extra:multiplier", "shivMultiplier", null, 1, "multiplier", "Shivs por descarte", "Shivs per discard", "Crea {0} Shivs adicionales por carta descartada.", "Create {0} additional Shivs per discarded card.");
		yield return E("Stoke", "extra:multiplier", "generationMultiplier", null, 1, "multiplier", "Cartas por agotamiento", "Cards per Exhaust", "Crea {0} cartas adicionales por carta agotada.", "Create {0} additional cards per Exhausted card.");
		array = new string[3] { "Snap", "HandTrick", "Transfigure" };
		foreach (string text14 in array)
		{
			yield return E(text14, "extra:selection", (text14 == "Transfigure") ? "selectHand" : "keywordHand", null, 1, "selection", "Cartas afectadas", "Cards affected", "Afecta a {0} cartas adicionales.", "Affect {0} additional cards.");
		}
		array = new string[8] { "AstralPulse", "DaggerSpray", "Maul", "Refract", "RipAndTear", "Thrash", "TwinStrike", "Uproar" };
		foreach (string card8 in array)
		{
			yield return E(card8, "extra:hits", "hits", null, 2, "repeat", "Golpes", "Hits", "Golpea {0} veces más.", "Hits {0} additional times.");
		}
		yield return E("Dismantle", "extra:hits", "conditionalHits", null, 2, "repeat", "Golpes con Vulnerable", "Hits with Vulnerable", "Con Vulnerable, golpea {0} veces más.", "With Vulnerable, hits {0} additional times.");
		array = new string[5] { "Eradicate", "Skewer", "Stardust", "Volley", "Whirlwind" };
		foreach (string card9 in array)
		{
			yield return E(card9, "extra:x", "x", null, 0, "repeat", "Golpes adicionales a X", "Additional hits to X", "Golpea {0} veces más.", "Hits {0} additional times.");
		}
		yield return E("Cascade", "extra:x", "x", null, 0, "autoPlay", "Cartas adicionales a X", "Additional cards to X", "Juega {0} cartas adicionales.", "Play {0} additional cards.");
		yield return E("Dirge", "extra:x", "x", null, 0, "groupResource", "Repeticiones adicionales a X", "Additional repetitions to X", "Repite la invocación y genera Almas {0} veces más.", "Repeat the summon and generate Souls {0} additional times.");
		yield return E("Malaise", "extra:x", "x", null, 0, "power", "Débil y pérdida de Fuerza adicionales a X", "Additional Weak and Strength loss to X", "Aplica {0} Débil adicional y reduce la Fuerza en {0} más.", "Apply {0} additional Weak and reduce Strength by {0} more.");
		// Combined build: the Fool's cards that hard-code their amounts.
		yield return E("LockedIn", "extra:selection", "retrieve", null, 1, "selection", "Cartas de Disparo recuperadas", "Shoot cards retrieved", "Recupera {0} cartas de Disparo adicionales.", "Retrieve {0} additional Shoot cards.");
		yield return E("Recall", "extra:selection", "retrieveTop", null, 1, "selection", "Cartas puestas encima del mazo", "Cards put on top of your draw pile", "Pon {0} cartas adicionales encima del mazo.", "Put {0} additional cards on top of your draw pile.");
		yield return E("BlotOutTheSky", "extra:generation", "generate", null, 1, "generation", "Cartas de Disparo creadas", "Shoot cards created", "Crea {0} cartas de Disparo gratis adicionales.", "Create {0} additional free Shoot cards.");
		yield return E("MythicForm", "extra:power", "power", "MythicFormPower", 1, "persistentGeneration", "Cartas por turno", "Cards per turn", "Añade {0} cartas adicionales cada turno.", "Add {0} additional cards each turn.");
		yield return E("Florescence", "extra:shotDamage", "shotDamage", null, 10, "damage", "Daño por carta agotada", "Damage per exhausted card", "The Shot inflige {0} daño adicional por carta agotada.", "The Shot deals {0} additional damage per exhausted card.", 13);
		yield return E("DaggerThrow", "extra:draw", "draw", null, 1, "draw", "Robo", "Draw", "Roba {0} cartas adicionales.", "Draw {0} additional cards.");
		yield return E("EscapePlan", "extra:draw", "draw", null, 1, "draw", "Robo", "Draw", "Roba {0} cartas adicionales; solo comprueba la primera.", "Draw {0} additional cards; only check the first.");
		yield return E("Havoc", "extra:power", "autoPlay", null, 1, "autoPlay", "Cartas jugadas", "Cards played", "Juega y Agota {0} cartas adicionales.", "Play and Exhaust {0} additional cards.");
		yield return E("Discovery", "extra:options", "options", null, 3, "selection", "Opciones", "Options", "Elige entre {0} opciones más.", "Choose from {0} additional options.");
		yield return E("BladeOfInk", "extra:power", "enchant", "Inky", 1, "multiplier", "Débil de las Shivs", "Shiv Weak", "Las Shivs aplican {0} Débil adicional.", "The Shivs apply {0} additional Weak.");
		yield return E("Darkness", "extra:passive", "darkPassive", null, 1, "orbRepeat", "Activaciones de Oscuridad", "Dark activations", "Activa las pasivas de Oscuridad {0} veces más.", "Trigger Dark passives {0} additional times.", 2);
		yield return E("TeslaCoil", "extra:passive", "teslaPassive", null, 1, "orbRepeat", "Activaciones de Relámpago", "Lightning activations", "Activa cada Relámpago {0} veces más.", "Trigger each Lightning Orb {0} additional times.", 2);
		yield return E("DoubleEnergy", "extra:multiplier", "energyMultiplier", null, 2, "multiplier", "Multiplicador de energía", "Energy multiplier", "Añade {0} veces más tu energía actual.", "Gain {0} additional times your current Energy.");
		yield return E("Entrench", "extra:multiplier", "blockMultiplier", null, 2, "multiplier", "Multiplicador de Bloqueo", "Block multiplier", "Añade {0} veces más tu Bloqueo actual.", "Gain {0} additional times your current Block.");
		yield return E("MoltenFist", "extra:multiplier", "vulnerableMultiplier", "VulnerablePower", 2, "multiplier", "Multiplicador de Vulnerable", "Vulnerable multiplier", "Añade {0} veces más el Vulnerable actual.", "Apply {0} additional times the current Vulnerable.");
		string[] cards = new string[16]
		{
			"DarkEmbrace", "InfiniteBlades", "HelloWorld", "Calamity", "Mayhem", "Aggression", "Nightmare", "Subroutine", "TheSealedThrone", "Unmovable",
			"Stratagem", "Flanking", "Conqueror", "TagTeam", "Predator", "Juggling"
		};
		string[] targets = new string[16]
		{
			"DarkEmbracePower", "InfiniteBladesPower", "HelloWorldPower", "CalamityPower", "MayhemPower", "AggressionPower", "NightmarePower", "SubroutinePower", "TheSealedThronePower", "UnmovablePower",
			"StratagemPower", "FlankingPower", "ConquerorPower", "TagTeamPower", "DrawCardsNextTurnPower", "JugglingPower"
		};
		string[] families = new string[16]
		{
			"persistentDraw", "persistentGeneration", "persistentGeneration", "persistentGeneration", "persistentRepeat", "selection", "persistentRepeat", "persistentEnergy", "persistentEnergy", "persistentRepeat",
			"selection", "multiplier", "duration", "persistentRepeat", "draw", "persistentRepeat"
		};
		string[] es = new string[16]
		{
			"Robo por agotamiento", "Shivs por turno", "Cartas por turno", "Cartas por Ataque", "Jugadas por turno", "Recuperaciones por turno", "Copias al próximo turno", "Energía por Poder", "Estrellas por carta", "Ganancias de Bloqueo duplicadas",
			"Cartas por barajado", "Multiplicador de daño", "Duración", "Repeticiones del aliado", "Robo al próximo turno", "Copias del tercer ataque"
		};
		string[] en = new string[16]
		{
			"Draw per Exhaust", "Shivs per turn", "Cards per turn", "Cards per Attack", "Plays per turn", "Retrievals per turn", "Copies next turn", "Energy per Power", "Stars per card", "Doubled Block gains",
			"Cards per shuffle", "Damage multiplier", "Duration", "Ally replays", "Draw next turn", "Copies of the third Attack"
		};
		string[] extraEs = new string[16]
		{
			"Roba {0} cartas adicionales por agotamiento.", "Crea {0} Shivs adicionales por turno.", "Crea {0} cartas adicionales por turno.", "Crea {0} cartas adicionales por Ataque.", "Juega {0} cartas adicionales por turno.", "Recupera {0} Ataques adicionales por turno.", "Obtén {0} copias adicionales al próximo turno.", "Gana {0} energía adicional por Poder.", "Gana {0} Estrellas adicionales por carta.", "Duplica {0} ganancias adicionales de Bloqueo por turno.",
			"Recupera {0} cartas adicionales por barajado.", "El multiplicador de daño aumenta en {0}.", "Dura {0} turnos adicionales.", "El aliado repite su siguiente ataque {0} veces más.", "Roba {0} cartas adicionales al próximo turno.", "Añade {0} copias adicionales del tercer Ataque."
		};
		string[] extraEn = new string[16]
		{
			"Draw {0} additional cards per Exhaust.", "Create {0} additional Shivs per turn.", "Create {0} additional cards per turn.", "Create {0} additional cards per Attack.", "Play {0} additional cards per turn.", "Retrieve {0} additional Attacks per turn.", "Gain {0} additional copies next turn.", "Gain {0} additional Energy per Power.", "Gain {0} additional Stars per card.", "Double {0} additional Block gains per turn.",
			"Retrieve {0} additional cards per shuffle.", "The damage multiplier increases by {0}.", "Lasts {0} additional turns.", "The ally replays their next Attack {0} additional times.", "Draw {0} additional cards next turn.", "Add {0} additional copies of the third Attack."
		};
		for (int i = 0; i < cards.Length; i++)
		{
			yield return E(cards[i], "extra:power", "power", targets[i], (cards[i] == "Nightmare") ? 3 : ((!(cards[i] == "Flanking") && !(cards[i] == "Predator")) ? 1 : 2), families[i], es[i], en[i], extraEs[i], extraEn[i]);
		}
	}

	public static IEnumerable<ExpandedEffect> For(string card)
	{
		return Entries.Where((ExpandedEffect e) => e.Card == card);
	}

	public static ExpandedEffect Find(string card, string key)
	{
		return For(card).FirstOrDefault((ExpandedEffect e) => e.Attribute == BidirectionalEffects.Axis(card, key));
	}

	public static bool Known(string key)
	{
		if (key != null && (key.StartsWith("extra:", StringComparison.Ordinal) || key.StartsWith("extradown:", StringComparison.Ordinal)))
		{
			if (!Entries.Any((ExpandedEffect e) => e.Attribute == key))
			{
				return BidirectionalEffects.KnownExtra(key);
			}
			return true;
		}
		return false;
	}

	public static int Maximum(string key)
	{
		return key switch
		{
			"extra:sharedForge" => 85899345, 
			"extra:blockGrowth" => 214748364, 
			"extra:protectionPercent" => 100, 
			_ => int.MaxValue, 
		};
	}

	public static int Value(string card, string key, bool upgraded, CardMachineState state)
	{
		ExpandedEffect expandedEffect = Find(card, key) ?? throw new ArgumentException("Efecto incompatible: " + card + "/" + key);
		return checked((upgraded ? expandedEffect.Upgraded : expandedEffect.Native) + BidirectionalEffects.RawDelta(card, key, state));
	}
}
