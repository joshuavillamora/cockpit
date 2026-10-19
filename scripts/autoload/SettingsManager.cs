using Godot;

// Player settings (volumes, fullscreen), saved to user://settings.cfg.
public partial class SettingsManager : Node {
	private const string SavePath = "user://settings.cfg";

	public static SettingsManager Instance { get; private set; }

	public float MasterVolume { get; set; } = 1f;
	public float MusicVolume { get; set; } = 0.8f;
	public float SfxVolume { get; set; } = 1f;
	public bool Fullscreen { get; set; } = true;

	public override void _EnterTree() {
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		Load();
		Apply();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F11) {
			Fullscreen = !Fullscreen;
			Apply();
			Save();
		}
	}

	public void Apply() {
		SetBusVolume("Master", MasterVolume);
		SetBusVolume("Music", MusicVolume);
		SetBusVolume("SFX", SfxVolume);
		DisplayServer.WindowSetMode(Fullscreen
			? DisplayServer.WindowMode.Fullscreen
			: DisplayServer.WindowMode.Windowed);
	}

	public void Save() {
		var cfg = new ConfigFile();
		cfg.SetValue("audio", "master", MasterVolume);
		cfg.SetValue("audio", "music", MusicVolume);
		cfg.SetValue("audio", "sfx", SfxVolume);
		cfg.SetValue("video", "fullscreen", Fullscreen);
		cfg.Save(SavePath);
	}

	private void Load() {
		var cfg = new ConfigFile();
		if (cfg.Load(SavePath) != Error.Ok) return;
		MasterVolume = cfg.GetValue("audio", "master", MasterVolume).AsSingle();
		MusicVolume = cfg.GetValue("audio", "music", MusicVolume).AsSingle();
		SfxVolume = cfg.GetValue("audio", "sfx", SfxVolume).AsSingle();
		Fullscreen = cfg.GetValue("video", "fullscreen", Fullscreen).AsBool();
	}

	private static void SetBusVolume(string bus, float linear) {
		int index = AudioServer.GetBusIndex(bus);
		if (index < 0) return;
		AudioServer.SetBusMute(index, linear <= 0.001f);
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Max(linear, 0.001f)));
	}
}
