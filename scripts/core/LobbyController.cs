using Godot;

// Training lobby, shown after map select. Both players can test movement and attacks (and hit the
// dummy), then both walk into the door to start the match. Nobody can be knocked out here.
public partial class LobbyController : Node2D {
	private const int PracticeHealth = 9999;
	private const float StartDelay = 0.8f;
	private const float TipInterval = 4f;

	// Add abilities / block here once they exist in Fighters.cs
	private static readonly string[] Tips = {
		"HOLD UP OR DOWN WITH ATTACK",
		"ATTACK IN THE AIR FOR AERIALS",
		"HIT THE DUMMY TO TEST DAMAGE",
		"RELEASE JUMP EARLY FOR A SHORT HOP",
		"BLOCK AND ABILITIES COME SOON",
	};

	private readonly Fighters[] fighters = new Fighters[2];
	private readonly bool[] inDoor = new bool[2];
	private Label[] playerStatus;
	private Label doorStatus;
	private Label tipLabel;
	private double tipTimer;
	private int tipIndex;
	private bool starting;

	public override void _Ready() {
		GameManager gm = GameManager.Instance;
		gm.EnsureSelection();

		playerStatus = new[] { GetNode<Label>("%P1Status"), GetNode<Label>("%P2Status") };
		doorStatus = GetNode<Label>("%DoorStatus");
		tipLabel = GetNode<Label>("%TipLabel");

		ApplyArena(gm.SelectedArena);
		SpawnFighters(gm);

		var door = GetNode<Area2D>("%Door");
		door.BodyEntered += body => SetInDoor(body, true);
		door.BodyExited += body => SetInDoor(body, false);

		tipLabel.Text = Tips[0];
		Refresh();
	}

	public override void _Process(double delta) {
		tipTimer += delta;
		if (tipTimer >= TipInterval) {
			tipTimer = 0;
			tipIndex = (tipIndex + 1) % Tips.Length;
			tipLabel.Text = Tips[tipIndex];
		}
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (!starting && @event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			SceneFlow.Instance.GoTo(GameManager.MapSelectScene);
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

		inDoor[fighter.PlayerId - 1] = inside;
		fighter.Modulate = new Color(1, 1, 1, inside ? 0.5f : 1f);
		Refresh();

		if (inDoor[0] && inDoor[1]) BeginMatch();
	}

	private async void BeginMatch() {
		starting = true;
		foreach (Fighters fighter in fighters) fighter.InputLocked = true;
		Refresh();

		await ToSignal(GetTree().CreateTimer(StartDelay), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(this)) return;   // left the lobby while waiting

		GameManager.Instance.EnterMatch();
	}

	private void Refresh() {
		for (int p = 0; p < 2; p++) {
			playerStatus[p].Text = inDoor[p] ? "READY!" : "GO TO THE DOOR";
			playerStatus[p].Modulate = inDoor[p] ? new Color(0.6f, 1f, 0.6f) : Colors.White;
		}

		if (starting) doorStatus.Text = "STARTING...";
		else if (inDoor[0]) doorStatus.Text = "P1 READY - WAITING FOR P2";
		else if (inDoor[1]) doorStatus.Text = "P2 READY - WAITING FOR P1";
		else doorStatus.Text = "BOTH PLAYERS: ENTER THE DOOR";
	}
}
