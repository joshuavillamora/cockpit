using Godot;
using System.Collections.Generic;
using System.Linq;

// Key rebinding popup, opened from Settings (it takes Settings' place; BACK / Esc returns to Settings).
// One column per player: each action has two key slots. Select a slot (ENTER / SPACE / click) and press
// the new key; ESC cancels. DEL / BACKSPACE clears a slot (an action always keeps one key).
// A key that another action already has moves to this one (InputBindings.Bind). Saved when the screen closes.
public partial class ControlsScreen : OverlayScreen {
	public const string ScenePath = "res://scenes/ui/screens/controls_screen.tscn";

	private const float LabelWidth = 92f;
	private static readonly Vector2 SlotSize = new(78, 18);
	private const float BlinkSpeed = 6f;
	private static readonly Color EmptyColor = new(0.45f, 0.45f, 0.42f);
	private static readonly Color Gold = new(1f, 0.85f, 0.3f);

	// Action name, P1 keys, P2 keys (current bindings). null = not in the game yet ("SOON").
	// The training lobby shows these rows (except PAUSE).
	public static (string Action, string[] P1, string[] P2)[] Rows => new[] {
		("MOVE", FirstKeys(1, "move_left", "move_right"), FirstKeys(2, "move_left", "move_right")),
		("UP/DOWN", FirstKeys(1, "up", "down"), FirstKeys(2, "up", "down")),
		("JUMP", AllKeys(1, "jump"), AllKeys(2, "jump")),
		("ATTACK", AllKeys(1, "attack"), AllKeys(2, "attack")),
		("BLOCK", AllKeys(1, "block"), AllKeys(2, "block")),
		("ABILITY", AllKeys(1, "ability"), AllKeys(2, "ability")),
		("PAUSE", new[] { "ESC", "P" }, new[] { "ESC", "P" }),
	};

	private readonly Dictionary<Button, (string Action, int Slot)> slots = new();
	private Button listening;
	private Label status;
	private Control hint;
	private double blink;

	public override void _Ready() {
		status = GetNode<Label>("%Status");
		hint = GetNode<Control>("%Hint");
		BuildHint();
		BuildColumn(GetNode<Control>("%P1Column"), 1);
		BuildColumn(GetNode<Control>("%P2Column"), 2);
		RefreshSlots();

		GetNode<Button>("%ResetButton").Pressed += () => {
			StopListening();
			InputBindings.Instance.ResetDefaults();
			RefreshSlots();
			ShowStatus("KEYS RESET TO DEFAULT");
		};
		GetNode<Button>("%BackButton").Pressed += Close;
		slots.Keys.First().GrabFocus();
	}

	public override void _Process(double delta) {
		if (listening == null) return;
		blink += delta;
		listening.Modulate = new Color(1, 1, 1, 0.55f + 0.45f * Mathf.Sin((float)blink * BlinkSpeed));
	}

	// While a slot waits for a key, every key press goes to it (so Esc / P don't close the screen or pause)
	public override void _Input(InputEvent @event) {
		if (listening == null || @event is not InputEventKey { Pressed: true, Echo: false } press) return;
		GetViewport().SetInputAsHandled();

		Key key = press.PhysicalKeycode != Key.None ? press.PhysicalKeycode : press.Keycode;
		if (key == Key.Escape) {
			StopListening();
			ShowHint();
			return;
		}
		if (InputBindings.Instance.IsReserved(key)) {
			ShowStatus($"{InputBindings.Label(key)} IS RESERVED, TRY ANOTHER KEY");
			return;
		}

		var (action, slot) = slots[listening];
		string loser = InputBindings.Instance.Bind(action, slot, key);
		StopListening();
		RefreshSlots();
		if (loser != null) ShowStatus($"{KeyText(key)} MOVED FROM {InputBindings.ActionName(loser)}");
		else ShowHint();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (listening == null && @event is InputEventKey { Pressed: true, Echo: false } press
			&& press.Keycode is Key.Delete or Key.Backspace
			&& GetViewport().GuiGetFocusOwner() is Button focused && slots.TryGetValue(focused, out var target)) {
			GetViewport().SetInputAsHandled();
			if (InputBindings.Instance.Clear(target.Action, target.Slot)) {
				RefreshSlots();
				ShowHint();
			} else {
				ShowStatus("EVERY ACTION NEEDS AT LEAST ONE KEY");
			}
			return;
		}
		base._UnhandledInput(@event);
	}

	protected override void OnClosing() {
		InputBindings.Instance.Save();
	}

