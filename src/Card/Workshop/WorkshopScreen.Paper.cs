using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace Tinker.Card;

// The painted look: blue paper panels with gold outlines and the rolled-paper rollers (shaders).
internal sealed partial class WorkshopScreen
{
	private const string PanelShader = "\r\nshader_type canvas_item;\r\nuniform sampler2D paper : filter_linear, repeat_enable;\r\nuniform vec2 size = vec2(400.0, 400.0);\r\nuniform vec2 tex_px = vec2(200.0, 300.0);\r\nuniform float mean_lum = 0.75;\r\nuniform float radius = 16.0;\r\nuniform float roller_shadow = 0.0;\r\nuniform vec3 deep = vec3(0.090, 0.150, 0.340);\r\nuniform vec3 royal = vec3(0.215, 0.345, 0.660);\r\nuniform vec3 ink = vec3(0.055, 0.090, 0.200);\r\n\r\nfloat hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }\r\nfloat vnoise(vec2 p) {\r\n    vec2 i = floor(p);\r\n    vec2 f = fract(p);\r\n    vec2 u = f * f * (3.0 - 2.0 * f);\r\n    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);\r\n}\r\nfloat fbm(vec2 p) {\r\n    float v = 0.0;\r\n    float a = 0.5;\r\n    for (int i = 0; i < 5; i++) { v += a * vnoise(p); p *= 2.03; a *= 0.5; }\r\n    return v;\r\n}\r\n\r\nfloat rbox(vec2 p, vec2 b, float r) {\r\n    vec2 q = abs(p) - b + r;\r\n    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;\r\n}\r\n\r\nvoid fragment() {\r\n    vec2 p = UV * size;\r\n    float d = rbox(p - size * 0.5, size * 0.5, radius);\r\n    // The game's paper grain, mirrored so it has no seams.\r\n    vec2 t = p / tex_px;\r\n    vec2 m = 1.0 - abs(mod(t, 2.0) - 1.0);\r\n    float l = dot(texture(paper, m).rgb, vec3(0.299, 0.587, 0.114));\r\n    vec3 col = royal * (1.0 + (l - mean_lum) * 3.2);\r\n    // Hand-painted paper: big blotches, medium grain and fine horizontal fibres.\r\n    float mott = fbm(p / 170.0) - 0.5;\r\n    float mid = fbm(p / 38.0 + 7.3) - 0.5;\r\n    float fib = vnoise(vec2(p.x / 9.0, p.y / 1.7)) - 0.5;\r\n    col *= 1.0 + mott * 0.30 + mid * 0.14 + fib * 0.06;\r\n    // Soft vignette towards the edges, and a shadow under the rollers.\r\n    col = mix(deep, col, 0.45 + 0.55 * smoothstep(0.0, 70.0, -d));\r\n    if (roller_shadow > 0.0) {\r\n        float ys = min(p.y, size.y - p.y);\r\n        col *= mix(0.62, 1.0, smoothstep(roller_shadow * 0.8, roller_shadow * 1.9, ys));\r\n    }\r\n    col = mix(ink, col, smoothstep(3.0, 4.5, -d));\r\n    COLOR = vec4(col, 1.0 - smoothstep(-0.5, 0.5, d));\r\n}";

