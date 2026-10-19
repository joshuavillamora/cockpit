using Godot;

public partial class MainMenu : Control {
	private Button playButton;
	private Control menu;

	public override void _Ready() {
		playButton = GetNode<Button>("%PlayButton");
		menu = GetNode<Control>("%Menu");

		playButton.Pressed += () => SceneFlow.Instance.GoTo(GameManager.CharacterSelectScene);
		GetNode<Button>("%SettingsButton").Pressed += () => OpenOverlay(SettingsScreen.ScenePath);
		GetNode<Button>("%QuitButton").Pressed += () => GetTree().Quit();

		playButton.GrabFocus();
	}

	private void OpenOverlay(string scenePath) {
		menu.Visible = false;
		OverlayScreen.Open(this, scenePath, () => {
			menu.Visible = true;
			playButton.GrabFocus();
		});
	}
}
