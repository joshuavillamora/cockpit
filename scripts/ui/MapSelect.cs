using Godot;
using System.Collections.Generic;

// Either player can move the cursor and continue (the arena is a shared choice).
// Layout follows the team's map selection design: three framed thumbnails with a numbered name box
// under each, and an instruction bar along the bottom. Number keys 1-3 jump to a map.
public partial class MapSelect : Control {
	private static readonly string[] ArenaPaths = {
		"res://resources/arenas/thepit_day.tres",
		"res://resources/arenas/thepit_night.tres",
		"res://resources/arenas/death_chamber.tres",
	};

	private static readonly Vector2 ThumbSize = new(168, 104);
	private const float CardTop = 96f;
	private const float CardGap = 16f;
	private const float FrameMargin = 4f;    // the frame is the thumbnail plus 4 px on each side
	private const float NameHeight = 24f;
	private static readonly Color Gold = new(1f, 0.85f, 0.3f);
	private static readonly Color Dimmed = new(0.5f, 0.5f, 0.5f);

	private readonly List<ArenaData> arenas = new();
	private readonly List<Control> cards = new();
	private readonly List<Label> cardNames = new();
	private int cursor;

	public override void _Ready() {
		var frameTexture = GD.Load<Texture2D>("res://assets/ui/menus/ui_map_frame.png");
		var plateTexture = GD.Load<Texture2D>("res://assets/ui/hud/ui_counter_frame.png");

		foreach (string path in ArenaPaths) arenas.Add(GD.Load<ArenaData>(path));

		// Cards are centred as a row: [frame] gap [frame] gap [frame]
		Vector2 frameSize = ThumbSize + new Vector2(FrameMargin * 2, FrameMargin * 2);
		float left = (640f - (arenas.Count * frameSize.X + (arenas.Count - 1) * CardGap)) / 2f;
		for (int i = 0; i < arenas.Count; i++) {
			var card = new Control {
				Position = new Vector2(left + i * (frameSize.X + CardGap), CardTop),
				Size = frameSize + new Vector2(0, NameHeight),
				MouseFilter = MouseFilterEnum.Ignore,
			};
			card.AddChild(new TextureRect {
				Texture = arenas[i].Thumbnail,
				Position = new Vector2(FrameMargin, FrameMargin),
				Size = ThumbSize,
				MouseFilter = MouseFilterEnum.Ignore,
			});
			card.AddChild(new NinePatchRect {
				Texture = frameTexture,
				Size = frameSize,
				PatchMarginLeft = 8, PatchMarginTop = 8, PatchMarginRight = 8, PatchMarginBottom = 8,
				DrawCenter = false,
				MouseFilter = MouseFilterEnum.Ignore,
			});
			// Numbered name box under the picture
			var plate = new NinePatchRect {
				Texture = plateTexture,
				Position = new Vector2(0, frameSize.Y - 2),
				Size = new Vector2(frameSize.X, NameHeight),
				PatchMarginLeft = 8, PatchMarginTop = 8, PatchMarginRight = 8, PatchMarginBottom = 8,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			var name = new Label {
				Text = $"{i + 1}. {arenas[i].DisplayName}",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Size = plate.Size,
			};
			plate.AddChild(name);
			card.AddChild(plate);
			AddChild(card);
			cards.Add(card);
			cardNames.Add(name);
		}

		BuildInstructions(GetNode<HBoxContainer>("%Instructions"));
		Refresh();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			AudioManager.Instance?.PlaySfx(Sfx.MenuBack);
			SceneFlow.Instance.GoTo(GameManager.CharacterSelectScene);
		} else if (@event is InputEventKey { Pressed: true, Echo: false } key && key.Keycode >= Key.Key1 && key.Keycode < Key.Key1 + arenas.Count) {
			GetViewport().SetInputAsHandled();
			MoveTo((int)(key.Keycode - Key.Key1));
		}
	}

	public override void _Process(double delta) {
		if (Input.IsActionJustPressed("menu_start")) {
			AudioManager.Instance?.PlaySfx(Sfx.MenuConfirm);
			SetProcess(false);
			GameManager.Instance.SelectArena(arenas[cursor]);
			return;
		}

		for (int p = 1; p <= 2; p++) {
			string prefix = $"p{p}_";
			if (Input.IsActionJustPressed(prefix + "move_left")) MoveTo((cursor - 1 + arenas.Count) % arenas.Count);
			if (Input.IsActionJustPressed(prefix + "move_right")) MoveTo((cursor + 1) % arenas.Count);

			if (Input.IsActionJustPressed(prefix + "attack")) {
				AudioManager.Instance?.PlaySfx(Sfx.MenuConfirm);
				SetProcess(false);
				GameManager.Instance.SelectArena(arenas[cursor]);
				return;
			}
			if (Input.IsActionJustPressed(prefix + "jump")) {
				AudioManager.Instance?.PlaySfx(Sfx.MenuBack);
				SetProcess(false);
				SceneFlow.Instance.GoTo(GameManager.CharacterSelectScene);
				return;
			}
		}
	}

	private void MoveTo(int index) {
		if (index == cursor) return;
		cursor = index;
		AudioManager.Instance?.PlaySfx(Sfx.SelectMove, 0.05f);
		Refresh();
	}

	private void Refresh() {
		for (int i = 0; i < cards.Count; i++) {
			bool selected = i == cursor;
			cards[i].Modulate = selected ? Colors.White : Dimmed;
			cardNames[i].AddThemeColorOverride("font_color", selected ? Gold : Colors.White);
		}
	}

	// Arrow keys MOVE, ENTER CONFIRM, ESC BACK, 1 2 3 JUMP TO MAP
	private static void BuildInstructions(HBoxContainer bar) {
		void Keys(string word, params string[] keys) {
			foreach (string k in keys) {
				Keycap cap = Keycap.Create(k);
				cap.SizeFlagsVertical = SizeFlags.ShrinkCenter;   // keep the 16 px cap height in the taller bar
				bar.AddChild(cap);
			}
			bar.AddChild(new Label { Text = word, VerticalAlignment = VerticalAlignment.Center });
		}
		void Gap() => bar.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });

		Keys("MOVE", Keycap.Left, Keycap.Right);
		Gap();
		Keys("CONFIRM", "ENTER");
		Gap();
		Keys("BACK", "ESC");
		Gap();
		Keys("JUMP TO MAP", "1", "2", "3");
	}
}
