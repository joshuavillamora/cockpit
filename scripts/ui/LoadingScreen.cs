using Godot;

// First screen: preloads the game's scenes and data in the background, then waits for "press start".
// When loading is done the rooster calls (BAKAWK!) and the music starts.
public partial class LoadingScreen : Control {
	private const float MinShowTime = 1.2f;
	private const float BlinkInterval = 0.5f;
	private const float MusicDelay = 1.2f;   // let the rooster call finish first

	private static readonly string[] Preload = {
		GameManager.MainMenuScene,
		GameManager.CharacterSelectScene,
		GameManager.MapSelectScene,
		GameManager.LobbyScene,
		"res://scenes/gameplay/training_dummy.tscn",
		GameManager.GameScene,
		SettingsScreen.ScenePath,
		ControlsScreen.ScenePath,
		"res://scenes/ui/screens/game_hud.tscn",
		"res://scenes/ui/screens/pause_menu.tscn",
		"res://scenes/ui/screens/victory_overlay.tscn",
		"res://resources/characters/banaba.tres",
		"res://resources/characters/paraokan.tres",
		"res://resources/characters/joloano.tres",
		"res://resources/characters/da_chik.tres",
		"res://resources/arenas/thepit_day.tres",
		"res://resources/arenas/thepit_night.tres",
		"res://resources/arenas/death_chamber.tres",
	};

	private TextureProgressBar bar;
	private Label status;
	private Label pressStart;
	private double elapsed;
	private double blink;
	private bool ready;
	private bool leaving;

	public override void _Ready() {
		bar = GetNode<TextureProgressBar>("%LoadingBar");
		status = GetNode<Label>("%StatusLabel");
		pressStart = GetNode<Label>("%PressStart");

		foreach (string path in Preload) {
			ResourceLoader.LoadThreadedRequest(path);
		}
	}

	public override void _Process(double delta) {
		elapsed += delta;

		if (!ready) {
			float progress = 0f;
			var info = new Godot.Collections.Array();
			foreach (string path in Preload) {
				var state = ResourceLoader.LoadThreadedGetStatus(path, info);
				progress += state == ResourceLoader.ThreadLoadStatus.InProgress ? info[0].AsSingle() : 1f;
			}
			progress /= Preload.Length;

			bar.Value = Mathf.MoveToward(bar.Value, progress * 100.0, delta * 120.0);
			if (progress >= 1f && bar.Value >= 100.0 && elapsed >= MinShowTime) {
				ready = true;
				bar.Visible = false;
				GetNode<Control>("%LoadingBarBack").Visible = false;
				GetNode<Control>("%LoadingBarFrame").Visible = false;
				status.Visible = false;
				pressStart.Visible = true;
				AudioManager.Instance?.PlaySfx(Sfx.Bakawk);
				StartMusicLater();
			}
			return;
		}

		blink += delta;
		pressStart.Visible = ((int)(blink / BlinkInterval)) % 2 == 0;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (!ready || leaving || @event.IsEcho() || !@event.IsPressed()) return;
		if (@event is InputEventKey || @event is InputEventJoypadButton) {
			leaving = true;
			AudioManager.Instance?.PlaySfx(Sfx.MenuConfirm);
			SceneFlow.Instance.GoTo(GameManager.MainMenuScene);
		}
	}

	private async void StartMusicLater() {
		await ToSignal(GetTree().CreateTimer(MusicDelay), SceneTreeTimer.SignalName.Timeout);
		AudioManager.Instance?.PlayMusic(Music.Main);   // the autoload outlives this screen
	}
}
