namespace Tinker.Common;

public sealed class BidirectionalEffect
{
	public string Card;

	public string Canonical;

	public string Reverse;

	public string Spanish;

	public string English;

	public string Family = "selection";

	public string Up
	{
		get
		{
			if (AttributeDirection.SignOf(Canonical) <= 0)
			{
				return Reverse;
			}
			return Canonical;
		}
	}

	public string Down
	{
		get
		{
			if (AttributeDirection.SignOf(Canonical) >= 0)
			{
				return Reverse;
			}
			return Canonical;
		}
	}
}
