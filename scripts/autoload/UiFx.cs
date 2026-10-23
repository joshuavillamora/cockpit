using Godot;

// Small UI effects that apply on every screen. Registered as an autoload (see project.godot).
//  - ui_cursor.png is the mouse pointer everywhere.
//  - The same art is drawn beside whichever button or slider has keyboard focus.
//  - Every button flashes its dark version (ui_button_normal_dark.png) briefly when it is pressed.
public partial class UiFx : CanvasLayer {
	private const string CursorPath = "res://assets/ui/icons/ui_cursor.png";
	private const string DarkButtonPath = "res://assets/ui/dark/ui_button_normal_dark.png";
	public const string NoFlash = "ui_fx_no_flash";   // set this meta on a button to skip the press flash
	private const float FlashSeconds = 0.1f;
	private const float PointerGap = 4f;        // space between the pointer and the focused control
	private static readonly string[] ButtonStyles = { "normal", "hover", "pressed", "focus", "hover_pressed", "disabled" };

	private Texture2D cursorTexture;
	private StyleBoxTexture darkButton;
	private TextureRect pointer;

	public override void _Ready() {
		Layer = 99;                               // above every screen, below the scene fade (100)
		ProcessMode = ProcessModeEnum.Always;     // keep working while the game is paused

		cursorTexture = GD.Load<Texture2D>(CursorPath);

		pointer = new TextureRect {
			Texture = cursorTexture,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false,
		};
		AddChild(pointer);

		darkButton = new StyleBoxTexture { Texture = GD.Load<Texture2D>(DarkButtonPath) };
		darkButton.TextureMarginLeft = 8;
		darkButton.TextureMarginTop = 8;
		darkButton.TextureMarginRight = 8;
		darkButton.TextureMarginBottom = 8;
		darkButton.ContentMarginLeft = 10;
		darkButton.ContentMarginRight = 10;

		GetTree().NodeAdded += OnNodeAdded;
		GetWindow().SizeChanged += ApplyMouseCursor;
		ApplyMouseCursor();
	}

	public override void _Process(double delta) {
		Control focus = GetViewport().GuiGetFocusOwner();
		bool show = focus != null && focus.IsVisibleInTree() && (focus is BaseButton || focus is Slider);
		pointer.Visible = show;
		if (!show) return;

		Rect2 rect = focus.GetGlobalRect();
		pointer.Position = new Vector2(
			rect.Position.X - cursorTexture.GetWidth() - PointerGap,
			rect.Position.Y + (rect.Size.Y - cursorTexture.GetHeight()) / 2f);
	}

	// The mouse pointer is a real OS cursor, so scale it up with the window (the game is 640x360)
	private void ApplyMouseCursor() {
		if (DisplayServer.GetName() == "headless") return;

		int scale = Mathf.Max(1, Mathf.RoundToInt(DisplayServer.WindowGetSize().Y / 360f));
		Image image = cursorTexture.GetImage();
		image.Resize(image.GetWidth() * scale, image.GetHeight() * scale, Image.Interpolation.Nearest);
		var hotspot = new Vector2(image.GetWidth() / 2f, image.GetHeight() / 2f);   // the ornament's center

		// An Image (not a texture) so no GPU resource is left over at exit
		Input.SetCustomMouseCursor(image, Input.CursorShape.Arrow, hotspot);
		Input.SetCustomMouseCursor(image, Input.CursorShape.PointingHand, hotspot);
	}

	private void OnNodeAdded(Node node) {
		if (node is Button button) button.Pressed += () => Flash(button);
	}

	// Swap every button look for the dark one for a moment, then go back to the look it had.
	// Buttons with the NoFlash meta (the key slots on the controls screen) keep their own look.
	private async void Flash(Button button) {
		if (button.HasMeta(NoFlash)) return;
		var previous = new StyleBox[ButtonStyles.Length];
		for (int i = 0; i < ButtonStyles.Length; i++) {
			if (button.HasThemeStyleboxOverride(ButtonStyles[i])) previous[i] = button.GetThemeStylebox(ButtonStyles[i]);
			button.AddThemeStyleboxOverride(ButtonStyles[i], darkButton);
		}

		await ToSignal(GetTree().CreateTimer(FlashSeconds), SceneTreeTimer.SignalName.Timeout);
		if (!IsInstanceValid(button)) return;   // the button's screen closed in the meantime

		for (int i = 0; i < ButtonStyles.Length; i++) {
			if (previous[i] != null) button.AddThemeStyleboxOverride(ButtonStyles[i], previous[i]);
			else button.RemoveThemeStyleboxOverride(ButtonStyles[i]);
		}
	}
}
