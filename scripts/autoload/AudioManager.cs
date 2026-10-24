using Godot;
using System.Collections.Generic;

// Sound names used by the game. Files live in assets/audio/sfx/<name>.(wav|ogg|mp3).
// A missing file is skipped silently, so sounds can be added later without touching code.
// To swap a sound, replace its file and keep the name (see CREDITS.md for where each one came from).
public static class Sfx {
	public const string Bakawk = "bakawk";            // startup rooster call, when PRESS START appears
	public const string MenuMove = "menu_move";
	public const string MenuConfirm = "menu_confirm";
	public const string MenuBack = "menu_back";
	public const string PopupOpen = "popup_open";     // settings, controls, pause
	public const string PopupClose = "popup_close";
	public const string SelectMove = "select_move";   // character and map select cursors
	public const string Ready = "ready";
	public const string Unready = "unready";
	public const string Error = "error";
	public const string MatchStart = "match_start";
	public const string Tip = "tip";
	public const string TipsToggle = "tips_toggle";
	public const string Door = "door";
	public const string RoundStart = "round_start";
	public const string Deathmatch = "deathmatch";
	public const string Fight = "fight";
	public const string Hit = "hit";
	public const string Swing = "swing";              // ground attack starts
	public const string SwingAir = "swing_air";       // aerial attack starts
	public const string Block = "block";
	public const string Ko = "ko";
	public const string AbilityReady = "ability_ready";   // the ability meter fills (no file yet: silent)
	public const string AbilityUse = "ability_use";       // a fighter uses its ability (no file yet: silent)
	public const string Draw = "draw";
	public const string Victory = "victory";

	// A chicken's voiced name, played when a player confirms it: namecall_<CharacterData.Id>
	public static string NameCall(string characterId) => "namecall_" + characterId;
}

// Music names. Files live in assets/audio/music/<name>.(ogg|mp3|wav). Music loops.
public static class Music {
	public const string Main = "main";                // "I Want It All" by Peachwood, everywhere
}

// Plays sound effects and music. Registered as an autoload (see project.godot).
// Every Button gets menu move / confirm sounds automatically, and sliders click when moved.
// Quit and back-to-menu buttons play the back sound instead; overlay Back buttons stay quiet
// because OverlayScreen plays the popup close sound.
public partial class AudioManager : Node {
	private const int SfxVoices = 8;
	private const ulong UiGraceMs = 250;     // ignore the focus / value changes a screen makes while it opens
	private const ulong SliderGapMs = 70;    // don't click on every tiny slider step
	private const ulong FocusQuietMs = 100;  // no focus click right after another sound
	private const float MusicDb = -10f;        // the track is mastered loud; keep it under the effects
	// The lobby and matches jump to the song's driving part and loop it, so a fight never drops into the quiet intro
	private const float IntenseStart = 70f;    // seconds into "I Want It All" (the build into the second chorus)
	private const float IntenseEnd = 158f;     // loops back to IntenseStart here
	private const float SectionFadeSeconds = 0.35f;
	private const float VoiceFadeSeconds = 0.15f;
	private const float MusicFadeSeconds = 0.6f;
	private static readonly string[] Extensions = { "ogg", "wav", "mp3" };
	private static readonly string[] BackButtons = { "QuitButton", "MenuButton" };
	// Attack animation names, as Fighters.Attacks plays them
	private static readonly string[] GroundAttacks = { "neutral_attack", "side_attack", "up_attack", "down_attack" };
	private static readonly string[] AirAttacks = { "neutral_air", "side_air", "up_air", "down_air" };

	// Loudness balance in dB. The files are all peak-normalized, so UI clicks are pulled down
	// and the big moments (banners, KO, the startup call) stay loud. Missing = 0 dB.
	private static readonly Dictionary<string, float> Volumes = new() {
		{ Sfx.MenuMove, -12f }, { Sfx.MenuConfirm, -9f }, { Sfx.MenuBack, -9f },
		{ Sfx.PopupOpen, -10f }, { Sfx.PopupClose, -10f }, { Sfx.SelectMove, -10f },
		{ Sfx.Ready, -10f }, { Sfx.Unready, -8f }, { Sfx.Error, -8f }, { Sfx.Tip, -12f }, { Sfx.TipsToggle, -12f },
		{ Sfx.Door, -6f }, { Sfx.MatchStart, -4f }, { Sfx.RoundStart, -3f }, { Sfx.Deathmatch, -3f },
		{ Sfx.Hit, -4f }, { Sfx.Block, -6f }, { Sfx.Draw, -4f }, { Sfx.Victory, -3f },
		{ Sfx.Swing, -9f }, { Sfx.SwingAir, -9f },
	};

