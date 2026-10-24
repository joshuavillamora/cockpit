using Godot;

// One selectable arena. Edit the .tres files in resources/arenas/.
[GlobalClass]
public partial class ArenaData : Resource {
	[Export] public string Id = "";
	[Export] public string DisplayName = "";
	[Export] public Texture2D Thumbnail;          // shown on the map select screen
	[Export] public Texture2D Background;         // drawn behind the fighters in game.tscn
	[Export] public Color BackgroundTint = Colors.White;
	[Export] public PackedScene Scene;            // arena scene: art, floor, walls, platforms, SpawnP1 / SpawnP2
	[Export] public Vector2 Size = new(640, 360); // world size; wider than 640x360 turns on the follow / zoom camera
}
