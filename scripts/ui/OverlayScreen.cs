using Godot;
using System;

// Base for popup screens (Settings, Controls) shown on top of the main menu or pause menu.
// Esc closes them. Open() adds one to a parent and calls onClosed when it goes away.
public partial class OverlayScreen : Control {
	[Signal] public delegate void ClosedEventHandler();

	public static OverlayScreen Open(Node parent, string scenePath, Action onClosed) {
		var overlay = GD.Load<PackedScene>(scenePath).Instantiate<OverlayScreen>();
		overlay.Closed += () => onClosed?.Invoke();
		parent.AddChild(overlay);
		AudioManager.Instance?.PlaySfx(Sfx.PopupOpen);
		return overlay;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			Close();
		}
	}

	public void Close() {
		OnClosing();
		AudioManager.Instance?.PlaySfx(Sfx.PopupClose);
		EmitSignal(SignalName.Closed);
		QueueFree();
	}

	protected virtual void OnClosing() { }
}
