using Godot;
using System.Collections.Generic;

// Each player has their own column of chickens (P1 left, P2 right), moves with up/down,
// confirms with attack and cancels with jump. The picks stand on name boxes in the middle ("box vs box").
// Layout follows the approved redesign mockup (canvas "Character Select Redesign", mockup px / 3):
// slots 64x64 with frames 72x72, a 128x128 chicken whose feet sit on its name box, a controls bar per
// player (keycaps above MOVE / OK / BACK), and one ability + passive panel per player.
// While a player is confirmed their MOVE and OK keys fade; once both are ready the bars give way to ENTER: PLAY.
// When a player confirms, their selection frames flash dark (ui_dark set).
public partial class CharacterSelect : Control {
	private static readonly string[] CharacterPaths = {
		"res://resources/characters/banaba.tres",
		"res://resources/characters/paraokan.tres",
		"res://resources/characters/joloano.tres",
		"res://resources/characters/da_chik.tres",
	};

	private static readonly float[] ColumnX = { 40f, 536f };
	private const float SlotTop = 56f;
	private const float SlotPitch = 72f;
	private const float SlotSize = 64f;
	private const float FrameMargin = 4f;      // the selection frame is the portrait plus 4 px on each side
	private const float FlashSeconds = 0.12f;
	private const float PulseSpeed = 5f;
	private const float LockedAlpha = 0.3f;    // MOVE and OK while the player is confirmed

	// Stage layout, relative to %P1Stage / %P2Stage (192 wide, top at y 44)
	private const float StageWidth = 192f;
	private const float SpriteSize = 128f;
	private const float SpriteTop = -18f;      // the sprite's bottom row (the feet) lands on the box top
	private const float BoxWidth = 112f;
	private const float BoxTop = SpriteTop + SpriteSize;
	private const float BarTop = 148f;
	private const float BarHeight = 48f;

	private static readonly Color[] PlayerColors = { new(0.98f, 0.84f, 0.58f), new(0.8f, 0.8f, 0.8f) };   // from the frames
	private static readonly Color[] LidColors = { new(0.77f, 0.54f, 0.24f), new(0.52f, 0.55f, 0.57f) };
	private static readonly Color NameColor = new(1f, 0.85f, 0.3f);
	private static readonly Color ReadyColor = new(0.6f, 1f, 0.6f);
	private static readonly Color Cream = new(0.93f, 0.89f, 0.78f);
	private static readonly Color Muted = new(0.54f, 0.59f, 0.61f);
	private static readonly Color Ink = new(0.05f, 0.067f, 0.078f);
	private static readonly Color Fill = new(0.094f, 0.125f, 0.141f);
	private static readonly Color Edge = new(0.165f, 0.196f, 0.212f);
	private static readonly Color BoxFront = new(0.106f, 0.133f, 0.149f);

	private readonly List<CharacterData> characters = new();
	private readonly List<TextureRect>[] slotFrames = { new(), new() };
	private readonly TextureRect[] previews = new TextureRect[2];
	private readonly Label[] statusLabels = new Label[2];
	private readonly Label[] nameLabels = new Label[2];
	private readonly Control[] keyBars = new Control[2];
	private readonly Control[][] lockedKeys = { new Control[2], new Control[2] };   // MOVE and OK columns
	private readonly int[] cursor = new int[2];
	private readonly bool[] confirmed = new bool[2];
	private readonly Label[][] infoLabels = { new Label[4], new Label[4] };   // ability name, text, passive name, text
	private Control playHint;