	public static AudioManager Instance { get; private set; }

	private readonly Dictionary<string, AudioStream> cache = new();
	private readonly HashSet<string> missing = new();
	private AudioStreamPlayer[] voices;
	private AudioStreamPlayer musicPlayer;
	private Tween musicFade;
	private string[] voiceNames;
	private float musicTargetDb = MusicDb;
	private bool intense;
	private bool switchingSection;
	private string currentMusic = "";
	private int nextVoice;
	private ulong lastSliderMs;
	private ulong lastSfxMs;
	private bool verbose;   // run the game with:  -- --audio-log   to print what plays

	public override void _EnterTree() {
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		verbose = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--audio-log") >= 0;
	}

	public override void _Ready() {
		voices = new AudioStreamPlayer[SfxVoices];
		for (int i = 0; i < SfxVoices; i++) {
			voices[i] = new AudioStreamPlayer { Bus = "SFX" };
			AddChild(voices[i]);
		}
		musicPlayer = new AudioStreamPlayer { Bus = "Music", VolumeDb = MusicDb };
		AddChild(musicPlayer);

		GetTree().NodeAdded += HookUiNode;
	}

	// Let go of the loaded sounds so Godot doesn't report them as leaked at exit
	public override void _ExitTree() {
		GetTree().NodeAdded -= HookUiNode;
		foreach (AudioStreamPlayer voice in voices) voice.Stream = null;
		musicPlayer.Stream = null;
		cache.Clear();
	}

	// pitchVariation: 0.1 means the pitch is randomly 10% lower or higher, so repeats don't sound identical
	public void PlaySfx(string name, float pitchVariation = 0f) {
		if (voices == null) return;   // called before the autoload is ready
		if (verbose) GD.Print($"[audio] sfx {name}");
		AudioStream stream = Find("sfx", name);
		if (stream == null) return;

		AudioStreamPlayer voice = FreeVoice();
		voiceNames ??= new string[voices.Length];
		voiceNames[System.Array.IndexOf(voices, voice)] = name;
		voice.Stream = stream;
		voice.PitchScale = 1f + (float)GD.RandRange(-pitchVariation, pitchVariation);
		voice.VolumeDb = Volumes.TryGetValue(name, out float db) ? db : 0f;
		voice.Play();
		lastSfxMs = Time.GetTicksMsec();
	}

	// Asking for the track that is already playing does nothing, so music carries across screens.
	public void PlayMusic(string name) {
		if (name == currentMusic) return;
		currentMusic = name;
		if (verbose) GD.Print($"[audio] music {name}");

		AudioStream stream = Find("music", name);
		if (stream == null) {
			musicPlayer.Stop();
			return;
		}
		if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
		else if (stream is AudioStreamMP3 mp3) mp3.Loop = true;

		musicPlayer.Stream = stream;
		musicPlayer.Play();
	}

	// offsetDb is added to the normal music level, e.g. -6 to sit under a fight. Fades smoothly.
	public void SetMusicLevel(float offsetDb) {
		musicTargetDb = MusicDb + offsetDb;
		if (switchingSection) return;   // the section crossfade ends at the new level
		musicFade?.Kill();
		musicFade = CreateTween();
		musicFade.TweenProperty(musicPlayer, "volume_db", musicTargetDb, MusicFadeSeconds);
	}

	// Lobby and matches: crossfade to the intense part of the song (does nothing if already there)
	public void PlayIntense() {
		if (intense) return;
		intense = true;
		JumpMusic(IntenseStart);
	}

	// Menus: back to the start of the song
	public void PlayCalm() {
		if (!intense) return;
		intense = false;
		JumpMusic(0f);
	}

