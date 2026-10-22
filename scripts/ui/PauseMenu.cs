using Godot;

// Esc / P toggles pause during a match. Runs while the tree is paused (process mode Always).
public partial class PauseMenu : CanvasLayer {
	private Control root;
	private Control panel;
	private Button resumeButton;
	private bool overlayOpen;

	public override void _Ready() {
		root = GetNode<Control>("%Root");
		panel = GetNode<Control>("%Panel");
		resumeButton = GetNode<Button>("%ResumeButton");

		resumeButton.Pressed += Resume;
		GetNode<Button>("%SettingsButton").Pressed += () => OpenOverlay(SettingsScreen.ScenePath);
		GetNode<Button>("%QuitButton").Pressed += () => GameManager.Instance.ReturnToMenu();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (overlayOpen || !@event.IsActionPressed("pause")) return;
		if (GameManager.Instance.MatchWinner != 0) return;   // the victory screen is showing
		GetViewport().SetInputAsHandled();
		if (root.Visible) Resume(); else Pause();
	}

	private void Pause() {
		GetTree().Paused = true;
		root.Visible = true;
		AudioManager.Instance?.PlaySfx(Sfx.PopupOpen);
		resumeButton.GrabFocus();
	}

	private void Resume() {
		root.Visible = false;
		GetTree().Paused = false;
		AudioManager.Instance?.PlaySfx(Sfx.PopupClose);
	}

	private void OpenOverlay(string scenePath) {
		overlayOpen = true;
		panel.Visible = false;
		OverlayScreen.Open(this, scenePath, () => {
			overlayOpen = false;
			panel.Visible = true;
			resumeButton.GrabFocus();
		});
	}
}
