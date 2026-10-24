using Godot;
using System.Threading.Tasks;

// Runs a match inside game.tscn: spawns fighters each round, drives intro -> fight -> KO,
// records the result in GameManager, then starts the next round or goes to Victory.
public partial class MatchController : Node {
	private const float DoubleKoWindow = 0.1f;

	private readonly Fighters[] fighters = new Fighters[2];
	private GameHud hud;
	private Node2D arena;
	private MatchCamera camera;
	private bool roundActive;

	public override void _Ready() {
		GameManager.Instance.EnsureSelection();
		hud = GetNode<GameHud>("../HUD");
		CallDeferred(MethodName.LoadArena);    // parent is still setting up its children
		CallDeferred(MethodName.StartRound);
	}

	// The chosen arena scene (art, floor, walls, platforms, spawns) goes behind everything else,
	// plus the camera that follows the fighters on arenas bigger than the screen
	private void LoadArena() {
		ArenaData data = GameManager.Instance.SelectedArena;
		arena = data.Scene.Instantiate<Node2D>();
		GetParent().AddChild(arena);
		GetParent().MoveChild(arena, 0);

		camera = new MatchCamera();
		GetParent().AddChild(camera);
		camera.Setup(data.Size);
	}

	private async void StartRound() {
		GameManager gm = GameManager.Instance;
		SpawnFighters(gm);
		camera.Track(fighters[0], fighters[1]);
		hud.Bind(fighters[0], fighters[1], gm.Selected[0], gm.Selected[1]);

		await hud.PlayRoundIntro(gm.Round, gm.IsDeathmatch);
		if (!IsInstanceValid(this)) return;   // left the match during the intro

		foreach (Fighters fighter in fighters) fighter.InputLocked = false;
		roundActive = true;
	}

	private void SpawnFighters(GameManager gm) {
		for (int i = 0; i < 2; i++) {
			if (IsInstanceValid(fighters[i])) fighters[i].QueueFree();

			var fighter = gm.Selected[i].Scene.Instantiate<Fighters>();
			fighter.PlayerId = i + 1;
			fighter.StartFacingRight = i == 0;
			fighter.InputLocked = true;
			fighter.Position = arena.GetNode<Marker2D>(i == 0 ? "SpawnP1" : "SpawnP2").Position;
			GetParent().AddChild(fighter);

			int index = i;
			fighter.Died += () => OnFighterDied(index);
			fighters[i] = fighter;
		}
	}

	private async void OnFighterDied(int index) {
		if (!roundActive) return;
		roundActive = false;
		foreach (Fighters fighter in fighters) fighter.InputLocked = true;

		// A second KO inside this window counts as a double KO (draw, round is replayed)
		await Wait(DoubleKoWindow);
		if (!IsInstanceValid(this)) return;

		bool p1Dead = fighters[0].IsDead;
		bool p2Dead = fighters[1].IsDead;
		int winner = p1Dead && p2Dead ? 0 : p1Dead ? 2 : 1;

		bool matchOver = GameManager.Instance.RegisterRoundResult(winner);
		hud.UpdateScore();
		await hud.ShowKo(winner == 0);
		if (!IsInstanceValid(this)) return;

		if (matchOver) {
			GetNode<VictoryOverlay>("../VictoryOverlay").Present(GameManager.Instance.MatchWinner);
		} else {
			StartRound();
		}
	}

	private async Task Wait(float seconds) {
		await ToSignal(GetTree().CreateTimer(seconds, false), SceneTreeTimer.SignalName.Timeout);
	}
}
