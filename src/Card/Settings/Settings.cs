using System;
using System.Collections.Generic;
using System.Linq;

namespace Tinker.Card;

internal static class Settings
{
	private static CardSettings _hostOverride;

	private static BalanceConfig _balance;

	public static CardSettings Local { get; } = CardSettings.Default();

	public static CardSettings HostOverride
	{
		get
		{
			return _hostOverride;
		}
		set
		{
			_hostOverride = value;
			Invalidate();
		}
	}

	public static CardSettings Active => _hostOverride ?? Local;

	public static BalanceConfig Balance => _balance ?? (_balance = Active.ApplyTo(BalanceFile.Fresh()));

	public static void LocalChanged()
	{
		Local.Sanitize();
		Local.Preset = Local.DetectPreset();
		Invalidate();
	}

	public static void Invalidate()
	{
		_balance = null;
	}
}