	public override void _Ready() {
		foreach (string path in CharacterPaths) characters.Add(GD.Load<CharacterData>(path));

		BuildEscHint(GetNode<Control>("%EscHint"));
		for (int p = 0; p < 2; p++) {
			BuildColumn(p);
			BuildStage(p, GetNode<Control>(p == 0 ? "%P1Stage" : "%P2Stage"));
			BuildInfoPanel(p, GetNode<Control>(p == 0 ? "%P1Info" : "%P2Info"));
		}
		BuildPlayHint();

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
		bool bothReady = confirmed[0] && confirmed[1];

		for (int p = 0; p < 2; p++) {
			for (int i = 0; i < characters.Count; i++) {
				TextureRect frame = slotFrames[p][i];
				frame.Visible = cursor[p] == i;
				frame.Modulate = new Color(1, 1, 1, confirmed[p] ? 1f : 0.6f);
			}

			CharacterData current = characters[cursor[p]];
			previews[p].Texture = SpriteOf(current);
			nameLabels[p].Text = current.DisplayName;
			statusLabels[p].Text = confirmed[p] ? "READY" : "CHOOSING...";
			statusLabels[p].AddThemeColorOverride("font_color", confirmed[p] ? ReadyColor : Cream);

			keyBars[p].Visible = !bothReady;
			foreach (Control column in lockedKeys[p]) column.Modulate = new Color(1, 1, 1, confirmed[p] ? LockedAlpha : 1f);

			infoLabels[p][0].Text = current.AbilityName;
			infoLabels[p][1].Text = current.AbilityText;
			infoLabels[p][2].Text = current.PassiveName;
			infoLabels[p][3].Text = current.PassiveText;
		}

		playHint.Visible = bothReady;
		playHint.Modulate = new Color(1, 1, 1, 0.65f + 0.35f * Mathf.Sin((float)Time.GetTicksMsec() / 1000f * PulseSpeed));
	}

	private void GoToMapSelect() {
		SetProcess(false);
		GameManager.Instance.SelectCharacters(characters[cursor[0]], characters[cursor[1]]);
	}

	// Show the dark frame for a moment on the highlighted slot, then back to light
	private async void FlashFrames(int player) {
		TextureRect slotFrame = slotFrames[player][cursor[player]];
		Texture2D dark = FrameTexture(player, dark: true);
		Texture2D light = FrameTexture(player);

		slotFrame.Texture = dark;
		await ToSignal(GetTree().CreateTimer(FlashSeconds), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(this)) return;   // left the screen in the meantime

		slotFrame.Texture = light;
	}

	// Vertical list of all chickens for one player. P2's column is mirrored so both face the middle.
	private void BuildColumn(int player) {
		for (int i = 0; i < characters.Count; i++) {
			CharacterData data = characters[i];
			var slot = new Panel {
				Position = new Vector2(ColumnX[player], SlotTop + i * SlotPitch),
				Size = new Vector2(SlotSize, SlotSize),
				MouseFilter = MouseFilterEnum.Ignore,
			};
			slot.AddThemeStyleboxOverride("panel", Box(Fill, Edge, 2));
			AddChild(slot);

			if (data.Playable) {
				slot.AddChild(new TextureRect { Texture = SpriteOf(data), FlipH = player == 1 });
			} else {
				// No placeholder art underneath: a dim "?" and a SOON strip along the bottom
				var mark = MakeLabel("?", 32, new Color(0.23f, 0.27f, 0.29f));
				mark.HorizontalAlignment = HorizontalAlignment.Center;
				mark.VerticalAlignment = VerticalAlignment.Center;
				mark.Size = new Vector2(SlotSize, SlotSize - 14);
				slot.AddChild(mark);

				var strip = new ColorRect { Color = Ink, Position = new Vector2(0, SlotSize - 14), Size = new Vector2(SlotSize, 14) };
				var soon = MakeLabel("SOON", 16, Muted);
				soon.HorizontalAlignment = HorizontalAlignment.Center;
				soon.VerticalAlignment = VerticalAlignment.Center;
				soon.SetAnchorsPreset(LayoutPreset.FullRect);
				strip.AddChild(soon);
				slot.AddChild(strip);
			}

			var frame = new TextureRect {
				Texture = FrameTexture(player),
				Position = new Vector2(-FrameMargin, -FrameMargin),
			};
			slot.AddChild(frame);
			slotFrames[player].Add(frame);
		}
	}

