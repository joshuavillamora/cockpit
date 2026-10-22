using Godot;
using System.Collections.Generic;

// Each player has their own column of chickens (P1 left, P2 right), moves with up/down,
// confirms with attack and cancels with jump. The picks are shown in the middle.
// Positions and sizes follow the designer's final wireframe (frame 11: portraits 64x64, frames 72x72,
// one ability + passive info panel per player). The slot under each preview shows the chicken's name,
// then READY. The keys are shown as keycaps between the two previews; once both are ready they
// change to ENTER: PLAY. When a player confirms, their selection frames flash dark (ui_dark set).
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
	private const float PulseSpeed = 5f;
	private static readonly Color[] PlayerColors = { new(0.98f, 0.84f, 0.58f), new(0.8f, 0.8f, 0.8f) };   // from the frames
	private static readonly Color NameColor = new(1f, 0.85f, 0.3f);
	private static readonly Color ReadyColor = new(0.6f, 1f, 0.6f);

	private readonly List<CharacterData> characters = new();
	private readonly List<TextureRect>[] slotFrames = { new(), new() };
	private readonly TextureRect[] previewFrames = new TextureRect[2];
	private readonly TextureRect[] previews = new TextureRect[2];
	private readonly int[] cursor = new int[2];
	private readonly bool[] confirmed = new bool[2];
	private readonly Label[][] infoLabels = { new Label[4], new Label[4] };   // ability name, text, passive name, text
	private Label[] readyLabels;
	private Control[] keyHints;
	private Control playHint;

	public override void _Ready() {
		readyLabels = new[] { GetNode<Label>("%P1Ready"), GetNode<Label>("%P2Ready") };
		BuildHints(GetNode<Control>("%Hints"));

		foreach (string path in CharacterPaths) characters.Add(GD.Load<CharacterData>(path));

		for (int p = 0; p < 2; p++) {
			BuildColumn(p);
			BuildPreview(p);
			BuildInfoPanel(p, GetNode<Control>(p == 0 ? "%P1Info" : "%P2Info"));
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
			readyLabels[p].Text = confirmed[p] ? "READY" : current.DisplayName;
			readyLabels[p].Modulate = confirmed[p] ? ReadyColor : Colors.White;

			infoLabels[p][0].Text = current.AbilityName;
			infoLabels[p][1].Text = current.AbilityText;
			infoLabels[p][2].Text = current.PassiveName;
			infoLabels[p][3].Text = current.PassiveText;
		}

		bool bothReady = confirmed[0] && confirmed[1];
		foreach (Control hint in keyHints) hint.Visible = !bothReady;
		playHint.Visible = bothReady;
		playHint.Modulate = new Color(1, 1, 1, 0.65f + 0.35f * Mathf.Sin((float)Time.GetTicksMsec() / 1000f * PulseSpeed));
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

	// ABILITY and PASSIVE tags, each followed by two lines: the name, then a short description
	private void BuildInfoPanel(int player, Control panel) {
		string[] tags = { "ABILITY", "PASSIVE" };
		string[] tagArt = { "res://assets/ui/menus/ui_tag_ability.png", "res://assets/ui/menus/ui_tag_passive.png" };
		for (int t = 0; t < 2; t++) {
			float top = 14 + t * 44;
			var tag = new NinePatchRect {
				Texture = GD.Load<Texture2D>(tagArt[t]),
				PatchMarginLeft = 4, PatchMarginRight = 4, PatchMarginTop = 4, PatchMarginBottom = 4,
				Position = new Vector2(14, top),
				Size = new Vector2(68, 12),   // a little wider than the 56 px art so the font fits
			};
			var tagLabel = new Label { Text = tags[t], HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			tagLabel.AddThemeConstantOverride("outline_size", 0);
			tagLabel.SetAnchorsPreset(LayoutPreset.FullRect);
			tag.AddChild(tagLabel);
			panel.AddChild(tag);

			var name = new Label { Position = new Vector2(14, top + 14), Size = new Vector2(164, 12), ClipText = true };
			name.AddThemeColorOverride("font_color", NameColor);
			var text = new Label { Position = new Vector2(14, top + 26), Size = new Vector2(164, 12), ClipText = true };
			panel.AddChild(name);
			panel.AddChild(text);
			infoLabels[player][t * 2] = name;
			infoLabels[player][t * 2 + 1] = text;
		}
	}

	// Key hints between the previews: a block per player, then ENTER: PLAY once both are ready
	private void BuildHints(Control area) {
		string[][] keys = { new[] { "W", "S", "J", "K" }, new[] { Keycap.Up, Keycap.Down, "M", "N" } };
		keyHints = new Control[2];
		for (int p = 0; p < 2; p++) {
			var block = new Control { Position = new Vector2(0, p * 42), Size = new Vector2(area.Size.X, 38) };
			AddHintRow(block, 0, p, $"P{p + 1}", (keys[p][0], null), (keys[p][1], "MOVE"));
			AddHintRow(block, 20, p, null, (keys[p][2], "OK"), (keys[p][3], "BACK"));
			area.AddChild(block);
			keyHints[p] = block;
		}

		playHint = new Control { Position = new Vector2(0, 26), Size = new Vector2(area.Size.X, 16) };
		Keycap enter = Keycap.Create("ENTER");
		var play = new Label { Text = "PLAY", VerticalAlignment = VerticalAlignment.Center };
		play.AddThemeColorOverride("font_color", NameColor);
		float width = enter.Size.X + 6 + 32;
		enter.Position = new Vector2((area.Size.X - width) / 2f, 0);
		play.Position = new Vector2(enter.Position.X + enter.Size.X + 6, 0);
		play.Size = new Vector2(32, 16);
		playHint.AddChild(enter);
		playHint.AddChild(play);
		area.AddChild(playHint);
	}

	// One centred row: an optional player tag, then keycaps each optionally followed by a word,
	// with the words in the player's colour
	private static void AddHintRow(Control block, float y, int player, string tag, params (string Key, string Word)[] items) {
		var row = new HBoxContainer { Position = new Vector2(0, y), Size = new Vector2(block.Size.X, 16), Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 3);
		if (tag != null) row.AddChild(HintLabel(tag, player));
		for (int i = 0; i < items.Length; i++) {
			if (i > 0 && items[i - 1].Word != null) row.AddChild(new Control { CustomMinimumSize = new Vector2(4, 0) });
			row.AddChild(Keycap.Create(items[i].Key));
			if (items[i].Word != null) row.AddChild(HintLabel(items[i].Word, player));
		}
		block.AddChild(row);
	}

	private static Label HintLabel(string text, int player) {
		var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
		label.AddThemeColorOverride("font_color", PlayerColors[player]);
		return label;
	}

	private static Texture2D FrameTexture(int player, bool dark = false) {
		string path = dark
			? $"res://assets/ui/dark/ui_select_frame_p{player + 1}_dark.png"
			: $"res://assets/ui/hud/ui_select_frame_p{player + 1}.png";
		return GD.Load<Texture2D>(path);
	}
}
