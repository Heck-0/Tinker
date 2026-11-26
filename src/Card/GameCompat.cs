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
	private static MethodInfo _exhaust;

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

	public static int NetIdForPropertyName(string name)
	{
		if (_netId == null)
		{
			_netId = (from t in new string[1] { "MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache" }.Select(AccessTools.TypeByName)
				where t != null
				select AccessTools.Method(t, "GetNetIdForPropertyName", new Type[1] { typeof(string) })).FirstOrDefault((MethodInfo m) => m != null);
			if (_netId == null)
			{
				throw new InvalidOperationException("GetNetIdForPropertyName not found.");
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