	// Status row, the chicken standing on its name box, and the controls bar under it
	private void BuildStage(int player, Control stage) {
		// P1 [chip] READY  /  CHOOSING... [chip] P2
		var row = new HBoxContainer { Size = new Vector2(StageWidth, 14), Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 6);
		var chip = new PanelContainer();
		var chipStyle = Box(PlayerColors[player], PlayerColors[player], 0);
		chipStyle.ContentMarginLeft = chipStyle.ContentMarginRight = 3;
		chip.AddThemeStyleboxOverride("panel", chipStyle);
		var chipLabel = MakeLabel($"P{player + 1}", 16, new Color(0.1f, 0.08f, 0.06f));
		chipLabel.VerticalAlignment = VerticalAlignment.Center;
		chip.AddChild(chipLabel);
		statusLabels[player] = MakeLabel("", 16, Cream, shadow: true);
		statusLabels[player].VerticalAlignment = VerticalAlignment.Center;
		if (player == 0) { row.AddChild(chip); row.AddChild(statusLabels[player]); }
		else { row.AddChild(statusLabels[player]); row.AddChild(chip); }
		stage.AddChild(row);

		// The name box: thin lid line, darker lid, a foot shadow, then the dark front with the name
		float boxX = (StageWidth - BoxWidth) / 2f;
		stage.AddChild(new ColorRect { Color = new Color(Ink, 0.5f), Position = new Vector2(boxX + 4, BoxTop + 32), Size = new Vector2(BoxWidth - 8, 4) });
		stage.AddChild(new ColorRect { Color = PlayerColors[player], Position = new Vector2(boxX, BoxTop), Size = new Vector2(BoxWidth, 2) });
		stage.AddChild(new ColorRect { Color = LidColors[player], Position = new Vector2(boxX, BoxTop + 2), Size = new Vector2(BoxWidth, 6) });
		stage.AddChild(new ColorRect { Color = new Color(Ink, 0.45f), Position = new Vector2(boxX + 32, BoxTop + 2), Size = new Vector2(48, 4) });
		var front = new Panel { Position = new Vector2(boxX, BoxTop + 8), Size = new Vector2(BoxWidth, 24), MouseFilter = MouseFilterEnum.Ignore };
		var frontStyle = Box(BoxFront, Ink, 2);
		frontStyle.BorderWidthTop = 0;
		front.AddThemeStyleboxOverride("panel", frontStyle);
		nameLabels[player] = MakeLabel("", 16, NameColor, shadow: true);
		nameLabels[player].HorizontalAlignment = HorizontalAlignment.Center;
		nameLabels[player].VerticalAlignment = VerticalAlignment.Center;
		nameLabels[player].SetAnchorsPreset(LayoutPreset.FullRect);
		front.AddChild(nameLabels[player]);
		stage.AddChild(front);

		// Drawn after the box so the feet sit on top of the lid
		previews[player] = new TextureRect {
			Position = new Vector2((StageWidth - SpriteSize) / 2f, SpriteTop),
			Size = new Vector2(SpriteSize, SpriteSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			FlipH = player == 1,
		};
		stage.AddChild(previews[player]);

		keyBars[player] = BuildKeyBar(player);
		stage.AddChild(keyBars[player]);
	}

	// Three even columns, keycaps above the word: MOVE / OK / BACK in the player's colour
	private Control BuildKeyBar(int player) {
		string[][] keys = { new[] { "W", "S", "J", "K" }, new[] { Keycap.Up, Keycap.Down, "M", "N" } };
		var bar = new Panel { Position = new Vector2(0, BarTop), Size = new Vector2(StageWidth, BarHeight), MouseFilter = MouseFilterEnum.Ignore };
		bar.AddThemeStyleboxOverride("panel", Box(Fill, Edge, 2));

		var columns = new HBoxContainer { Position = new Vector2(8, 6), Size = new Vector2(StageWidth - 16, BarHeight - 12) };
		columns.AddThemeConstantOverride("separation", 0);
		bar.AddChild(columns);

		(string[] Keys, string Word)[] groups = {
			(new[] { keys[player][0], keys[player][1] }, "MOVE"),
			(new[] { keys[player][2] }, "OK"),
			(new[] { keys[player][3] }, "BACK"),
		};
		for (int g = 0; g < groups.Length; g++) {
			var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
			column.AddThemeConstantOverride("separation", 4);
			var caps = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			caps.AddThemeConstantOverride("separation", 2);
			foreach (string key in groups[g].Keys) caps.AddChild(Keycap.Create(key));
			var word = MakeLabel(groups[g].Word, 16, PlayerColors[player]);
			word.HorizontalAlignment = HorizontalAlignment.Center;
			column.AddChild(caps);
			column.AddChild(word);
			columns.AddChild(column);
			if (g < 2) lockedKeys[player][g] = column;
		}
		return bar;
	}

	// Once both are ready: one yellow-framed box with ENTER and PLAY! between the boxes
	private void BuildPlayHint() {
		playHint = new Panel { Position = new Vector2(220, 200), Size = new Vector2(200, 34), MouseFilter = MouseFilterEnum.Ignore };
		playHint.AddThemeStyleboxOverride("panel", Box(Fill, NameColor, 2));
		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 8);
		row.SetAnchorsPreset(LayoutPreset.FullRect);
		var enter = Keycap.Create("ENTER");
		enter.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		var play = MakeLabel("PLAY!", 24, NameColor, shadow: true);
		play.VerticalAlignment = VerticalAlignment.Center;
		row.AddChild(enter);
		row.AddChild(play);
		playHint.AddChild(row);
		AddChild(playHint);
	}

