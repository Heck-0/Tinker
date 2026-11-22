using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace Tinker.Card;

internal static class GameCompat
{
	private static ConstructorInfo _rngCtor;

	private static MethodInfo _exhaust;

	private static MethodInfo _damage;

	private static MethodInfo _netId;

	public static Task Exhaust(PlayerChoiceContext context, CardModel card, bool causedByEthereal = false, bool skipVisuals = false)
	{
		if ((object)_exhaust == null)
		{
			_exhaust = AccessTools.Method(typeof(CardCmd), "Exhaust", new Type[4]
			{
				typeof(PlayerChoiceContext),
				typeof(CardModel),
				typeof(bool),
				typeof(bool)
			});
		}
		return (Task)_exhaust.Invoke(null, new object[4] { context, card, causedByEthereal, skipVisuals });
	}

	public static Rng SeededRng(Player player, ModelId id, ulong mixin)
	{
		if (_rngCtor == null)
		{
			_rngCtor = AccessTools.Constructor(typeof(Rng), new Type[3]
			{
				typeof(Player),
				typeof(ModelId),
				typeof(ulong)
			}) ?? AccessTools.Constructor(typeof(Rng), new Type[4]
			{
				typeof(Player),
				typeof(ModelId),
				typeof(uint),
				typeof(int)
			});
			if (_rngCtor == null)
			{
				throw new MissingMethodException("Rng(Player, ModelId, mixin)");
			}
		}
		object[] parameters = ((_rngCtor.GetParameters().Length != 3) ? new object[4]
		{
			player,
			id,
			(uint)(mixin ^ (mixin >> 32)),
			0
		} : new object[3] { player, id, mixin });
		return (Rng)_rngCtor.Invoke(parameters);
	}

	public static bool RegisterNetworkProperties(Type type)
	{
		Type type2 = AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache");
		MethodInfo methodInfo = ((type2 == null) ? null : AccessTools.Method(type2, "InjectTypeIntoCache", new Type[1] { typeof(Type) }));
		if (methodInfo == null)
		{
			return false;
		}
		methodInfo.Invoke(null, new object[1] { type });
		FieldInfo fieldInfo = AccessTools.Field(type2, "_netIdToPropertyNameMap");
		PropertyInfo propertyInfo = AccessTools.Property(type2, "NetIdBitSize");
		int num = (fieldInfo?.GetValue(null) as ICollection)?.Count ?? 0;
		if (num > 0 && propertyInfo != null)
		{
			int num2 = (int)Math.Ceiling(Math.Log(num, 2.0));
			if (num2 > (int)propertyInfo.GetValue(null))
			{
				propertyInfo.SetValue(null, num2);
			}
		}
		return true;
	}

	public static int NetIdForPropertyName(string name)
	{
		if (_netId == null)
		{
			_netId = (from t in new string[2] { "MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache", "MegaCrit.Sts2.Core.Saves.Runs.SavedPropertiesTypeCache" }.Select(AccessTools.TypeByName)
				where t != null
				select AccessTools.Method(t, "GetNetIdForPropertyName", new Type[1] { typeof(string) })).FirstOrDefault((MethodInfo m) => m != null);
			if (_netId == null)
			{
				throw new InvalidOperationException("No se encontró GetNetIdForPropertyName.");
			}
		}
		try
		{
			return (int)_netId.Invoke(null, new object[1] { name });
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			throw ex.InnerException;
		}
	}
}
