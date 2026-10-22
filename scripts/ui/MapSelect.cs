using Godot;
using System.Collections.Generic;

// Either player can move the cursor and continue (the arena is a shared choice).
// Layout follows the designer's wireframe: two 256x144 thumbnails with the name under each.
public partial class MapSelect : Control {
	private static readonly string[] ArenaPaths = {
		"res://resources/arenas/backyard.tres",
		"res://resources/arenas/backyard_night.tres",
	};

	private const float CardTop = 100f;
	private const float CardGap = 24f;
	private const float FrameMargin = 4f;   // the frame is the thumbnail plus 4 px on each side

	private readonly List<ArenaData> arenas = new();
	private readonly List<Control[]> cardImages = new();   // thumbnail + frame per card
	private readonly List<Label> cardNames = new();
	private int cursor;

	public override void _Ready() {
		var frameTexture = GD.Load<Texture2D>("res://assets/ui/menus/ui_map_frame.png");

		foreach (string path in ArenaPaths) arenas.Add(GD.Load<ArenaData>(path));

		// Cards are centered as a row: [frame] gap [frame] ...
		float cardWidth = frameTexture.GetWidth();
		float left = (640f - (arenas.Count * cardWidth + (arenas.Count - 1) * CardGap)) / 2f;
		for (int i = 0; i < arenas.Count; i++) {
			var card = new Control {
				Position = new Vector2(left + i * (cardWidth + CardGap), CardTop),
				Size = frameTexture.GetSize(),
			};
			var thumbnail = new TextureRect { Texture = arenas[i].Thumbnail, Position = new Vector2(FrameMargin, FrameMargin) };
			var frame = new TextureRect { Texture = frameTexture };
			card.AddChild(thumbnail);
			card.AddChild(frame);
			var name = new Label {
				Text = arenas[i].DisplayName,
				HorizontalAlignment = HorizontalAlignment.Center,
				Position = new Vector2(0, frameTexture.GetHeight() + 4),
				Size = new Vector2(cardWidth, 24),
			};
			card.AddChild(name);
			AddChild(card);
			cardImages.Add(new Control[] { thumbnail, frame });
			cardNames.Add(name);
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
		for (int i = 0; i < cardImages.Count; i++) {
			bool selected = i == cursor;
			foreach (Control image in cardImages[i]) image.Modulate = selected ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
			cardNames[i].Modulate = selected ? Colors.White : new Color(0.75f, 0.75f, 0.75f);   // names stay readable
		}
	}
}