	private static void BuildEscHint(Control area) {
		Keycap esc = Keycap.Create("ESC");
		area.AddChild(esc);
		var menu = MakeLabel("MENU", 16, Muted);
		menu.Position = new Vector2(esc.Size.X + 4, 0);
		menu.Size = new Vector2(40, 16);
		menu.VerticalAlignment = VerticalAlignment.Center;
		area.AddChild(menu);
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

			var name = MakeLabel("", 16, NameColor);
			name.Position = new Vector2(14, top + 14);
			name.Size = new Vector2(164, 12);
			name.ClipText = true;
			var text = MakeLabel("", 16, Cream);
			text.Position = new Vector2(14, top + 26);
			text.Size = new Vector2(164, 12);
			text.ClipText = true;
			panel.AddChild(name);
			panel.AddChild(text);
			infoLabels[player][t * 2] = name;
			infoLabels[player][t * 2 + 1] = text;
		}
	}

	// A label without the theme's black outline, optionally with the mockup's 1 px dark drop shadow
	private static Label MakeLabel(string text, int size, Color color, bool shadow = false) {
		var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore };
		label.AddThemeConstantOverride("outline_size", 0);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		if (shadow) {
			label.AddThemeColorOverride("font_shadow_color", Ink);
			label.AddThemeConstantOverride("shadow_offset_x", 1);
			label.AddThemeConstantOverride("shadow_offset_y", 1);
		}
		return label;
	}

	private static StyleBoxFlat Box(Color fill, Color border, int width) {
		var style = new StyleBoxFlat { BgColor = fill, BorderColor = border };
		style.SetBorderWidthAll(width);
		return style;
	}

	private static Texture2D SpriteOf(CharacterData data) => data.SelectSprite ?? data.Portrait;

	private static Texture2D FrameTexture(int player, bool dark = false) {
		string path = dark
			? $"res://assets/ui/dark/ui_select_frame_p{player + 1}_dark.png"
			: $"res://assets/ui/hud/ui_select_frame_p{player + 1}.png";
		return GD.Load<Texture2D>(path);
	}
}
