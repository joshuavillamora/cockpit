using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Player key bindings. Every gameplay action (p1_jump, p2_attack, ...) has two key slots that players can
// change on the controls screen. The defaults are the keys in project.godot's Input Map (first two);
// custom keys are saved to the [controls] section of user://settings.cfg and written into the InputMap,
// so Fighters and every menu that reads the actions pick them up without changes.
// Only actions that differ from the defaults are saved. Picking a key that another action already uses
// moves it (the other action loses it).
public partial class InputBindings : Node {
	public const int SlotCount = 2;
	private const string SavePath = "user://settings.cfg";
	private const string Section = "controls";

	// Shown in this order on the controls screen. "{0}" is the player number.
	public static readonly (string Action, string Label)[] Actions = {
		("p{0}_move_left", "LEFT"),
		("p{0}_move_right", "RIGHT"),
		("p{0}_up", "UP"),
		("p{0}_down", "DOWN"),
		("p{0}_jump", "JUMP"),
		("p{0}_attack", "ATTACK"),
		("p{0}_block", "BLOCK"),
	};

	// Keys owned by these actions (plus F11 for fullscreen) can't be given to a player
	private static readonly string[] ReservedActions = { "pause", "menu_start", "start_fight", "lobby_skip_tips" };

	public static InputBindings Instance { get; private set; }

	public event Action Changed;

	private readonly Dictionary<string, Key[]> defaults = new();
	private readonly Dictionary<string, Key[]> current = new();
	private readonly HashSet<Key> reserved = new() { Key.F11 };

	public override void _EnterTree() {
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		foreach (string action in AllActions()) {
			defaults[action] = Pad(KeysOf(action).Take(SlotCount));
			current[action] = (Key[])defaults[action].Clone();
		}
		foreach (string action in ReservedActions) reserved.UnionWith(KeysOf(action));

		Load();
		Apply();
	}

	public static IEnumerable<string> AllActions() {
		for (int player = 1; player <= 2; player++)
			foreach (var (action, _) in Actions) yield return string.Format(action, player);
	}

	// "P1 JUMP" for p1_jump
	public static string ActionName(string action) {
		for (int player = 1; player <= 2; player++)
			foreach (var (pattern, label) in Actions)
				if (string.Format(pattern, player) == action) return $"P{player} {label}";
		return action.ToUpperInvariant();
	}

	// Slot keys of an action; Key.None = empty slot
	public Key[] GetKeys(string action) => (Key[])current[action].Clone();

	// The keys of an action as keycap labels, empty slots left out
	public string[] GetLabels(string action) => current[action].Where(k => k != Key.None).Select(Label).ToArray();

	public bool IsReserved(Key key) => reserved.Contains(key);

	// Puts key in the slot. Returns the action that lost the key because it already had it, or null.
	public string Bind(string action, int slot, Key key) {
		string loser = null;
		foreach (var (other, keys) in current) {
			for (int s = 0; s < SlotCount; s++) {
				if (keys[s] != key || (other == action && s == slot)) continue;
				keys[s] = Key.None;
				if (other != action) loser = other;
			}
		}
		current[action][slot] = key;
		Commit();
		return loser;
	}

	// Empties a slot, but an action always keeps at least one key
	public bool Clear(string action, int slot) {
		Key[] keys = current[action];
		if (keys[slot] == Key.None || keys.Count(k => k != Key.None) <= 1) return false;
		keys[slot] = Key.None;
		Commit();
		return true;
	}

	public void ResetDefaults() {
		foreach (var (action, keys) in defaults) current[action] = (Key[])keys.Clone();
		Commit();
	}

	// Writes the bindings into the InputMap (keeps any non-keyboard events on the action)
	public void Apply() {
		foreach (var (action, keys) in current) {
			foreach (InputEvent e in InputMap.ActionGetEvents(action))
				if (e is InputEventKey) InputMap.ActionEraseEvent(action, e);
			foreach (Key key in keys)
				if (key != Key.None) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
		}
	}

	public void Save() {
		var cfg = new ConfigFile();
		cfg.Load(SavePath);   // keep the other settings in the file
		// Only changed actions are stored, so a new default in project.godot still reaches everyone else
		foreach (var (action, keys) in current) {
			if (keys.SequenceEqual(defaults[action])) {
				if (cfg.HasSectionKey(Section, action)) cfg.EraseSectionKey(Section, action);
			} else {
				cfg.SetValue(Section, action, keys.Select(k => (long)k).ToArray());
			}
		}
		cfg.Save(SavePath);
	}

	// Text for a key as the keycaps show it (arrows use Keycap's drawn triangles)
	public static string Label(Key key) => key switch {
		Key.Up => Keycap.Up,
		Key.Down => Keycap.Down,
		Key.Left => Keycap.Left,
		Key.Right => Keycap.Right,
		Key.Comma => ",",
		Key.Period => ".",
		Key.Slash => "/",
		Key.Semicolon => ";",
		Key.Apostrophe => "'",
		Key.Bracketleft => "[",
		Key.Bracketright => "]",
		Key.Backslash => "\\",
		Key.Minus => "-",
		Key.Equal => "=",
		Key.Quoteleft => "`",
		Key.Escape => "ESC",
		Key.Ctrl => "CTRL",
		_ => OS.GetKeycodeString(key).ToUpperInvariant(),
	};

	private void Commit() {
		Apply();
		Changed?.Invoke();
	}

	private void Load() {
		var cfg = new ConfigFile();
		if (cfg.Load(SavePath) != Error.Ok || !cfg.HasSection(Section)) return;
		foreach (string action in current.Keys.ToList()) {
			long[] saved = cfg.GetValue(Section, action, Array.Empty<long>()).AsInt64Array();
			if (saved.Length != SlotCount || saved.All(k => k == 0)) continue;   // missing or broken: keep the default
			current[action] = saved.Select(k => (Key)k).ToArray();
		}
	}

	private static IEnumerable<Key> KeysOf(string action) {
		if (!InputMap.HasAction(action)) yield break;
		foreach (InputEvent e in InputMap.ActionGetEvents(action))
			if (e is InputEventKey k) yield return k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode;
	}

	private static Key[] Pad(IEnumerable<Key> keys) {
		var slots = new Key[SlotCount];
		int i = 0;
		foreach (Key key in keys) slots[i++] = key;
		return slots;
	}
}
