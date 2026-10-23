using Godot;
using System.Threading.Tasks;

// In-match HUD. Layout follows the designer's final HUD wireframe (frame 5, everything inside y 0-76,
// so the stage ceiling and platforms must stay below y 76): chicken name, portrait, health bar,
// ability meter and round markers per player, and the match score between them.
// Round, FIGHT! and KO banners have the lettering drawn in; DEATHMATCH and DRAW are written on blank banners.
// MatchController calls Bind() each round.
// Dark frames (ui_dark set) flash when a round marker is earned and when the score changes.
public partial class GameHud : CanvasLayer {
	private const string HudPath = "res://assets/ui/hud/";
	private const string DarkPath = "res://assets/ui/dark/";
	private const float FlashSeconds = 0.15f;
	private const float PulseSeconds = 0.25f;   // how often the full meter swaps gold <-> dark gold
	private const int BlockChipMax = 3;         // a blocked hit only takes 20% (1-3 HP); real hits take 4 or more
	private const float MatchMusicDb = -6f;     // music sits under the fight

	private readonly TextureProgressBar[] bars = new TextureProgressBar[2];
	private readonly TextureProgressBar[] meters = new TextureProgressBar[2];
	private readonly TextureRect[] portraits = new TextureRect[2];
	private readonly Label[] names = new Label[2];
	private readonly HBoxContainer[] markerBoxes = new HBoxContainer[2];
	private readonly bool[] meterFull = new bool[2];
	private readonly int[] lastHealth = new int[2];
	private readonly bool[][] markerFilled = { new bool[GameManager.RoundsToWin], new bool[GameManager.RoundsToWin] };

	private TextureRect counterFrame;
	private Label scoreLabel;
	private ColorRect dim;
	private TextureRect roundBanner;
	private TextureRect koBanner;
	private Texture2D[] roundBanners;
	private Texture2D fightBanner;
	private Texture2D koTexture;
	private Texture2D blankRoundBanner;
	private Texture2D blankKoBanner;
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
		names[0] = GetNode<Label>("%P1Name");
		names[1] = GetNode<Label>("%P2Name");
		markerBoxes[0] = GetNode<HBoxContainer>("%P1Markers");
		markerBoxes[1] = GetNode<HBoxContainer>("%P2Markers");
		counterFrame = GetNode<TextureRect>("%CounterFrame");
		scoreLabel = GetNode<Label>("%ScoreLabel");
		dim = GetNode<ColorRect>("%Dim");
		roundBanner = GetNode<TextureRect>("%RoundBanner");
		koBanner = GetNode<TextureRect>("%KoBanner");

		roundBanners = new[] {
			GD.Load<Texture2D>(HudPath + "ui_banner_round_1.png"),
			GD.Load<Texture2D>(HudPath + "ui_banner_round_2.png"),
			GD.Load<Texture2D>(HudPath + "ui_banner_round_3.png"),
		};
		fightBanner = GD.Load<Texture2D>(HudPath + "ui_banner_fight.png");
		koTexture = GD.Load<Texture2D>(HudPath + "ui_banner_ko.png");
		blankRoundBanner = GD.Load<Texture2D>(HudPath + "ui_banner_round.png");
		blankKoBanner = GD.Load<Texture2D>(HudPath + "ui_banner_blank_ko.png");

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
			names[i].Text = characters[i].DisplayName;
			SetMeter(i, 1f);   // placeholder: always full until abilities exist
			lastHealth[i] = fighters[i].Health;
			SetHealth(i, fighters[i].Health, fighters[i].MaxHealth, false);
			fighters[i].HealthChanged += (current, max) => SetHealth(index, current, max, true);
		}
		UpdateScore(false);
		AudioManager.Instance?.SetMusicLevel(MatchMusicDb);
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
		AudioManager.Instance?.PlaySfx(deathmatch ? Sfx.Deathmatch : Sfx.RoundStart);
		if (deathmatch) {
			await ShowBanner(roundBanner, blankRoundBanner, "DEATHMATCH", 24, 1.6f, true);
		} else if (round >= 1 && round <= roundBanners.Length) {
			await ShowBanner(roundBanner, roundBanners[round - 1], null, 0, 1.0f, false);
		} else {
			await ShowBanner(roundBanner, blankRoundBanner, $"ROUND {round}", 32, 1.0f, false);   // no drawn banner past 3
		}
		AudioManager.Instance?.PlaySfx(Sfx.Fight);
		await ShowBanner(roundBanner, fightBanner, null, 0, 0.6f, false);
	}

	public async Task ShowKo(bool draw) {
		AudioManager.Instance?.PlaySfx(draw ? Sfx.Draw : Sfx.Ko);
		if (draw) {
			await ShowBanner(koBanner, blankKoBanner, "DRAW", 40, 1.8f, true);
		} else {
			await ShowBanner(koBanner, koTexture, null, 0, 1.8f, true);
		}
	}

	private void SetHealth(int player, int current, int max, bool animate) {
		float ratio = max > 0 ? (float)current / max : 0f;
		TextureProgressBar bar = bars[player];
		int lost = lastHealth[player] - current;
		lastHealth[player] = current;
		// Hit sounds come from health changes, so Fighters.cs needs no audio code
		if (animate && lost > 0) AudioManager.Instance?.PlaySfx(lost <= BlockChipMax ? Sfx.Block : Sfx.Hit, 0.1f);
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

	// text = null for banners that have the lettering drawn in
	private async Task ShowBanner(TextureRect banner, Texture2D texture, string text, int fontSize, float seconds, bool dimBackground) {
		banner.Texture = texture;
		var label = banner.GetNode<Label>("Label");
		label.Visible = text != null;
		if (text != null) {
			label.Text = text;
			label.AddThemeFontSizeOverride("font_size", fontSize);
		}
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
