using Godot;

// Training lobby, shown after map select. Both players can test movement and attacks (and hit the
// dummy), then both walk into the door to start the match. Nobody can be knocked out here.
// Layout follows the designer's lobby wireframe (frame 12): a keys panel per player, the dummy's
// HITS plate between them, the door art on the right, and a tips box along the bottom.
public partial class LobbyController : Node2D {
	private const int PracticeHealth = 9999;
	private const float StartDelay = 0.8f;
	private const float TipInterval = 4f;
	private const float RowTop = 30f;
	private const float RowPitch = 16f;
	private const float KeysLeft = 96f;
	private const string DoorPath = "res://assets/ui/hud/ui_lobby_door.png";
	private const string DoorDarkPath = "res://assets/ui/dark/ui_lobby_door_dark.png";
	private const string DotFullPath = "res://assets/ui/hud/ui_round_marker_full.png";
	private const string DotEmptyPath = "res://assets/ui/hud/ui_round_marker_empty.png";
	private static readonly Color ReadyColor = new(0.6f, 1f, 0.6f);
	private static readonly Color SoonColor = new(0.6f, 0.58f, 0.52f);

	// Add the ability tip once abilities exist in Fighters.cs
	private static readonly string[] Tips = {
		"HOLD UP OR DOWN WITH ATTACK FOR DIFFERENT HITS",
		"ATTACK IN THE AIR FOR AERIALS",
		"JUMP AGAIN IN THE AIR TO DOUBLE JUMP",
		"HOLD JUMP WHILE FALLING TO GLIDE",
		"HOLD BLOCK ON THE GROUND TO TAKE LESS DAMAGE",
		"HIT THE DUMMY TO TEST DAMAGE",
		"ABILITIES COME SOON",
	};

	private readonly Fighters[] fighters = new Fighters[2];
	private readonly bool[] inDoor = new bool[2];
	private readonly Label[] playerStatus = new Label[2];
	private TextureRect[] tipDots;
	private Label doorStatus;
	private Label tipLabel;
	private Label hitLabel;
	private Control tipsBox;
	private TextureRect doorArt;
	private Texture2D doorLight;
	private Texture2D doorDark;
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
		hitLabel = GetNode<Label>("%HitLabel");
		tipsBox = GetNode<Control>("%TipsBox");
		doorArt = GetNode<TextureRect>("%Art");
		doorLight = GD.Load<Texture2D>(DoorPath);
		doorDark = GD.Load<Texture2D>(DoorDarkPath);

		BuildPanel(GetNode<Control>("%P1Panel"), 0);
		BuildPanel(GetNode<Control>("%P2Panel"), 1);
		BuildBackHint(GetNode<Control>("%BackHint"));
		BuildTips();

		ApplyArena(gm.SelectedArena);
		SpawnFighters(gm);

		GetNode<TrainingDummy>("Dummy").Hit += (hits, damage) => {
			hitLabel.Text = $"HITS {hits}\nDMG {damage}";
			AudioManager.Instance?.PlaySfx(Sfx.Hit, 0.1f);
		};
		AudioManager.Instance?.PlayMusic(Music.Main);
		AudioManager.Instance?.SetMusicLevel(0f);

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

	private void ApplyArena(ArenaData arena) {
		var background = GetNode<Sprite2D>("Background");
		background.Texture = arena.Background;
		background.Modulate = arena.BackgroundTint;
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
		doorArt.Texture = inDoor[0] || inDoor[1] ? doorDark : doorLight;

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

	// Heading, one row per action from the controls table (keys as keycaps), then the door status
	private void BuildPanel(Control panel, int player) {
		panel.AddChild(new Label {
			Text = $"PLAYER {player + 1}",
			Position = new Vector2(14, 10),
			Size = new Vector2(panel.Size.X - 28, 16),
			HorizontalAlignment = HorizontalAlignment.Center,
		});

		int row = 0;
		foreach (var (action, p1Keys, p2Keys) in ControlsScreen.Rows) {
			if (action == "PAUSE") continue;   // Esc leaves the lobby instead
			float y = RowTop + row++ * RowPitch;
			panel.AddChild(new Label {
				Text = action == "UP/DOWN" ? "AIM" : action,
				Position = new Vector2(14, y),
				Size = new Vector2(80, 16),
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

		playerStatus[player] = new Label {
			Position = new Vector2(14, RowTop + row * RowPitch + 4),
			Size = new Vector2(panel.Size.X - 28, 16),
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		panel.AddChild(playerStatus[player]);
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
