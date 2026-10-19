using Godot;

// Shown on top of the (dimmed) match when someone wins. MatchController calls Present().
public partial class VictoryOverlay : CanvasLayer {
	private Control root;
	private Button rematchButton;

	public override void _Ready() {
		root = GetNode<Control>("%Root");
		rematchButton = GetNode<Button>("%RematchButton");

		rematchButton.Pressed += () => GameManager.Instance.Rematch();
		GetNode<Button>("%MenuButton").Pressed += () => GameManager.Instance.ReturnToMenu();
	}

	public void Present(int winner) {
		GetNode<Label>("%Winner").Text = $"PLAYER {winner} WINS";
		root.Visible = true;
		rematchButton.GrabFocus();
	}
}