	private const string RollerShader = "\r\nshader_type canvas_item;\r\nuniform vec2 size = vec2(1420.0, 60.0);\r\nuniform vec3 blue = vec3(0.190, 0.310, 0.600);\r\nuniform vec3 ink = vec3(0.055, 0.090, 0.200);\r\nuniform float seed = 0.0;\r\n\r\nfloat hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }\r\nfloat vnoise(vec2 p) {\r\n    vec2 i = floor(p);\r\n    vec2 f = fract(p);\r\n    vec2 u = f * f * (3.0 - 2.0 * f);\r\n    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);\r\n}\r\nfloat fbm(vec2 p) {\r\n    float v = 0.0;\r\n    float a = 0.5;\r\n    for (int i = 0; i < 4; i++) { v += a * vnoise(p); p *= 2.03; a *= 0.5; }\r\n    return v;\r\n}\r\n\r\nvoid fragment() {\r\n    vec2 p = UV * size;\r\n    float s = p.x;\r\n    float R = size.y * 0.5 - 7.0;\r\n    // Thickness and axis wobble a little along the length: rolled by hand.\r\n    float r = R * (0.78 + 0.40 * fbm(vec2(s / 120.0 + seed, 1.3)));\r\n    // The ends, rolled less tightly, bulge a little more.\r\n    r *= 1.0 + 0.10 * (1.0 - smoothstep(0.0, 90.0, min(s, size.x - s)));\r\n    float c = size.y * 0.5 + 7.0 * (vnoise(vec2(s / 240.0 + seed, 7.1)) - 0.5);\r\n    float y = p.y - c;\r\n    // Seen a little from the left: the left face shows the spiral of the turns and the\r\n    // right one, facing away, only the paper's edge, a little uneven from rolling by hand.\r\n    float x0 = 26.0;\r\n    float x1 = size.x - 12.0 - 5.0 * vnoise(vec2(seed + 9.0, p.y / 7.0)) - 2.0 * vnoise(vec2(seed + 13.0, p.y / 2.5));\r\n    float body = max(abs(y) - r, max(x0 - s, s - x1));\r\n    float rx = r * 0.40;\r\n    vec2 e = vec2((s - x0) / rx, y / r);\r\n    float face = (length(e) - 1.0) * rx;\r\n    float d = min(body, face);\r\n    if (d > 1.0) discard;\r\n\r\n    float grain = 1.0 + (fbm(p / 26.0 + seed) - 0.5) * 0.26 + (vnoise(vec2(p.x / 8.0, p.y / 1.5)) - 0.5) * 0.07;\r\n    float v = clamp(y / r, -1.0, 1.0);\r\n    // Soft light from above, no specular shine: matte paper.\r\n    float shade = 0.60 + 0.32 * sqrt(1.0 - v * v) - 0.13 * v;\r\n    vec3 col = blue * shade * grain;\r\n    // The edge looks a little darker, like stacked paper.\r\n    col *= 1.0 - 0.18 * (1.0 - smoothstep(0.0, 7.0, x1 - s));\r\n    float edge = smoothstep(-2.6, -1.5, d);\r\n    if (face < 0.0) {\r\n        float q = length(e);\r\n        float a = atan(e.y, e.x) / 6.2831853;\r\n        float sp = fract(q * 3.6 - a + seed);\r\n        float line = 1.0 - smoothstep(0.02, 0.16, min(sp, 1.0 - sp));\r\n        col = blue * (1.05 + 0.15 * q) * grain;\r\n        col = mix(col, ink, line * 0.85 * smoothstep(0.12, 0.30, q));\r\n        col = mix(col, ink, 1.0 - smoothstep(0.08, 0.22, q));\r\n        edge = max(edge, smoothstep(-2.4, -1.3, face));\r\n    }\r\n    col = mix(col, ink, edge);\r\n    COLOR = vec4(col, 1.0 - smoothstep(0.0, 1.0, d));\r\n}";

	private static float _paperMean = 0.75f;

	private static ImageTexture _paperGrain;

	private static ImageTexture PaperGrain()
	{
		if (_paperGrain != null && GodotObject.IsInstanceValid(_paperGrain))
		{
			return _paperGrain;
		}
		try
		{
			Texture2D texture2D = ResourceLoader.Load<Texture2D>("res://images/atlases/ui_atlas.sprites/map/map_legend.tres", null, ResourceLoader.CacheMode.Reuse);
			Image image;
			Rect2I rect2I;
			if (texture2D is AtlasTexture atlasTexture)
			{
				image = atlasTexture.Atlas.GetImage();
				rect2I = new Rect2I((Vector2I)atlasTexture.Region.Position, (Vector2I)atlasTexture.Region.Size);
			}
			else
			{
				image = texture2D.GetImage();
				rect2I = new Rect2I(0, 0, image.GetWidth(), image.GetHeight());
			}
			if (image.IsCompressed())
			{
				image.Decompress();
			}
			Image region = image.GetRegion(new Rect2I(rect2I.Position + rect2I.Size / 4, rect2I.Size / 2));
			double num = 0.0;
			int num2 = 0;
			for (int i = 0; i < region.GetHeight(); i += 3)
			{
				for (int j = 0; j < region.GetWidth(); j += 3)
				{
					Color pixel = region.GetPixel(j, i);
					num += 0.299 * (double)pixel.R + 0.587 * (double)pixel.G + 0.114 * (double)pixel.B;
					num2++;
				}
			}
			_paperMean = ((num2 > 0) ? ((float)(num / (double)num2)) : 0.75f);
			_paperGrain = ImageTexture.CreateFromImage(region);
		}
		catch (Exception ex)
		{
			ModEntry.Warn("Could not read the map paper: " + ex.Message);
			Image image2 = Image.CreateEmpty(4, 4, useMipmaps: false, Image.Format.Rgba8);
			image2.Fill(new Color(0.75f, 0.75f, 0.75f));
			_paperGrain = ImageTexture.CreateFromImage(image2);
			_paperMean = 0.75f;
		}
		return _paperGrain;
	}

