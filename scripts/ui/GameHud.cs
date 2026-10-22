using Godot;
using System.Threading.Tasks;

// In-match HUD. Layout follows the designer's HUD wireframe (everything inside y 0-64):
// portrait, health bar, ability meter and round markers per player, and the match score between them.
// Round and K.O. banners appear in the middle. MatchController calls Bind() each round.
// Dark frames (ui_dark set) flash when a round marker is earned and when the score changes.
public partial class GameHud : CanvasLayer {
	private const string HudPath = "res://assets/ui/hud/";
	private const string DarkPath = "res://assets/ui/dark/";
	private const float FlashSeconds = 0.15f;
	private const float PulseSeconds = 0.25f;   // how often the full meter swaps gold <-> dark gold

	private readonly TextureProgressBar[] bars = new TextureProgressBar[2];
	private readonly TextureProgressBar[] meters = new TextureProgressBar[2];
	private readonly TextureRect[] portraits = new TextureRect[2];
	private readonly HBoxContainer[] markerBoxes = new HBoxContainer[2];
	private readonly bool[] meterFull = new bool[2];
	private readonly bool[][] markerFilled = { new bool[GameManager.RoundsToWin], new bool[GameManager.RoundsToWin] };

	private TextureRect counterFrame;
	private Label scoreLabel;
	private ColorRect dim;
	private Control roundBanner;
	private Control koBanner;
	private Texture2D markerFull;
	private Texture2D markerFullDark;
	private Texture2D markerEmpty;
	private Texture2D counterNormal;
	private Texture2D counterDark;
	private Texture2D meterFill;
	private Texture2D meterReady;
	private Texture2D meterReadyDark;
	private string lastScore = "";

	public override void _Ready() {
		bars[0] = GetNode<TextureProgressBar>("%P1Bar");
		bars[1] = GetNode<TextureProgressBar>("%P2Bar");
		meters[0] = GetNode<TextureProgressBar>("%P1Meter");
		meters[1] = GetNode<TextureProgressBar>("%P2Meter");
		portraits[0] = GetNode<TextureRect>("%P1Portrait");
		portraits[1] = GetNode<TextureRect>("%P2Portrait");
		markerBoxes[0] = GetNode<HBoxContainer>("%P1Markers");
		markerBoxes[1] = GetNode<HBoxContainer>("%P2Markers");
		counterFrame = GetNode<TextureRect>("%CounterFrame");
		scoreLabel = GetNode<Label>("%ScoreLabel");
		dim = GetNode<ColorRect>("%Dim");
		roundBanner = GetNode<Control>("%RoundBanner");
		koBanner = GetNode<Control>("%KoBanner");

		markerFull = GD.Load<Texture2D>(HudPath + "ui_round_marker_full.png");
		markerFullDark = GD.Load<Texture2D>(DarkPath + "ui_round_marker_full_dark.png");
		markerEmpty = GD.Load<Texture2D>(HudPath + "ui_round_marker_empty.png");
		counterNormal = counterFrame.Texture;
		counterDark = GD.Load<Texture2D>(DarkPath + "ui_counter_frame_dark.png");
		meterFill = GD.Load<Texture2D>(HudPath + "ui_meter_fill.png");
		meterReady = GD.Load<Texture2D>(HudPath + "ui_meter_fill_ready.png");
		meterReadyDark = GD.Load<Texture2D>(DarkPath + "ui_meter_fill_ready_dark.png");

		foreach (HBoxContainer box in markerBoxes) {
			for (int i = 0; i < GameManager.RoundsToWin; i++) {
				box.AddChild(new TextureRect { Texture = markerEmpty, CustomMinimumSize = new Vector2(16, 16) });
			}
		}
	}

	// The full meter pulses by alternating the gold fill and its dark version
	public override void _Process(double delta) {
		bool dark = (int)(Time.GetTicksMsec() / (ulong)(PulseSeconds * 1000)) % 2 == 1;
		for (int p = 0; p < 2; p++) {
			if (meterFull[p]) meters[p].TextureProgress = dark ? meterReadyDark : meterReady;
		}
	}

