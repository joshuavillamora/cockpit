using Godot;
using System.Collections.Generic;

// Each player has their own column of chickens (P1 left, P2 right), moves with up/down,
// confirms with attack and cancels with jump. The picks are shown big in the middle.
public partial class CharacterSelect : Control {
	private static readonly string[] CharacterPaths = {
		"res://resources/characters/banaba.tres",
		"res://resources/characters/paraokan.tres",
		"res://resources/characters/joloano.tres",
		"res://resources/characters/da_chik.tres",
	};

	private static readonly float[] ColumnX = { 40f, 536f };
	private static readonly float[] PreviewX = { 149f, 379f };
	private const float SlotTop = 62f;
	private const float SlotPitch = 66f;

	private readonly List<CharacterData> characters = new();
	private readonly List<TextureRect>[] slotFrames = { new(), new() };
	private readonly int[] cursor = new int[2];
	private readonly bool[] confirmed = new bool[2];
	private readonly TextureRect[] previews = new TextureRect[2];
	private Label[] names;
	private Button[] confirmButtons;
	private Button playButton;

	public override void _Ready() {
		names = new[] { GetNode<Label>("%P1Name"), GetNode<Label>("%P2Name") };
		confirmButtons = new[] { GetNode<Button>("%P1Confirm"), GetNode<Button>("%P2Confirm") };
		playButton = GetNode<Button>("%PlayButton");

		foreach (string path in CharacterPaths) characters.Add(GD.Load<CharacterData>(path));

		for (int p = 0; p < 2; p++) {
			BuildColumn(p);
			BuildPreview(p);
		}

		cursor[0] = FirstPlayable();
		cursor[1] = LastPlayable();
		Refresh();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			SceneFlow.Instance.GoTo(GameManager.MainMenuScene);
		}
	}

	public override void _Process(double delta) {
		// Enter moves on once both players have confirmed
		if (confirmed[0] && confirmed[1] && Input.IsActionJustPressed("menu_start")) {
			GoToMapSelect();
			return;
		}

		for (int p = 0; p < 2; p++) {
			string prefix = $"p{p + 1}_";

			if (!confirmed[p]) {
				if (Input.IsActionJustPressed(prefix + "up")) Move(p, -1);
				if (Input.IsActionJustPressed(prefix + "down")) Move(p, 1);
			}

			if (Input.IsActionJustPressed(prefix + "attack")) {
				confirmed[p] = true;
			} else if (Input.IsActionJustPressed(prefix + "jump")) {
				confirmed[p] = false;
			}
		}

		Refresh();
	}

	private void Move(int player, int direction) {
		int count = characters.Count;
		int index = cursor[player];
		for (int step = 0; step < count; step++) {
			index = (index + direction + count) % count;
			if (characters[index].Playable) {
				cursor[player] = index;
				return;
			}
		}
	}

	private int FirstPlayable() {
		for (int i = 0; i < characters.Count; i++) if (characters[i].Playable) return i;
		return 0;
	}

	private int LastPlayable() {
		for (int i = characters.Count - 1; i >= 0; i--) if (characters[i].Playable) return i;
		return 0;
	}

	private void Refresh() {
		for (int p = 0; p < 2; p++) {
			for (int i = 0; i < characters.Count; i++) {
				TextureRect frame = slotFrames[p][i];
				frame.Visible = cursor[p] == i;
				frame.Modulate = new Color(1, 1, 1, confirmed[p] ? 1f : 0.6f);
			}

			CharacterData current = characters[cursor[p]];
			previews[p].Texture = current.Portrait;
			names[p].Text = current.DisplayName;
			confirmButtons[p].Text = confirmed[p] ? "READY!" : "CONFIRM";
			confirmButtons[p].Modulate = confirmed[p] ? new Color(0.6f, 1f, 0.6f) : Colors.White;
		}

		playButton.Visible = confirmed[0] && confirmed[1];
	}

	private void GoToMapSelect() {
		SetProcess(false);
		GameManager.Instance.SelectCharacters(characters[cursor[0]], characters[cursor[1]]);
	}

	// Vertical list of all chickens for one player
	private void BuildColumn(int player) {
		for (int i = 0; i < characters.Count; i++) {
			CharacterData data = characters[i];
			var slot = new Control {
				Position = new Vector2(ColumnX[player], SlotTop + i * SlotPitch),
				Size = new Vector2(64, 56),
			};
			AddChild(slot);

			var portrait = new TextureRect { Texture = data.Portrait, Position = new Vector2(4, 4) };
			if (!data.Playable) portrait.Modulate = new Color(0.35f, 0.35f, 0.35f);
			slot.AddChild(portrait);

			var frame = new TextureRect { Texture = FrameTexture(player), Size = new Vector2(64, 56) };
			slot.AddChild(frame);
			slotFrames[player].Add(frame);

			if (!data.Playable) {
				var soon = new Label {
					Text = "SOON",
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Size = new Vector2(64, 56),
				};
				soon.AddThemeFontSizeOverride("font_size", 16);
				slot.AddChild(soon);
			}
		}
	}

	// Big close-up of the currently highlighted chicken
	private void BuildPreview(int player) {
		var frame = new TextureRect {
			Texture = FrameTexture(player),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			Position = new Vector2(PreviewX[player] - 8, 88),
			Size = new Vector2(128, 112),
		};
		AddChild(frame);

		previews[player] = new TextureRect {
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			Position = new Vector2(PreviewX[player], 96),
			Size = new Vector2(112, 96),
		};
		AddChild(previews[player]);
	}

	private static Texture2D FrameTexture(int player) {
		return GD.Load<Texture2D>($"res://assets/ui/hud/ui_select_frame_p{player + 1}.png");
	}
}
