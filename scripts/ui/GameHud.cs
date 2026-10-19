using Godot;
using System.Threading.Tasks;

public enum AbilityState { Ready, Cooling, Unavailable }

// In-match HUD: health bars, portraits, score, round markers, ability icons and the big
// center text (ROUND N / FIGHT! / DEATHMATCH / K.O.). MatchController calls Bind() each round;
// the backend can call SetAbilityState() once abilities exist.
public partial class GameHud : CanvasLayer {
	private readonly TextureProgressBar[] bars = new TextureProgressBar[2];
	private readonly TextureRect[] portraits = new TextureRect[2];
	private readonly Label[] names = new Label[2];
	private readonly TextureRect[] abilityIcons = new TextureRect[2];
	private readonly HBoxContainer[] markerBoxes = new HBoxContainer[2];
	private readonly CharacterData[] characters = new CharacterData[2];

	private Label scoreLabel;
	private ColorRect dim;
	private Label bigText;
	private Texture2D markerFull;
	private Texture2D markerEmpty;

	public override void _Ready() {
		bars[0] = GetNode<TextureProgressBar>("%P1Bar");
		bars[1] = GetNode<TextureProgressBar>("%P2Bar");
		portraits[0] = GetNode<TextureRect>("%P1Portrait");
		portraits[1] = GetNode<TextureRect>("%P2Portrait");
		names[0] = GetNode<Label>("%P1Name");
		names[1] = GetNode<Label>("%P2Name");
		abilityIcons[0] = GetNode<TextureRect>("%P1Ability");
		abilityIcons[1] = GetNode<TextureRect>("%P2Ability");
		markerBoxes[0] = GetNode<HBoxContainer>("%P1Markers");
		markerBoxes[1] = GetNode<HBoxContainer>("%P2Markers");
		scoreLabel = GetNode<Label>("%ScoreLabel");
		dim = GetNode<ColorRect>("%Dim");
		bigText = GetNode<Label>("%BigText");

		markerFull = GD.Load<Texture2D>("res://assets/ui/hud/ui_round_marker_full.png");
		markerEmpty = GD.Load<Texture2D>("res://assets/ui/hud/ui_round_marker_empty.png");
		foreach (HBoxContainer box in markerBoxes) {
			for (int i = 0; i < GameManager.RoundsToWin; i++) {
				box.AddChild(new TextureRect { Texture = markerEmpty, CustomMinimumSize = new Vector2(16, 16) });
			}
		}
	}

	public void Bind(Fighters p1, Fighters p2, CharacterData c1, CharacterData c2) {
		Fighters[] fighters = { p1, p2 };
		characters[0] = c1;
		characters[1] = c2;

		for (int i = 0; i < 2; i++) {
			int index = i;
			portraits[i].Texture = characters[i].Portrait;
			names[i].Text = characters[i].DisplayName;
			SetAbilityState(i, AbilityState.Ready);
			SetHealth(i, fighters[i].Health, fighters[i].MaxHealth, false);
			fighters[i].HealthChanged += (current, max) => SetHealth(index, current, max, true);
		}
		UpdateScore();
	}

	public void SetAbilityState(int player, AbilityState state) {
		CharacterData data = characters[player];
		abilityIcons[player].Texture = state switch {
			AbilityState.Ready => data.AbilityReady,
			AbilityState.Cooling => data.AbilityCooling,
			_ => data.AbilityUnavailable,
		};
	}

	public void UpdateScore() {
		GameManager gm = GameManager.Instance;
		scoreLabel.Text = $"{gm.MatchWins[0]}-{gm.MatchWins[1]}";   // matches won, kept across rematches
		for (int p = 0; p < 2; p++) {
			for (int m = 0; m < markerBoxes[p].GetChildCount(); m++) {
				((TextureRect)markerBoxes[p].GetChild(m)).Texture = gm.Wins[p] > m ? markerFull : markerEmpty;
			}
		}
	}

	public async Task PlayRoundIntro(int round, bool deathmatch) {
		if (deathmatch) {
			await ShowText("DEATHMATCH", 48, 1.6f, true);
		} else {
			await ShowText($"ROUND {round}", 48, 1.0f, false);
		}
		await ShowText("FIGHT!", 48, 0.6f, false);
	}

	public async Task ShowKo(bool draw) {
		await ShowText(draw ? "DRAW" : "K.O.", 64, 1.8f, true);
	}

	private void SetHealth(int player, int current, int max, bool animate) {
		float ratio = max > 0 ? (float)current / max : 0f;
		TextureProgressBar bar = bars[player];
		bar.TintProgress = new Color(Mathf.Min(1f, 2f * (1f - ratio)), Mathf.Min(1f, 2f * ratio), 0.2f);
		if (animate) {
			CreateTween().TweenProperty(bar, "value", ratio * 100.0, 0.15);
		} else {
			bar.Value = ratio * 100.0;
		}
	}

	private async Task ShowText(string text, int fontSize, float seconds, bool dimBackground) {
		bigText.Text = text;
		bigText.AddThemeFontSizeOverride("font_size", fontSize);
		bigText.PivotOffset = bigText.Size / 2;
		bigText.Scale = new Vector2(1.5f, 1.5f);
		dim.Visible = dimBackground;
		bigText.Visible = true;
		CreateTween().TweenProperty(bigText, "scale", Vector2.One, 0.15)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		await ToSignal(GetTree().CreateTimer(seconds, false), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(this)) return;   // scene changed while waiting
		bigText.Visible = false;
		dim.Visible = false;
	}
}