	public void Bind(Fighters p1, Fighters p2, CharacterData c1, CharacterData c2) {
		Fighters[] fighters = { p1, p2 };
		CharacterData[] characters = { c1, c2 };

		for (int i = 0; i < 2; i++) {
			int index = i;
			portraits[i].Texture = characters[i].HudPortrait;
			SetMeter(i, 1f);   // placeholder: always full until abilities exist
			SetHealth(i, fighters[i].Health, fighters[i].MaxHealth, false);
			fighters[i].HealthChanged += (current, max) => SetHealth(index, current, max, true);
		}
		UpdateScore(false);
	}

	// ratio 0..1. A full meter shows the gold fill and pulses; anything less shows the normal fill.
	// The backend calls this when ability charge exists (P1 fills left to right, P2 right to left).
	public void SetMeter(int player, float ratio) {
		ratio = Mathf.Clamp(ratio, 0f, 1f);
		meters[player].Value = ratio * 100.0;
		meterFull[player] = ratio >= 0.999f;
		if (!meterFull[player]) meters[player].TextureProgress = meterFill;
	}

	// animate = false for the first update of a round, so nothing flashes at the start
	public void UpdateScore(bool animate = true) {
		GameManager gm = GameManager.Instance;

		string score = $"{gm.MatchWins[0]}-{gm.MatchWins[1]}";   // matches won, kept across rematches
		if (animate && score != lastScore) Flash(counterFrame, counterDark, counterNormal);
		lastScore = score;
		scoreLabel.Text = score;

		for (int p = 0; p < 2; p++) {
			for (int m = 0; m < GameManager.RoundsToWin; m++) {
				bool filled = gm.Wins[p] > m;
				var marker = (TextureRect)markerBoxes[p].GetChild(m);
				marker.Texture = filled ? markerFull : markerEmpty;
				if (animate && filled && !markerFilled[p][m]) Flash(marker, markerFullDark, markerFull);
				markerFilled[p][m] = filled;
			}
		}
	}

	public async Task PlayRoundIntro(int round, bool deathmatch) {
		if (deathmatch) {
			await ShowBanner(roundBanner, "DEATHMATCH", 24, 1.6f, true);
		} else {
			await ShowBanner(roundBanner, $"ROUND {round}", 32, 1.0f, false);
		}
		await ShowBanner(roundBanner, "FIGHT!", 32, 0.6f, false);
	}

	public async Task ShowKo(bool draw) {
		await ShowBanner(koBanner, draw ? "DRAW" : "K.O.", 40, 1.8f, true);
	}

	private void SetHealth(int player, int current, int max, bool animate) {
		float ratio = max > 0 ? (float)current / max : 0f;
		TextureProgressBar bar = bars[player];
		if (animate) {
			CreateTween().TweenProperty(bar, "value", ratio * 100.0, 0.15);
		} else {
			bar.Value = ratio * 100.0;
		}
	}

	// Show the dark texture for a moment, then go back to the light one
	private async void Flash(TextureRect rect, Texture2D dark, Texture2D normal) {
		rect.Texture = dark;
		await ToSignal(GetTree().CreateTimer(FlashSeconds), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(rect)) return;   // scene changed in the meantime
		rect.Texture = normal;
	}

	private async Task ShowBanner(Control banner, string text, int fontSize, float seconds, bool dimBackground) {
		var label = banner.GetNode<Label>("Label");
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", fontSize);
		banner.PivotOffset = banner.Size / 2;
		banner.Scale = new Vector2(1.5f, 1.5f);
		dim.Visible = dimBackground;
		banner.Visible = true;
		CreateTween().TweenProperty(banner, "scale", Vector2.One, 0.15)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		await ToSignal(GetTree().CreateTimer(seconds, false), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(this)) return;   // scene changed while waiting
		banner.Visible = false;
		dim.Visible = false;
	}
}