	internal static ColorRect BluePanel(Vector2 position, Vector2 size, float radius, float rollerShadow = 0f)
	{
		ImageTexture imageTexture = PaperGrain();
		ShaderMaterial shaderMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = PanelShader
			}
		};
		shaderMaterial.SetShaderParameter("paper", imageTexture);
		shaderMaterial.SetShaderParameter("size", size);
		shaderMaterial.SetShaderParameter("tex_px", new Vector2(imageTexture.GetWidth(), imageTexture.GetHeight()));
		shaderMaterial.SetShaderParameter("mean_lum", _paperMean);
		shaderMaterial.SetShaderParameter("radius", radius);
		shaderMaterial.SetShaderParameter("roller_shadow", rollerShadow);
		return new ColorRect
		{
			Material = shaderMaterial,
			Position = position,
			Size = size,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	internal static void GoldOutline(Control parent, Vector2 center, Vector2 size, float radius, float width, float gapHalfWidth = 0f)
	{
		Vector2 vector = size / 2f;
		Line2D line2D = new Line2D
		{
			Width = width,
			DefaultColor = Gold,
			JointMode = Line2D.LineJointMode.Round,
			BeginCapMode = Line2D.LineCapMode.Round,
			EndCapMode = Line2D.LineCapMode.Round,
			Position = center,
			Antialiased = true
		};
		line2D.AddPoint(new Vector2(gapHalfWidth, 0f - vector.Y));
		Vector2[] array = new Vector2[4]
		{
			new Vector2(vector.X - radius, 0f - vector.Y + radius),
			new Vector2(vector.X - radius, vector.Y - radius),
			new Vector2(0f - vector.X + radius, vector.Y - radius),
			new Vector2(0f - vector.X + radius, 0f - vector.Y + radius)
		};
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j <= 8; j++)
			{
				float s = Mathf.DegToRad(-90f + (float)i * 90f + (float)j * 90f / 8f);
				line2D.AddPoint(array[i] + new Vector2(Mathf.Cos(s), Mathf.Sin(s)) * radius);
			}
		}
		line2D.AddPoint(new Vector2(0f - gapHalfWidth, 0f - vector.Y));
		parent.AddChild(line2D, forceReadableName: false, Node.InternalMode.Disabled);
		if (!(gapHalfWidth <= 0f))
		{
			float[] array2 = new float[2]
			{
				gapHalfWidth,
				0f - gapHalfWidth
			};
			foreach (float x in array2)
			{
				Vector2 vector2 = center + new Vector2(x, 0f - vector.Y);
				parent.AddChild(new Polygon2D
				{
					Polygon = new Vector2[4]
					{
						vector2 + new Vector2(0f, -7f),
						vector2 + new Vector2(7f, 0f),
						vector2 + new Vector2(0f, 7f),
						vector2 + new Vector2(-7f, 0f)
					},
					Color = Gold
				}, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
	}

	private void Roller(Vector2 center, float length, float seed)
	{
		_canvas.AddChild(RollerRect(center, length, seed), forceReadableName: false, Node.InternalMode.Disabled);
	}

	internal static ColorRect RollerRect(Vector2 center, float length, float seed, float height = 58f)
	{
		ShaderMaterial shaderMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = RollerShader
			}
		};
		shaderMaterial.SetShaderParameter("size", new Vector2(length, height));
		shaderMaterial.SetShaderParameter("seed", seed);
		return new ColorRect
		{
			Material = shaderMaterial,
			Position = center - new Vector2(length / 2f, height / 2f),
			Size = new Vector2(length, height),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}
}
