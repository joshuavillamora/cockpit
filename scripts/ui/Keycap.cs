using Godot;

// One key drawn on the designer's keycap art, with the letter from the game font.
// Single letters use the 16x16 cap; longer names (SPACE, ESC, ENTER) use the wide cap, stretched to fit.
// The font has no arrow glyphs, so the arrow keys draw a small pixel triangle instead.
// Used by the controls screen, character select and the training lobby. Create with Keycap.Create("A").
public partial class Keycap : NinePatchRect {
	public const string Up = "UP";
	public const string Down = "DOWN";
	public const string Left = "LEFT";
	public const string Right = "RIGHT";

	private const string CapPath = "res://assets/ui/menus/ui_keycap.png";
	private const string WideCapPath = "res://assets/ui/menus/ui_keycap_wide.png";
	private const string ThemePath = "res://resources/ui_theme.tres";
	private const int Height = 16;
	private const int WideMinWidth = 40;
	private const int WidePadding = 10;     // space between the text and the rounded ends
	private static readonly Color TextColor = new(0.93f, 0.89f, 0.78f);

	private string key = "";

	public static Keycap Create(string key) {
		var cap = new Keycap { key = key, MouseFilter = MouseFilterEnum.Ignore };
		bool wide = key.Length > 1 && !IsArrow(key);
		cap.Texture = GD.Load<Texture2D>(wide ? WideCapPath : CapPath);
		cap.PatchMarginLeft = cap.PatchMarginRight = wide ? 6 : 0;

		int width = Height;
		if (wide) {
			Theme theme = GD.Load<Theme>(ThemePath);
			float textWidth = theme.DefaultFont.GetStringSize(key, HorizontalAlignment.Left, -1, theme.DefaultFontSize).X;
			width = Mathf.Max(WideMinWidth, Mathf.CeilToInt(textWidth) + WidePadding * 2);
		}
		cap.CustomMinimumSize = new Vector2(width, Height);
		cap.Size = cap.CustomMinimumSize;

		if (!IsArrow(key)) {
			var label = new Label {
				Text = key,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			label.AddThemeConstantOverride("outline_size", 0);
			label.AddThemeColorOverride("font_color", TextColor);
			label.SetAnchorsPreset(LayoutPreset.FullRect);
			label.OffsetTop = label.OffsetBottom = -1;   // the font sits 1 px low inside the cap
			cap.AddChild(label);
		}
		return cap;
	}

	public static bool IsArrow(string key) => key is Up or Down or Left or Right;

	public override void _Draw() {
		if (!IsArrow(key)) return;

		// A 7 px wide triangle centred on the cap, pointing the way of the key
		Vector2 c = Size / 2f;
		Vector2[] points = key switch {
			Up => new[] { c + new Vector2(0, -3), c + new Vector2(4, 2), c + new Vector2(-3, 2) },
			Down => new[] { c + new Vector2(-3, -2), c + new Vector2(4, -2), c + new Vector2(0, 3) },
			Left => new[] { c + new Vector2(-3, 0), c + new Vector2(2, -4), c + new Vector2(2, 3) },
			_ => new[] { c + new Vector2(3, 0), c + new Vector2(-2, -4), c + new Vector2(-2, 3) },
		};
		DrawColoredPolygon(points, TextColor);
	}
}
