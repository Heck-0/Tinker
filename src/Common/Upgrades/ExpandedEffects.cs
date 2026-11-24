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

	private static ExpandedEffect E(string card, string key, string kind, string target, int native, string family, string en, string extraEn, int upgraded = -1, int sites = 1, string method = "OnPlay")
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
			English = en,
			ExtraEnglish = extraEn,
			Sites = sites,
			Method = method
		};
	}

	private static IEnumerable<ExpandedEffect> Build()
	{
		yield return E("TrashToTreasure", "extra:statusOrbs", "power", "TrashToTreasurePower", 1, "persistentOrb", "Orbs per Status", "Channel {0} additional orbs per Status.");
		yield return E("ShadowStep", "extra:shadowTurns", "power", "ShadowStepPower", 1, "duration", "Boosted damage turns", "Boosted damage lasts {0} additional turns.");
		yield return E("ShadowStep", "extra:shadowMultiplier", "capture", "ShadowStepPower", 2, "shadowMultiplier", "Damage multiplier", "The damage multiplier increases by {0}.");
		yield return E("Enlightenment", "extradown:targetCost", "targetCost", null, 1, "cost", "Target cost", "The target cost decreases by {0}.", -1, 2);
		yield return E("HammerTime", "extra:sharedForge", "capture", "HammerTimePower", 4, "sharedForge", "Shared Forge (%)", "");
		yield return E("Barricade", "extra:blockGrowth", "capture", "BarricadePower", 0, "blockGrowth", "Block growth (%)", "");
		yield return E("Outbreak", "extra:explosionDamage", "power", "OutbreakPower", 11, "power", "Explosion damage", "Each explosion deals {0} additional damage.", 15);
		yield return E("Outbreak", "extradown:poisonThreshold", "capture", "OutbreakPower", 3, "threshold", "Required Poison applications", "The explosion needs {0} fewer Poison applications.");
		yield return E("Scare", "extra:scareWeak", "power", "WeakPower", 1, "power", "Weak", "Apply {0} additional Weak to all enemies.");
		yield return E("Guards", "extra:generatedBlock", "generatedStats", null, 0, "generatedStats", "Additional sacrifice Block", "Sacrifices gain {0} additional Block.");
		yield return E("PrimalForce", "extra:generatedDamage", "generatedStats", null, 0, "generatedStats", "Additional Rock damage", "Rocks deal {0} additional damage.");
		yield return E("CalculatedGamble", "extra:gambleDraw", "afterPlay", null, 0, "draw", "Additional draw", "Then draw {0} additional cards.");
		yield return E("BulletTime", "extra:allowedDraw", "capture", "NoDrawPower", 0, "draw", "Cards you may draw afterwards", "You may draw {0} cards before drawing is blocked.");
		yield return E("Apotheosis", "extra:permanentUpgrades", "afterPlay", null, 0, "permanentUpgrade", "Chosen permanent Upgrades", "Permanently Upgrade up to {0} cards in your deck.");
		yield return E("Anointed", "extra:rareDiscount", "afterPlay", null, 0, "groupResource", "Retrieved Rare discount", "Retrieved Rares cost {0} less Energy this turn.");
		yield return E("Scrawl", "extra:redraw", "redraw", null, 0, "selection", "Drawn cards you may replace", "You may discard up to {0} drawn cards and draw that many cards.");
		yield return E("MasterPlanner", "extra:initialSly", "afterPlay", null, 0, "selection", "Skills with immediate Sly", "Give Sly to up to {0} Skills in your hand.");
		yield return E("WellLaidPlans", "extra:planningDraw", "capture", "WellLaidPlansPower", 0, "persistentDraw", "Additional draw per turn", "Draw {0} additional cards at the start of your turn.");
		yield return E("GrandFinale", "extra:drawTolerance", "tolerance", null, 0, "threshold", "Cards allowed in draw pile", "Also playable with up to {0} cards in your draw pile.", -1, 1, "get_IsPlayable");
		yield return E("Clash", "extra:nonAttackTolerance", "tolerance", null, 0, "threshold", "Non-Attack cards allowed", "Also playable with up to {0} non-Attack cards in your hand.", -1, 1, "get_IsPlayable");
		yield return E("EchoingSlash", "extra:killReplays", "killReplays", null, 1, "persistentRepeat", "Additional hits per kill", "Each kill adds {0} additional hits to the chain.");
		yield return E("Hang", "extra:hangSeed", "literal", null, 2, "multiplier", "Initial damage multiplier", "The initial multiplier increases by {0}, independently of growth.");
		yield return E("Hang", "extra:hangGrowth", "hangGrowth", null, 2, "permanentGrowth", "Target growth multiplier", "The growth multiplier increases by {0}; the native counter limit is preserved.");
		yield return E("Shadowmeld", "extra:power", "power", "ShadowmeldPower", 1, "persistentRepeat", "Block doublings", "Double your Block {0} additional times; doublings multiply.");
		yield return E("Debilitate", "extra:debilitatePercent", "capture", "DebilitatePower", 100, "percentage", "Additional Weak/Vulnerable potency (%)", "Increase amplification by {0} percentage points; Weak retains at least 10% damage.");
		string[] array = new string[3] { "Bombardment", "HowlFromBeyond", "IAmInvincible" };
		foreach (string text in array)
		{
			yield return E(text, "extra:replays", (text == "IAmInvincible") ? "autoDrawReplays" : "autoReplays", null, 1, "persistentRepeat", "Plays per automatic trigger", "Play {0} additional times when automatically triggered.", -1, 1, (text == "Bombardment") ? "AfterAutoPrePlayPhaseEnteredEarly" : "AfterAutoPostPlayPhaseEntered");
		}
		array = new string[2] { "Headbutt", "CosmicIndifference" };
		foreach (string card in array)
		{
			yield return E(card, "extra:selection", "putBack", null, 1, "selection", "Maximum cards retrieved to draw pile", "You may put up to {0} additional cards on top of your draw pile.");
		}
		yield return E("Rebound", "extra:returnCount", "power", "ReboundPower", 1, "selection", "Returns this turn", "");
		yield return E("Nostalgia", "extra:returnCount", "power", "NostalgiaPower", 1, "persistentRepeat", "Returns per turn", "");
		yield return E("ThinkingAhead", "extradown:putBack", "reduceHand", null, 1, "drawback", "Required cards to put back", "Put {0} fewer cards back in your draw pile.");
		array = new string[4] { "Acrobatics", "DaggerThrow", "Survivor", "Prepared" };
		foreach (string text2 in array)
		{
			yield return E(text2, "extradown:discard", "reduceDiscard", null, 1, "drawback", "Required discards", "Discard {0} fewer cards.", (!(text2 == "Prepared")) ? 1 : 2);
		}
		yield return E("HiddenDaggers", "vardown:Cards", "variableSelection", null, 1, "drawback", "Required discards", "");
		array = new string[2] { "Glimmer", "PhotonCut" };
		foreach (string card2 in array)
		{
			yield return E(card2, "vardown:PutBack", "variableSelection", null, 1, "drawback", "Required cards to put back", "");
		}
		yield return E("SculptingStrike", "extra:selection", "keywordHand", null, 1, "selection", "Maximum Ethereal cards", "You may give Ethereal to up to {0} additional cards.");
		yield return E("Begone", "extra:selection", "transformHand", null, 1, "generation", "Maximum transformations", "You may transform up to {0} additional cards.");
		yield return E("Cinder", "extradown:randomExhaust", "randomExhaust", null, 1, "drawback", "Random Exhausts", "Exhaust {0} fewer random cards.");
		array = new string[5] { "Brand", "BurningPact", "Scavenge", "TrueGrit", "Cleanse" };
		foreach (string text3 in array)
		{
			yield return E(text3, "extra:exhaustMaximum", (text3 == "Cleanse") ? "exhaustPile" : ((text3 == "TrueGrit") ? "mixedExhaust" : "exhaustHand"), null, 1, "selection", "Maximum chosen Exhausts", "You may Exhaust up to {0} additional cards; the rest of the effect occurs once.", -1, (!(text3 == "TrueGrit")) ? 1 : 3);
		}
		array = new string[3] { "Colossus", "Tank", "BeaconOfHope" };
		foreach (string text4 in array)
		{
			yield return E(text4, "extra:protectionPercent", "capture", text4 + "Power", 50, "protection", (text4 == "BeaconOfHope") ? "Shared Block (%)" : ((text4 == "Tank") ? "Ally protection (%)" : "Damage reduction (%)"), "The percentage increases by {0} points; maximum 90%.");
		}
		array = new string[2] { "Conqueror", "Unmovable" };
		foreach (string text5 in array)
		{
			yield return E(text5, "extra:conditionalMultiplier", "capture", text5 + "Power", 2, "multiplier", (text5 == "Conqueror") ? "Sovereign Blade multiplier" : "Block multiplier", "The effect multiplier increases by {0}.");
		}
		yield return E("Soulbound", "extra:power", "power", "SoulboundPower", 1, "persistentGeneration", "Souls for the ally", "Generate {0} additional Souls for the ally per trigger.");
		array = new string[2] { "Midnight", "RocketPunch" };
		foreach (string text6 in array)
		{
			yield return E(text6, "extra:discount", "discount", null, 1, "persistentEnergy", "Discount per trigger", "Each trigger reduces the cost by {0} more.", -1, 1, (text6 == "Midnight") ? "ReduceCostBy" : "AfterCardGeneratedForCombat");
		}
		array = new string[2] { "CrimsonMantle", "Inferno" };
		foreach (string text7 in array)
		{
			yield return E(text7, "extradown:selfDamage", "selfIncrement", null, 1, "drawback", "Self-damage increment", (text7 == "Inferno") ? "Self-damage increases by {0} less per application; retains a minimum of 1." : "Self-damage increases by {0} less per application.");
		}
		yield return E("Regret", "extradown:regretPercent", "selfPercent", null, 100, "percentage", "Self-damage per card (%)", "Reduce self-damage by {0} percentage points.", -1, 1, "OnTurnEndInHand");
		yield return E("Hellraiser", "extra:replays", "capture", "HellraiserPower", 1, "persistentRepeat", "Plays per automatic Attack", "Each automatic Attack is played {0} additional times.");
		yield return E("Eidolon", "extra:replays", "eidolon", null, 1, "persistentRepeat", "Special effect strength", "");
		yield return E("KnifeTrap", "extra:replays", "autoReplays", null, 1, "persistentRepeat", "Plays per Shiv", "Each Shiv is played {0} additional times.");
		yield return E("Uproar", "extra:power", "attackAutoPlay", null, 1, "autoPlay", "Attacks played from draw pile", "Play {0} additional Attacks from the draw pile.");
		yield return E("Misery", "extra:multiplier", "spreadDebuffs", null, 1, "multiplier", "Spread debuff multiplier", "Spread {0} additional times the amount of each debuff.", -1, 2);
		yield return E("Reflect", "extra:reflectionPercent", "capture", "ReflectPower", 100, "percentage", "Reflected damage (%)", "Reflected damage increases by {0} percentage points.");
		yield return E("Underworld", "extra:power", "power", "UnderworldPower", 1, "multiplier", "Damage-to-Doom multiplier", "Apply {0} additional times the Doom for the same damage.");
		yield return E("Omnislice", "extra:splashPercent", "splashPercent", null, 100, "percentage", "Splash damage (%)", "Splash damage increases by {0} percentage points.");
		yield return E("Spinner", "extra:channels", "initialOrb", "GlassOrb", 0, "orb", "Initial Glass Orbs", "Channel {0} additional Glass Orbs when played.", 1, 2);
		yield return E("Normality", "extra:normalityLimit", "literal", null, 3, "drawback", "Cards per turn limit", "The cards-per-turn limit increases by {0}.", -1, 1, "get_ShouldPreventCardPlay");
		array = new string[2] { "Guilty", "Dowsing" };
		foreach (string c in array)
		{
			string[] array2 = ((!(c == "Guilty")) ? new string[2] { "BeforeRoomEntered", "set_RoomsEntered" } : new string[2] { "AfterCombatEnd", "set_CombatsSeen" });
			string[] array3 = array2;
			foreach (string method in array3)
			{
				yield return E(c, "extradown:quest", "literal", null, 5, "threshold", (c == "Guilty") ? "Total combats until removal" : "Total quest rooms", "The quest requires {0} fewer steps.", -1, 1, method);
			}
		}
		array = new string[4] { "Discovery", "Quasar", "Splash", "Abundance" };
		foreach (string card3 in array)
		{
			yield return E(card3, "extra:selection", "chooseGenerated", null, 1, "generation", "Selected cards", "Choose {0} additional cards from the offered choices.");
		}
		yield return E("ForbiddenGrimoire", "extra:power", "power", "ForbiddenGrimoirePower", 1, "deckRemoval", "Removals per combat", "Gain {0} additional card removals after each combat.");
		yield return E("TheHunt", "extra:generation", "huntRewards", null, 1, "generation", "Rewards on Fatal", "On Fatal, gain {0} additional card rewards.");
		yield return E("Fisticuffs", "extra:multiplier", "blockMultiplier", null, 1, "multiplier", "Block per damage", "The Block multiplier increases by {0}.");
		yield return E("Prolong", "extra:multiplier", "power", "BlockNextTurnPower", 1, "groupResource", "Preserved Block", "The preserved Block multiplier increases by {0}.");
		yield return E("BlightStrike", "extra:multiplier", "power", "DoomPower", 1, "multiplier", "Doom per damage", "The Doom multiplier increases by {0}.");
		array = new string[2] { "ReaperForm", "NecroMastery" };
		foreach (string text8 in array)
		{
			yield return E(text8, "extra:power", "power", text8 + "Power", 1, (text8 == "NecroMastery") ? "permanentGrowth" : "multiplier", (text8 == "NecroMastery") ? "Osty HP per HP lost" : "Doom per damage", "The conversion factor increases by {0}.");
		}
		yield return E("Reflect", "extra:power", "power", "ReflectPower", 1, "duration", "Reflection turns", "Lasts {0} additional turns.");
		array = new string[4] { "KinglyKick", "Pinpoint", "Stomp", "UpMySleeve" };
		foreach (string text9 in array)
		{
			yield return E(text9, "extra:discount", "discount", null, 1, "persistentEnergy", "Cost discount per trigger", "Each cost discount increases by {0}.", -1, 1, (text9 == "KinglyKick") ? "AfterCardDrawn" : ((text9 == "UpMySleeve") ? "OnPlay" : "ReduceCostBy"));
		}
		yield return E("Modded", "extradown:surcharge", "surcharge", null, 1, "persistentEnergy", "Cost surcharge when played", "The cost surcharge per play is reduced by {0}.");
		array = new string[6] { "BoostAway", "FightThrough", "GunkUp", "Overclock", "Turbo", "CollisionCourse" };
		foreach (string text10 in array)
		{
			yield return E(text10, "extradown:statuses", "statuses", null, (!(text10 == "FightThrough")) ? 1 : 2, "generation", "Statuses created", "Create {0} fewer Statuses.");
		}
		array = new string[2] { "Quasar", "Splash" };
		foreach (string card4 in array)
		{
			yield return E(card4, "extra:options", "options", null, 3, "selection", "Offered choices", "Choose among {0} additional cards.");
		}
		yield return E("Alchemize", "extra:generation", "potions", null, 1, "generation", "Potions created", "Create {0} additional potions if you have space.");
		yield return E("Largesse", "extra:generation", "generate", null, 1, "generation", "Colorless cards created", "Create {0} additional Colorless cards.");
		yield return E("Abundance", "extra:options", "options", null, 3, "selection", "Offered choices", "Choose among {0} additional cards.");
		yield return E("SeekerStrike", "extra:selection", "retrieve", null, 1, "selection", "Selected cards", "Retrieve {0} additional cards from the offered choices.");
		yield return E("TheHunt", "extra:options", "rewardChoices", null, 3, "selection", "Reward choices", "Its reward offers {0} additional cards.");
		yield return E("Tracking", "extra:power", "power", "TrackingPower", 2, "multiplier", "Damage multiplier", "Each application adds {0} to the damage multiplier.", -1, 2);
		array = new string[2] { "Tyranny", "ToolsOfTheTrade" };
		foreach (string text11 in array)
		{
			yield return E(text11, "extra:draw", "capture", text11 + "Power", 1, "persistentDraw", "Additional draw per turn", "Draw {0} additional cards each turn without increasing its card cost.");
		}
		yield return E("Lethality", "extra:coverage", "capture", "LethalityPower", 1, "persistentRepeat", "Boosted Attacks per turn", "Boost the first {0} additional Attacks each turn.");
		yield return E("Juggling", "extradown:threshold", "capture", "JugglingPower", 3, "threshold", "Attack to copy", "Copy an Attack {0} positions earlier each turn.");
		yield return E("Cacophony", "extradown:threshold", "capture", "CacophonyPower", 33, "threshold", "Cards per trigger", "Draw threshold reduced by {0}.");
		yield return E("Dominate", "var:StrengthPerVulnerable", "varMultiplier", "StrengthPower", 1, "multiplier", "Strength per Vulnerable", "");
		yield return E("Transfigure", "vardown:Energy", "varSurcharge", null, 1, "threshold", "Applied cost surcharge", "");
		yield return E("Transfigure", "extra:replays", "grantReplays", null, 1, "persistentRepeat", "Granted replays", "Grant {0} additional replays to each selected card.");
		array = new string[4] { "EchoForm", "SignalBoost", "Burst", "OneTwoPunch" };
		foreach (string text12 in array)
		{
			yield return E(text12, "extra:replays", "capture", text12 + "Power", 1, "persistentRepeat", "Replays per card", "Each affected card replays {0} additional times.");
		}
		yield return E("LightningRod", "extra:channels", "capture", "LightningRodPower", 1, "persistentOrb", "Lightning per turn", "Channel {0} additional Lightning Orbs per turn.");
		array = new string[3] { "Automation", "Orbit", "Panache" };
		foreach (string text13 in array)
		{
			yield return E(text13, "extradown:threshold", "capture", text13 + "Power", (text13 == "Automation") ? 10 : ((text13 == "Orbit") ? 4 : 5), "threshold", (text13 == "Orbit") ? "Energy per trigger" : "Cards per trigger", "Trigger threshold reduced by {0}.");
		}
		yield return E("RollingBoulder", "var:IncrementAmount", "capture", "RollingBoulderPower", 5, "permanentGrowth", "Growth per turn", "");
		yield return E("DanseMacabre", "vardown:Energy", "capture", "DanseMacabrePower", 2, "threshold", "Minimum triggering cost", "");
		yield return E("PaleBlueDot", "vardown:CardPlay", "capture", "PaleBlueDotPower", 5, "threshold", "Cards required", "");
		yield return E("Demesne", "var:Energy", "capture", "DemesnePower", 1, "persistentEnergy", "Additional Energy per turn", "");
		yield return E("Iteration", "extra:coverage", "capture", "IterationPower", 1, "persistentDraw", "Statuses triggering draw per turn", "{0} additional Statuses can trigger draw each turn.");
		yield return E("PhantomBlades", "extra:coverage", "capture", "PhantomBladesPower", 1, "persistentRepeat", "Boosted Shivs per turn", "Boost {0} additional Shivs each turn.");
		array = new string[2] { "Convergence", "Salvo" };
		foreach (string card5 in array)
		{
			yield return E(card5, "extra:power", "power", "RetainHandPower", 1, "duration", "Retain turns", "Retain your hand for {0} additional turns.");
		}
		string[] freeCards = new string[4] { "Pounce", "Synthesis", "Unrelenting", "Veilpiercer" };
		string[] freePowers = new string[4] { "FreeSkillPower", "FreePowerPower", "FreeAttackPower", "VeilpiercerPower" };
		for (int i = 0; i < freeCards.Length; i++)
		{
			yield return E(freeCards[i], "extra:power", "power", freePowers[i], 1, "persistentEnergy", "Free cards", "Makes {0} additional cards of the same type free.");
		}
		array = new string[6] { "Hologram", "Graveblast", "Wish", "SecretWeapon", "SecretTechnique", "Tutor" };
		foreach (string card6 in array)
		{
			yield return E(card6, "extra:selection", "retrieve", null, 1, "selection", "Cards retrieved", "Retrieve {0} additional cards.");
		}
		yield return E("Armaments", "extra:selection", "upgradeHand", null, 1, "selection", "Cards upgraded", "Upgrade {0} additional cards.");
		array = new string[4] { "WhiteNoise", "Distraction", "InfernalBlade", "ManifestAuthority" };
		foreach (string card7 in array)
		{
			yield return E(card7, "extra:generation", "generate", null, 1, "generation", "Cards generated", "Generate {0} additional cards with the same effect.");
		}
		yield return E("Severance", "extra:generation", "souls", null, 1, "groupResource", "Souls per pile", "Add {0} additional Souls to each pile.", -1, 3);
		yield return E("StormOfSteel", "extra:multiplier", "shivMultiplier", null, 1, "multiplier", "Shivs per discard", "Create {0} additional Shivs per discarded card.");
		yield return E("Stoke", "extra:multiplier", "generationMultiplier", null, 1, "multiplier", "Cards per Exhaust", "Create {0} additional cards per Exhausted card.");
		array = new string[3] { "Snap", "HandTrick", "Transfigure" };
		foreach (string text14 in array)
		{
			yield return E(text14, "extra:selection", (text14 == "Transfigure") ? "selectHand" : "keywordHand", null, 1, "selection", "Cards affected", "Affect {0} additional cards.");
		}
		array = new string[8] { "AstralPulse", "DaggerSpray", "Maul", "Refract", "RipAndTear", "Thrash", "TwinStrike", "Uproar" };
		foreach (string card8 in array)
		{
			yield return E(card8, "extra:hits", "hits", null, 2, "repeat", "Hits", "Hits {0} additional times.");
		}
		yield return E("Dismantle", "extra:hits", "conditionalHits", null, 2, "repeat", "Hits with Vulnerable", "With Vulnerable, hits {0} additional times.");
		array = new string[5] { "Eradicate", "Skewer", "Stardust", "Volley", "Whirlwind" };
		foreach (string card9 in array)
		{
			yield return E(card9, "extra:x", "x", null, 0, "repeat", "Additional hits to X", "Hits {0} additional times.");
		}
		yield return E("Cascade", "extra:x", "x", null, 0, "autoPlay", "Additional cards to X", "Play {0} additional cards.");
		yield return E("Dirge", "extra:x", "x", null, 0, "groupResource", "Additional repetitions to X", "Repeat the summon and generate Souls {0} additional times.");
		yield return E("Malaise", "extra:x", "x", null, 0, "power", "Additional Weak and Strength loss to X", "Apply {0} additional Weak and reduce Strength by {0} more.");
		// Combined build: the Fool's cards that hard-code their amounts.
		yield return E("LockedIn", "extra:selection", "retrieve", null, 1, "selection", "Shoot cards retrieved", "Retrieve {0} additional Shoot cards.");
		yield return E("Recall", "extra:selection", "retrieveTop", null, 1, "selection", "Cards put on top of your draw pile", "Put {0} additional cards on top of your draw pile.");
		yield return E("BlotOutTheSky", "extra:generation", "generate", null, 1, "generation", "Shoot cards created", "Create {0} additional free Shoot cards.");
		yield return E("MythicForm", "extra:power", "power", "MythicFormPower", 1, "persistentGeneration", "Cards per turn", "Add {0} additional cards each turn.");
		yield return E("Florescence", "extra:shotDamage", "shotDamage", null, 10, "damage", "Damage per exhausted card", "The Shot deals {0} additional damage per exhausted card.", 13);
		yield return E("DaggerThrow", "extra:draw", "draw", null, 1, "draw", "Draw", "Draw {0} additional cards.");
		yield return E("EscapePlan", "extra:draw", "draw", null, 1, "draw", "Draw", "Draw {0} additional cards; only check the first.");
		yield return E("Havoc", "extra:power", "autoPlay", null, 1, "autoPlay", "Cards played", "Play and Exhaust {0} additional cards.");
		yield return E("Discovery", "extra:options", "options", null, 3, "selection", "Options", "Choose from {0} additional options.");
		yield return E("BladeOfInk", "extra:power", "enchant", "Inky", 1, "multiplier", "Shiv Weak", "The Shivs apply {0} additional Weak.");
		yield return E("Darkness", "extra:passive", "darkPassive", null, 1, "orbRepeat", "Dark activations", "Trigger Dark passives {0} additional times.", 2);
		yield return E("TeslaCoil", "extra:passive", "teslaPassive", null, 1, "orbRepeat", "Lightning activations", "Trigger each Lightning Orb {0} additional times.", 2);
		yield return E("DoubleEnergy", "extra:multiplier", "energyMultiplier", null, 2, "multiplier", "Energy multiplier", "Gain {0} additional times your current Energy.");
		yield return E("Entrench", "extra:multiplier", "blockMultiplier", null, 2, "multiplier", "Block multiplier", "Gain {0} additional times your current Block.");
		yield return E("MoltenFist", "extra:multiplier", "vulnerableMultiplier", "VulnerablePower", 2, "multiplier", "Vulnerable multiplier", "Apply {0} additional times the current Vulnerable.");
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
		string[] en = new string[16]
		{
			"Draw per Exhaust", "Shivs per turn", "Cards per turn", "Cards per Attack", "Plays per turn", "Retrievals per turn", "Copies next turn", "Energy per Power", "Stars per card", "Doubled Block gains",
			"Cards per shuffle", "Damage multiplier", "Duration", "Ally replays", "Draw next turn", "Copies of the third Attack"
		};
		string[] extraEn = new string[16]
		{
			"Draw {0} additional cards per Exhaust.", "Create {0} additional Shivs per turn.", "Create {0} additional cards per turn.", "Create {0} additional cards per Attack.", "Play {0} additional cards per turn.", "Retrieve {0} additional Attacks per turn.", "Gain {0} additional copies next turn.", "Gain {0} additional Energy per Power.", "Gain {0} additional Stars per card.", "Double {0} additional Block gains per turn.",
			"Retrieve {0} additional cards per shuffle.", "The damage multiplier increases by {0}.", "Lasts {0} additional turns.", "The ally replays their next Attack {0} additional times.", "Draw {0} additional cards next turn.", "Add {0} additional copies of the third Attack."
		};
		for (int i = 0; i < cards.Length; i++)
		{
			yield return E(cards[i], "extra:power", "power", targets[i], (cards[i] == "Nightmare") ? 3 : ((!(cards[i] == "Flanking") && !(cards[i] == "Predator")) ? 1 : 2), families[i], en[i], extraEn[i]);
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
