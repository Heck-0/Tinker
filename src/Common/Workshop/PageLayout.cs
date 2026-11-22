using System;

namespace Tinker.Common;

public readonly struct PageLayout
{
	public const float Width = 1920f;

	public const float Height = 1080f;

	public float Scale { get; }

	public float X { get; }

	public float Y { get; }

	public PageLayout(float width, float height)
	{
		Scale = Math.Max(0f, Math.Min(width / 1920f, height / 1080f));
		X = (width - 1920f * Scale) / 2f;
		Y = (height - 1080f * Scale) / 2f;
	}
}
