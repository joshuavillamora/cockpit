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

		// Already playing if we came from the loading screen; full volume again after a match
		AudioManager.Instance?.PlayMusic(Music.Main);
		AudioManager.Instance?.SetMusicLevel(0f);
		GameManager.Instance.ClearSeries();   // a score kept for a rematch ends at the menu
	}

	public override void _Input(InputEvent @event) {
		if (@event.IsActionPressed("start_fight")) {
			GetTree().ChangeSceneToFile("res://scenes/gameplay/test_fight.tscn");
		}
	}

	private void OpenOverlay(string scenePath) {
		menu.Visible = false;
		OverlayScreen.Open(this, scenePath, () => {
			menu.Visible = true;
			playButton.GrabFocus();
		});
	}
}
