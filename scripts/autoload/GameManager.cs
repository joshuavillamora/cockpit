using Godot;

// Global match state and screen paths. Registered as an autoload (see project.godot).
// Players are 0-indexed here (0 = P1, 1 = P2); winner values elsewhere are 1/2, 0 = draw.
//
// Screen flow: Loading -> MainMenu -> CharacterSelect -> MapSelect -> Lobby -> Game (Victory overlay inside)
public partial class GameManager : Node {
	public const int RoundsToWin = 2;   // 1-1 -> the last round is the Deathmatch

	public const string LoadingScene = "res://scenes/ui/screens/loading_screen.tscn";
	public const string MainMenuScene = "res://scenes/ui/screens/main_menu.tscn";
	public const string CharacterSelectScene = "res://scenes/ui/screens/character_select.tscn";
	public const string MapSelectScene = "res://scenes/ui/screens/map_select.tscn";
	public const string LobbyScene = "res://scenes/ui/screens/lobby_tutorial.tscn";
	public const string GameScene ="res://scenes/gameplay/game.tscn";

	public static GameManager Instance { get; private set; }

	public CharacterData[] Selected { get; } = new CharacterData[2];
	public ArenaData SelectedArena { get; private set; }
	public int[] Wins { get; } = new int[2];        // rounds won in the current match (HUD dots)
	public int[] MatchWins { get; } = new int[2];   // matches won since the last menu (HUD center score)

	// Draws replay the same round, so the round number follows the decisive rounds.
	public int Round => Wins[0] + Wins[1] + 1;
	public bool IsDeathmatch => Wins[0] == RoundsToWin - 1 && Wins[1] == RoundsToWin - 1;
	public int MatchWinner => Wins[0] >= RoundsToWin ? 1 : Wins[1] >= RoundsToWin ? 2 : 0;

	public override void _EnterTree() {
		Instance = this;
	}

	// Lets game.tscn be run on its own (F6) without going through the menus.
	public void EnsureSelection() {
		Selected[0] ??= GD.Load<CharacterData>("res://resources/characters/banaba.tres");
		Selected[1] ??= GD.Load<CharacterData>("res://resources/characters/joloano.tres");
		SelectedArena ??= GD.Load<ArenaData>("res://resources/arenas/thepit_day.tres");
	}

	// Character select -> map select
	public void SelectCharacters(CharacterData p1, CharacterData p2) {
		Selected[0] = p1;
		Selected[1] = p2;
		SceneFlow.Instance.GoTo(MapSelectScene);
	}

	// Map select -> lobby (a new series starts here)
	public void SelectArena(ArenaData arena) {
		SelectedArena = arena;
		MatchWins[0] = 0;
		MatchWins[1] = 0;
		ResetScores();
		SceneFlow.Instance.GoTo(LobbyScene);
	}

	// Lobby -> match (both players walked through the door)
	public void EnterMatch() {
		SceneFlow.Instance.GoTo(GameScene);
	}

	public void Rematch() {
		ResetScores();
		SceneFlow.Instance.GoTo(GameScene);
	}

	public void ReturnToMenu() {
		MatchWins[0] = 0;
		MatchWins[1] = 0;
		ResetScores();
		SceneFlow.Instance.GoTo(MainMenuScene);
	}

	// winner: 1 or 2, 0 for a draw. Returns true when the match is over.
	public bool RegisterRoundResult(int winner) {
		if (winner > 0) Wins[winner - 1]++;
		bool matchOver = MatchWinner != 0;
		if (matchOver) MatchWins[MatchWinner - 1]++;
		return matchOver;
	}

	private void ResetScores() {
		Wins[0] = 0;
		Wins[1] = 0;
	}
}
