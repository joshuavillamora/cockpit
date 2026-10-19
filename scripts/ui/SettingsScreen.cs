using Godot;

// Volume sliders and fullscreen. Controls live on their own screen (ControlsScreen).
public partial class SettingsScreen : OverlayScreen {
	public const string ScenePath = "res://scenes/ui/screens/settings_screen.tscn";

	public override void _Ready() {
		var settings = SettingsManager.Instance;
		var masterSlider = GetNode<HSlider>("%MasterSlider");
		var musicSlider = GetNode<HSlider>("%MusicSlider");
		var sfxSlider = GetNode<HSlider>("%SfxSlider");
		var fullscreen = GetNode<CheckButton>("%FullscreenCheck");

		masterSlider.Value = settings.MasterVolume;
		musicSlider.Value = settings.MusicVolume;
		sfxSlider.Value = settings.SfxVolume;
		fullscreen.ButtonPressed = settings.Fullscreen;

		masterSlider.ValueChanged += v => { settings.MasterVolume = (float)v; settings.Apply(); };
		musicSlider.ValueChanged += v => { settings.MusicVolume = (float)v; settings.Apply(); };
		sfxSlider.ValueChanged += v => { settings.SfxVolume = (float)v; settings.Apply(); };
		fullscreen.Toggled += on => { settings.Fullscreen = on; settings.Apply(); };

		var controlsButton = GetNode<Button>("%ControlsButton");
		controlsButton.Pressed += () => OverlayScreen.Open(this, ControlsScreen.ScenePath, () => controlsButton.GrabFocus());
		GetNode<Button>("%BackButton").Pressed += Close;
		masterSlider.GrabFocus();
	}

	protected override void OnClosing() {
		SettingsManager.Instance.Save();
	}
}
