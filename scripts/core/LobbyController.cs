using Godot;

// Training lobby, shown after map select. Both players can test movement and attacks (and hit the
// dummy), then both walk into the door to start the match. Nobody can be knocked out here.
// Layout follows the designers' coop art (wireframe 13): background, tile ground and the PIT door,
// each player's keys written into one of the coop's two windows (behind the fighters), the dummy
// between them, and a tips box on the ground.
public partial class LobbyController : Node2D {
	private const int PracticeHealth = 9999;
	private const float StartDelay = 0.8f;
	private const float TipInterval = 4f;
	private const float RowTop = 36f;
	private const float RowPitch = 16f;
	private const float RowLeft = 10f;
	private const float KeysLeft = 78f;
	private static readonly Color DoorWaiting = new(0.55f, 0.55f, 0.55f);   // the door darkens while someone is inside
	private const string DotFullPath = "res://assets/ui/hud/ui_round_marker_full.png";
	private const string DotEmptyPath = "res://assets/ui/hud/ui_round_marker_empty.png";
	private static readonly Color ReadyColor = new(0.6f, 1f, 0.6f);
	private static readonly Color SoonColor = new(0.6f, 0.58f, 0.52f);

	private static readonly string[] Tips = {
		"HOLD UP OR DOWN WITH ATTACK FOR DIFFERENT HITS",
		"ATTACK IN THE AIR FOR AERIALS",
		"JUMP AGAIN IN THE AIR TO DOUBLE JUMP",
		"HOLD JUMP WHILE FALLING TO GLIDE",
		"HOLD BLOCK ON THE GROUND TO TAKE LESS DAMAGE",
		"HIT THE DUMMY TO TEST DAMAGE",
		"PRESS ABILITY WHEN THE METER IS FULL",
	};

	private readonly Fighters[] fighters = new Fighters[2];
	private readonly bool[] inDoor = new bool[2];
	private readonly Label[] playerStatus = new Label[2];
	private TextureRect[] tipDots;
	private Label doorStatus;
	private Label tipLabel;
	private Control tipsBox;
	private Sprite2D doorArt;
	private Texture2D dotFull;
	private Texture2D dotEmpty;
	private double tipTimer;
	private int tipIndex;
	private bool starting;

	public override void _Ready() {
		GameManager gm = GameManager.Instance;
		gm.EnsureSelection();

		doorStatus = GetNode<Label>("%DoorStatus");
		tipLabel = GetNode<Label>("%TipLabel");
		tipsBox = GetNode<Control>("%TipsBox");
		doorArt = GetNode<Sprite2D>("%DoorArt");
		playerStatus[0] = GetNode<Label>("%P1Status");
		playerStatus[1] = GetNode<Label>("%P2Status");

		BuildPanel(GetNode<Control>("%P1Panel"), 0, gm.Selected[0]);
		BuildPanel(GetNode<Control>("%P2Panel"), 1, gm.Selected[1]);
		BuildBackHint(GetNode<Control>("%BackHint"));
		BuildTips();

		SpawnFighters(gm);

		GetNode<TrainingDummy>("Dummy").Hit += (hits, damage) => AudioManager.Instance?.PlaySfx(Sfx.Hit, 0.1f);
		AudioManager.Instance?.PlayMusic(Music.Main);
		AudioManager.Instance?.SetMusicLevel(0f);
		AudioManager.Instance?.PlayIntense();

		var door = GetNode<Area2D>("%Door");
		door.BodyEntered += body => SetInDoor(body, true);
		door.BodyExited += body => SetInDoor(body, false);

		ShowTip(0);
		Refresh();
	}

	public override void _Process(double delta) {
		tipTimer += delta;
		if (tipTimer >= TipInterval) ShowTip(tipIndex + 1);
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (!starting && @event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			AudioManager.Instance?.PlaySfx(Sfx.MenuBack);
			SceneFlow.Instance.GoTo(GameManager.MapSelectScene);
		} else if (@event.IsActionPressed("menu_start")) {
			GetViewport().SetInputAsHandled();
			tipsBox.Visible = true;
			ShowTip(tipIndex + 1);
			AudioManager.Instance?.PlaySfx(Sfx.Tip);
		} else if (@event.IsActionPressed("lobby_skip_tips")) {
			GetViewport().SetInputAsHandled();
			tipsBox.Visible = !tipsBox.Visible;
			AudioManager.Instance?.PlaySfx(Sfx.TipsToggle);
		}
	}

	private void SpawnFighters(GameManager gm) {
		for (int i = 0; i < 2; i++) {
			var fighter = gm.Selected[i].Scene.Instantiate<Fighters>();
			fighter.PlayerId = i + 1;
			fighter.StartFacingRight = true;
			fighter.MaxHealth = PracticeHealth;
			fighter.Position = GetNode<Marker2D>(i == 0 ? "SpawnP1" : "SpawnP2").Position;
			AddChild(fighter);
			fighters[i] = fighter;
		}
	}

