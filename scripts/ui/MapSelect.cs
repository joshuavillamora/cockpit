using Godot;
using System.Collections.Generic;

// Either player can move the cursor and start the match (the arena is a shared choice).
public partial class MapSelect : Control {
	private static readonly string[] ArenaPaths = {
		"res://resources/arenas/backyard.tres",
		"res://resources/arenas/backyard_night.tres",
	};

	private const float CardTop = 100f;
	private const float CardGap = 40f;

	private readonly List<ArenaData> arenas = new();
	private readonly List<Control> cards = new();
	private Label mapName;
	private int cursor;

	public override void _Ready() {
		mapName = GetNode<Label>("%MapName");
		var frameTexture = GD.Load<Texture2D>("res://assets/ui/menus/ui_map_frame.png");

		foreach (string path in ArenaPaths) arenas.Add(GD.Load<ArenaData>(path));

		// Cards are centered as a row: [frame 264x152] gap [frame 264x152] ...
		float cardWidth = frameTexture.GetWidth();
		float left = (640f - (arenas.Count * cardWidth + (arenas.Count - 1) * CardGap)) / 2f;
		for (int i = 0; i < arenas.Count; i++) {
			var card = new Control {
				Position = new Vector2(left + i * (cardWidth + CardGap), CardTop),
				Size = frameTexture.GetSize(),
			};
			card.AddChild(new TextureRect { Texture = frameTexture });
			card.AddChild(new TextureRect { Texture = arenas[i].Thumbnail, Position = new Vector2(4, 4) });
			AddChild(card);
			cards.Add(card);
		}

		Refresh();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			SceneFlow.Instance.GoTo(GameManager.CharacterSelectScene);
		}
	}

	public override void _Process(double delta) {
		if (Input.IsActionJustPressed("menu_start")) {
			SetProcess(false);
			GameManager.Instance.SelectArena(arenas[cursor]);
			return;
		}

		for (int p = 1; p <= 2; p++) {
			string prefix = $"p{p}_";
			if (Input.IsActionJustPressed(prefix + "move_left")) Move(-1);
			if (Input.IsActionJustPressed(prefix + "move_right")) Move(1);

			if (Input.IsActionJustPressed(prefix + "attack")) {
				SetProcess(false);
				GameManager.Instance.SelectArena(arenas[cursor]);
				return;
			}
			if (Input.IsActionJustPressed(prefix + "jump")) {
				SetProcess(false);
				SceneFlow.Instance.GoTo(GameManager.CharacterSelectScene);
				return;
			}
		}
	}

	private void Move(int direction) {
		cursor = (cursor + direction + arenas.Count) % arenas.Count;
		Refresh();
	}

	private void Refresh() {
		for (int i = 0; i < cards.Count; i++) {
			bool selected = i == cursor;
			cards[i].Modulate = selected ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
		}
		mapName.Text = arenas[cursor].DisplayName;
	}
}