	// PLAYER N, one row per rebindable action (label + two slots), then PAUSE (read-only)
	private void BuildColumn(Control column, int player) {
		column.AddChild(new Label { Text = $"PLAYER {player}", HorizontalAlignment = HorizontalAlignment.Center });

		foreach (var (pattern, label) in InputBindings.Actions) {
			string action = string.Format(pattern, player);
			HBoxContainer row = Row(label);
			for (int s = 0; s < InputBindings.SlotCount; s++) {
				Button slot = MakeSlot();
				int slotIndex = s;
				slot.Pressed += () => StartListening(slot);
				slots[slot] = (action, slotIndex);
				row.AddChild(slot);
			}
			column.AddChild(row);
		}

		HBoxContainer pause = Row("PAUSE");
		foreach (string key in new[] { "ESC", "P" }) pause.AddChild(SlotContent(key, CenteredBox()));
		column.AddChild(pause);
	}

	private static HBoxContainer Row(string label) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 4);
		row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(LabelWidth, SlotSize.Y), VerticalAlignment = VerticalAlignment.Center });
		return row;
	}

	// A flat focusable button; a gold box marks focus / hover
	private static Button MakeSlot() {
		var slot = new Button { CustomMinimumSize = SlotSize, FocusMode = FocusModeEnum.All };
		slot.SetMeta(UiFx.NoFlash, true);   // the slot shows PRESS KEY instead of the button flash
		var idle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.25f) };
		var focus = new StyleBoxFlat { BgColor = new Color(Gold, 0.18f), BorderColor = Gold };
		focus.SetBorderWidthAll(1);
		slot.AddThemeStyleboxOverride("normal", idle);
		slot.AddThemeStyleboxOverride("disabled", idle);
		foreach (string state in new[] { "focus", "hover", "pressed", "hover_pressed" }) slot.AddThemeStyleboxOverride(state, focus);
		return slot;
	}

	private static Control CenteredBox() => new CenterContainer { CustomMinimumSize = SlotSize, MouseFilter = MouseFilterEnum.Ignore };

	// Fills a slot with a keycap, or a dim dash when it's empty
	private static Control SlotContent(string key, Control box) {
		foreach (Node child in box.GetChildren()) child.QueueFree();
		if (key == null) {
			var dash = new Label { Text = "-", MouseFilter = MouseFilterEnum.Ignore };
			dash.AddThemeColorOverride("font_color", EmptyColor);
			box.AddChild(dash);
		} else {
			box.AddChild(Keycap.Create(key));
		}
		return box;
	}

	private void RefreshSlots() {
		foreach (var (slot, (action, index)) in slots) {
			Key key = InputBindings.Instance.GetKeys(action)[index];
			if (slot.GetChildCount() == 0) slot.AddChild(CenteredBox());
			SlotContent(key == Key.None ? null : InputBindings.Label(key), slot.GetChild<Control>(0));
		}
	}

	private void StartListening(Button slot) {
		StopListening();
		listening = slot;
		blink = 0;
		var box = slot.GetChild<Control>(0);
		foreach (Node child in box.GetChildren()) child.QueueFree();
		var prompt = new Label { Text = "PRESS KEY", MouseFilter = MouseFilterEnum.Ignore };
		prompt.AddThemeColorOverride("font_color", Gold);
		box.AddChild(prompt);
		var (action, _) = slots[slot];
		ShowStatus($"PRESS A KEY FOR {InputBindings.ActionName(action)}  (ESC CANCELS)");
	}

	private void StopListening() {
		if (listening == null) return;
		listening.Modulate = Colors.White;
		listening = null;
		RefreshSlots();
	}

	// Keycap hint line: ENTER CHANGE, DEL CLEAR, ESC BACK
	private void BuildHint() {
		foreach (var (key, word) in new[] { ("ENTER", "CHANGE"), ("DEL", "CLEAR"), ("ESC", "BACK") }) {
			if (hint.GetChildCount() > 0) hint.AddChild(new Control { CustomMinimumSize = new Vector2(8, 0) });
			hint.AddChild(Keycap.Create(key));
			hint.AddChild(new Label { Text = word, VerticalAlignment = VerticalAlignment.Center });
		}
	}

	private void ShowStatus(string text) {
		status.Text = text;
		status.Visible = true;
		hint.Visible = false;
	}

	private void ShowHint() {
		status.Visible = false;
		hint.Visible = true;
	}

	private static string KeyText(Key key) => InputBindings.Label(key) switch {
		Keycap.Up => "UP ARROW",
		Keycap.Down => "DOWN ARROW",
		Keycap.Left => "LEFT ARROW",
		Keycap.Right => "RIGHT ARROW",
		var label => label,
	};

	private static string[] FirstKeys(int player, params string[] actions) =>
		actions.Select(a => InputBindings.Instance.GetLabels($"p{player}_{a}").FirstOrDefault()).Where(k => k != null).ToArray();

	private static string[] AllKeys(int player, string action) => InputBindings.Instance.GetLabels($"p{player}_{action}");
}
