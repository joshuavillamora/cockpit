using Godot;
using System.Collections.Generic;

// Each player has their own column of chickens (P1 left, P2 right), moves with up/down,
// confirms with attack and cancels with jump. The picks are shown in the middle.
// Positions and sizes follow the designer's wireframe (portraits 64x64, frames 72x72).
// When a player confirms, their selection frames flash to the dark version (ui_dark set).
public partial class CharacterSelect : Control {
	private static readonly string[] CharacterPaths = {
		"res://resources/characters/banaba.tres",
		"res://resources/characters/paraokan.tres",
		"res://resources/characters/joloano.tres",
		"res://resources/characters/da_chik.tres",
	};

	private static readonly float[] ColumnX = { 48f, 528f };
	private static readonly float[] PreviewX = { 176f, 400f };
	private const float SlotTop = 56f;
	private const float SlotPitch = 72f;
	private const float PreviewTop = 144f;
	private const float FrameMargin = 4f;      // the selection frame is the portrait plus 4 px on each side
	private const float FlashSeconds = 0.12f;

	private readonly List<CharacterData> characters = new();
	private readonly List<TextureRect>[] slotFrames = { new(), new() };
	private readonly TextureRect[] previewFrames = new TextureRect[2];
	private readonly TextureRect[] previews = new TextureRect[2];
	private readonly int[] cursor = new int[2];
	private readonly bool[] confirmed = new bool[2];
	private Label[] names;
	private Label[] readyLabels;
	private Button playButton;

	public override void _Ready() {
		names = new[] { GetNode<Label>("%P1Name"), GetNode<Label>("%P2Name") };
		readyLabels = new[] { GetNode<Label>("%P1Ready"), GetNode<Label>("%P2Ready") };
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
				if (!confirmed[p]) {
					confirmed[p] = true;
					FlashFrames(p);
				}
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
			readyLabels[p].Text = confirmed[p] ? "READY" : "CHOOSE";
			readyLabels[p].Modulate = confirmed[p] ? new Color(0.6f, 1f, 0.6f) : Colors.White;
		}

		playButton.Visible = confirmed[0] && confirmed[1];
	}

	private void GoToMapSelect() {
		SetProcess(false);
		GameManager.Instance.SelectCharacters(characters[cursor[0]], characters[cursor[1]]);
	}

	// Show the dark frame for a moment on the preview and on the highlighted slot, then back to light
	private async void FlashFrames(int player) {
		TextureRect slotFrame = slotFrames[player][cursor[player]];
		Texture2D dark = FrameTexture(player, dark: true);
		Texture2D light = FrameTexture(player);

		previewFrames[player].Texture = dark;
		slotFrame.Texture = dark;
		await ToSignal(GetTree().CreateTimer(FlashSeconds), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(this)) return;   // left the screen in the meantime

		previewFrames[player].Texture = light;
		slotFrame.Texture = light;
	}

	// Vertical list of all chickens for one player
	private void BuildColumn(int player) {
		for (int i = 0; i < characters.Count; i++) {
			CharacterData data = characters[i];
			var slot = new Control {
				Position = new Vector2(ColumnX[player], SlotTop + i * SlotPitch),
				Size = new Vector2(64, 64),
			};
			AddChild(slot);

			var portrait = new TextureRect { Texture = data.Portrait };
			if (!data.Playable) portrait.Modulate = new Color(0.35f, 0.35f, 0.35f);
			slot.AddChild(portrait);

			var frame = new TextureRect {
				Texture = FrameTexture(player),
				Position = new Vector2(-FrameMargin, -FrameMargin),
			};
			slot.AddChild(frame);
			slotFrames[player].Add(frame);

			if (!data.Playable) {
				slot.AddChild(new Label {
					Text = "SOON",
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Size = new Vector2(64, 64),
				});
			}
		}
	}

	// The currently highlighted chicken, shown between the two columns
	private void BuildPreview(int player) {
		previews[player] = new TextureRect { Position = new Vector2(PreviewX[player], PreviewTop) };
		AddChild(previews[player]);

		previewFrames[player] = new TextureRect {
			Texture = FrameTexture(player),
			Position = new Vector2(PreviewX[player] - FrameMargin, PreviewTop - FrameMargin),
		};
		AddChild(previewFrames[player]);
	}

	private static Texture2D FrameTexture(int player, bool dark = false) {
		string path = dark
			? $"res://assets/ui/dark/ui_select_frame_p{player + 1}_dark.png"
			: $"res://assets/ui/hud/ui_select_frame_p{player + 1}.png";
		return GD.Load<Texture2D>(path);
	}
}