	private void JumpMusic(float seconds) {
		if (!musicPlayer.Playing) return;
		if (verbose) GD.Print($"[audio] music jumps to {seconds}s");
		switchingSection = true;
		musicFade?.Kill();
		musicFade = CreateTween();
		musicFade.TweenProperty(musicPlayer, "volume_db", -40f, SectionFadeSeconds);
		musicFade.TweenCallback(Callable.From(() => musicPlayer.Seek(seconds)));
		musicFade.TweenProperty(musicPlayer, "volume_db", musicTargetDb, SectionFadeSeconds * 2);
		musicFade.TweenCallback(Callable.From(() => switchingSection = false));
	}

	// Fades out every sound whose name starts with prefix (the name calls when the screen changes)
	public void FadeOutSfx(string prefix) {
		if (voiceNames == null) return;
		for (int i = 0; i < voices.Length; i++) {
			AudioStreamPlayer voice = voices[i];
			if (!voice.Playing || voiceNames[i] == null || !voiceNames[i].StartsWith(prefix)) continue;
			Tween fadeOut = CreateTween();
			fadeOut.TweenProperty(voice, "volume_db", -40f, VoiceFadeSeconds);
			fadeOut.TweenCallback(Callable.From(voice.Stop));
		}
	}

	public override void _Process(double delta) {
		if (intense && !switchingSection && musicPlayer.Playing && musicPlayer.GetPlaybackPosition() >= IntenseEnd) {
			musicPlayer.Seek(IntenseStart);
		}
	}

	public void StopMusic() {
		if (verbose) GD.Print("[audio] music stopped");
		currentMusic = "";
		musicPlayer.Stop();
	}

	private AudioStream Find(string folder, string name) {
		string key = $"{folder}/{name}";
		if (cache.TryGetValue(key, out AudioStream cached)) return cached;
		if (missing.Contains(key)) return null;

		foreach (string ext in Extensions) {
			string path = $"res://assets/audio/{key}.{ext}";
			if (ResourceLoader.Exists(path)) {
				var stream = GD.Load<AudioStream>(path);
				cache[key] = stream;
				return stream;
			}
		}

		missing.Add(key);   // only look once per name
		return null;
	}

	private AudioStreamPlayer FreeVoice() {
		foreach (AudioStreamPlayer voice in voices) {
			if (!voice.Playing) return voice;
		}
		AudioStreamPlayer stolen = voices[nextVoice];   // all busy: reuse the oldest
		nextVoice = (nextVoice + 1) % voices.Length;
		return stolen;
	}

	// Gives every Button a move sound on focus and a confirm sound on press, and sliders a click
	private void HookUiNode(Node node) {
		ulong addedAt = Time.GetTicksMsec();

		if (node is Button button) {
			button.FocusEntered += () => {
				// Skip when a screen grabs focus as it opens (it already played its own sound)
				ulong now = Time.GetTicksMsec();
				if (now - addedAt > UiGraceMs && now - lastSfxMs > FocusQuietMs) PlaySfx(Sfx.MenuMove);
			};
			if (button.Name == "BackButton") return;   // the overlay plays its close sound
			bool back = System.Array.IndexOf(BackButtons, button.Name.ToString()) >= 0;
			button.Pressed += () => PlaySfx(back ? Sfx.MenuBack : Sfx.MenuConfirm);
		} else if (node is HSlider slider) {
			slider.ValueChanged += _ => {
				ulong now = Time.GetTicksMsec();
				if (now - addedAt <= UiGraceMs || now - lastSliderMs < SliderGapMs) return;
				lastSliderMs = now;
				PlaySfx(Sfx.MenuMove);
			};
		} else if (node is Fighters fighter) {
			fighter.Ready += () => HookFighter(fighter);   // its sprite is ready once the fighter is
		}
	}

	// Attack swings come from the fighter's sprite starting an attack animation, so Fighters.cs needs
	// no audio code. Covers matches, the lobby and the test fight.
	private void HookFighter(Fighters fighter) {
		var sprite = fighter.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite == null) return;
		sprite.AnimationChanged += () => {
			string anim = sprite.Animation;
			if (System.Array.IndexOf(GroundAttacks, anim) >= 0) PlaySfx(Sfx.Swing, 0.1f);
			else if (System.Array.IndexOf(AirAttacks, anim) >= 0) PlaySfx(Sfx.SwingAir, 0.1f);
		};
	}
}
