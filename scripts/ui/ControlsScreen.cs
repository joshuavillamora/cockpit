using Godot;

// Read-only list of the default keys, laid out like the designer's controls wireframe (frame 10):
// one panel per player, an action name on the left and its keys as keycaps on the right.
// Update the table when bindings change in the Input Map (the keys are not final yet).
public partial class ControlsScreen : OverlayScreen {
	public const string ScenePath = "res://scenes/ui/screens/controls_screen.tscn";

	private const float RowLeft = 16f;
	private const float RowTop = 36f;
	private const float RowPitch = 26f;
	private const float KeysLeft = 152f;
	private const float KeyGap = 4f;
	private static readonly Color SoonColor = new(0.6f, 0.58f, 0.52f);

	// Action name, P1 keys, P2 keys. null = not in the game yet ("SOON").
	// The training lobby shows the same rows (except PAUSE), so this is the one place to change keys.
	public static readonly (string Action, string[] P1, string[] P2)[] Rows = {
		("MOVE", new[] { "A", "D" }, new[] { Keycap.Left, Keycap.Right }),
		("UP/DOWN", new[] { "W", "S" }, new[] { Keycap.Up, Keycap.Down }),
		("JUMP", new[] { "K", "SPACE" }, new[] { "N" }),
		("ATTACK", new[] { "J", "E" }, new[] { "M", "/" }),
		("BLOCK", new[] { "H" }, new[] { "," }),
		("ABILITY", null, null),
		("PAUSE", new[] { "ESC", "P" }, new[] { "ESC", "P" }),
	};

	public override void _Ready() {
		BuildPanel(GetNode<Control>("%P1Panel"), "PLAYER 1", 0);
		BuildPanel(GetNode<Control>("%P2Panel"), "PLAYER 2", 1);

		var back = GetNode<Button>("%BackButton");
		back.Pressed += Close;
		back.GrabFocus();
	}

	private static void BuildPanel(Control panel, string heading, int player) {
		panel.AddChild(new Label {
			Text = heading,
			Position = new Vector2(RowLeft, 10),
			Size = new Vector2(panel.Size.X - RowLeft * 2, 16),
			HorizontalAlignment = HorizontalAlignment.Center,
		});

		for (int r = 0; r < Rows.Length; r++) {
			float y = RowTop + r * RowPitch;
			panel.AddChild(new Label {
				Text = Rows[r].Action,
				Position = new Vector2(RowLeft, y),
				Size = new Vector2(120, 16),
				VerticalAlignment = VerticalAlignment.Center,
			});

			string[] keys = player == 0 ? Rows[r].P1 : Rows[r].P2;
			if (keys == null) {
				var soon = new Label { Text = "SOON", Position = new Vector2(KeysLeft, y), Size = new Vector2(80, 16), VerticalAlignment = VerticalAlignment.Center };
				soon.AddThemeColorOverride("font_color", SoonColor);
				panel.AddChild(soon);
				continue;
			}

			float x = KeysLeft;
			foreach (string key in keys) {
				Keycap cap = Keycap.Create(key);
				cap.Position = new Vector2(x, y);
				panel.AddChild(cap);
				x += cap.Size.X + KeyGap;
			}
		}
	}
}
