using BaseLib.Patches.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Tinker.Relic;

// Keeping relic upgrade state correct across clones, resyncs and evolutions.

[HarmonyPatch(typeof(AbstractModel), "MutableClone")]
public static class CloneStatePatch
{
	public static void Postfix(AbstractModel __instance, AbstractModel __result)
	{
		if (__instance is RelicModel relicModel && __result is RelicModel relic && relicModel.IsMutable)
		{
			Store.Restore(relic, Store.For(relicModel).ToJson());
		}
	}
}

[HarmonyPatch(typeof(Player), "SyncWithSerializedPlayer")]
public static class PlayerResyncPatch
{
	public static void Postfix(Player __instance, SerializablePlayer player)
	{
		ExtendedSaveHandlers<Player, SerializablePlayer>.Load(player, __instance);
	}
}

[HarmonyPatch(typeof(RelicCmd), "Replace")]
public static class EvolutionPatch
{
	public static void Prefix(RelicModel original, RelicModel replace)
	{
		Evolution.Transfer(original, replace);
	}
}
