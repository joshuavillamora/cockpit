using Godot;

// Read-only list of the default keys. Update the table when bindings change in the Input Map.
public partial class ControlsScreen : OverlayScreen {
	public const string ScenePath = "res://scenes/ui/screens/controls_screen.tscn";

	private static readonly string[][] Rows = {
		new[] { "", "PLAYER 1", "PLAYER 2" },
		new[] { "MOVE", "A/D", "LEFT/RIGHT" },
		new[] { "UP/DOWN", "W/S", "UP/DOWN" },
		new[] { "JUMP", "K/SPACE", "N" },
		new[] { "ATTACK", "J/E", "M OR /" },
		new[] { "PAUSE", "ESC/P", "ESC/P" },
		new[] { "BLOCK", "SOON", "SOON" },
		new[] { "ABILITY", "SOON", "SOON" },
		new[] { "FULLSCREEN", "F11", "F11" },
	};

	public override void _Ready() {
		var grid = GetNode<GridContainer>("%Grid");
		foreach (string[] row in Rows) {
			foreach (string cell in row) {
				grid.AddChild(new Label { Text = cell });
			}
		}

		var back = GetNode<Button>("%BackButton");
		back.Pressed += Close;
		back.GrabFocus();
	}
}