	// A fighter standing in the door counts as ready; walking out un-readies them
	private void SetInDoor(Node2D body, bool inside) {
		if (starting || body is not Fighters fighter) return;

		if (inside && !inDoor[fighter.PlayerId - 1]) AudioManager.Instance?.PlaySfx(Sfx.Door);
		inDoor[fighter.PlayerId - 1] = inside;
		fighter.Modulate = new Color(1, 1, 1, inside ? 0.5f : 1f);
		Refresh();

		if (inDoor[0] && inDoor[1]) BeginMatch();
	}

	private async void BeginMatch() {
		starting = true;
		foreach (Fighters fighter in fighters) fighter.InputLocked = true;
		AudioManager.Instance?.PlaySfx(Sfx.MatchStart);
		Refresh();

		await ToSignal(GetTree().CreateTimer(StartDelay), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(this)) return;   // left the lobby while waiting

		GameManager.Instance.EnterMatch();
	}

	private void Refresh() {
		for (int p = 0; p < 2; p++) {
			playerStatus[p].Text = inDoor[p] ? "READY!" : "GO TO THE DOOR";
			playerStatus[p].Modulate = inDoor[p] ? ReadyColor : Colors.White;
		}

		// The door goes dark while someone is waiting inside it
		doorArt.Modulate = inDoor[0] || inDoor[1] ? DoorWaiting : Colors.White;

		if (starting) doorStatus.Text = "STARTING...";
		else if (inDoor[0]) doorStatus.Text = "P1 READY - WAITING FOR P2";
		else if (inDoor[1]) doorStatus.Text = "P2 READY - WAITING FOR P1";
		else doorStatus.Text = "BOTH PLAYERS: ENTER THE DOOR";
	}

	private void ShowTip(int index) {
		tipIndex = index % Tips.Length;
		tipTimer = 0;
		tipLabel.Text = Tips[tipIndex];
		for (int i = 0; i < tipDots.Length; i++) tipDots[i].Texture = i == tipIndex ? dotFull : dotEmpty;
	}

	// Chicken icon and heading, then one row per action from the controls table (keys as keycaps).
	// The panel sits inside one of the coop's windows; the status line hangs under the window.
	private static void BuildPanel(Control panel, int player, CharacterData chicken) {
		panel.AddChild(new TextureRect {
			Texture = chicken.HudPortrait,
			Position = new Vector2(RowLeft, 2),
			Size = new Vector2(32, 32),
			FlipH = player == 1,   // P2 faces left, like on the HUD
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		panel.AddChild(new Label {
			Text = $"PLAYER {player + 1}",
			Position = new Vector2(RowLeft + 38, 10),
			Size = new Vector2(panel.Size.X - RowLeft - 38, 16),
		});

		int row = 0;
		foreach (var (action, p1Keys, p2Keys) in ControlsScreen.Rows) {
			if (action == "PAUSE") continue;   // Esc leaves the lobby instead
			float y = RowTop + row++ * RowPitch;
			panel.AddChild(new Label {
				Text = action == "UP/DOWN" ? "AIM" : action,
				Position = new Vector2(RowLeft, y),
				Size = new Vector2(KeysLeft - RowLeft, 16),
				VerticalAlignment = VerticalAlignment.Center,
			});

			string[] keys = player == 0 ? p1Keys : p2Keys;
			if (keys == null) {
				var soon = new Label { Text = "SOON", Position = new Vector2(KeysLeft, y), Size = new Vector2(80, 16), VerticalAlignment = VerticalAlignment.Center };
				soon.AddThemeColorOverride("font_color", SoonColor);
				panel.AddChild(soon);
				continue;
			}

			float x = KeysLeft;
			foreach (string key in keys) {
				Keycap cap = Keycap.Create(key);
				cap.Position = new Vector2(x, y);
				panel.AddChild(cap);
				x += cap.Size.X + 3;
			}
		}
	}

	private static void BuildBackHint(Control area) {
		Keycap esc = Keycap.Create("ESC");
		area.AddChild(esc);
		area.AddChild(new Label { Text = "BACK", Position = new Vector2(esc.Size.X + 4, 0), Size = new Vector2(48, 16), VerticalAlignment = VerticalAlignment.Center });
	}

	// One dot per tip (round markers), and ENTER: NEXT / TAB: SKIP as keycaps
	private void BuildTips() {
		dotFull = GD.Load<Texture2D>(DotFullPath);
		dotEmpty = GD.Load<Texture2D>(DotEmptyPath);

		var dots = GetNode<HBoxContainer>("%TipDots");
		tipDots = new TextureRect[Tips.Length];
		for (int i = 0; i < Tips.Length; i++) {
			tipDots[i] = new TextureRect { Texture = dotEmpty, CustomMinimumSize = new Vector2(16, 16) };
			dots.AddChild(tipDots[i]);
		}

		var keys = GetNode<HBoxContainer>("%TipKeys");
		keys.AddChild(Keycap.Create("ENTER"));
		keys.AddChild(new Label { Text = "NEXT", VerticalAlignment = VerticalAlignment.Center });
		keys.AddChild(new Control { CustomMinimumSize = new Vector2(8, 0) });
		keys.AddChild(Keycap.Create("TAB"));
		keys.AddChild(new Label { Text = "SKIP", VerticalAlignment = VerticalAlignment.Center });
	}
}
