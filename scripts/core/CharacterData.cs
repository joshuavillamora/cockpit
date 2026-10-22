using Godot;

// One playable (or upcoming) chicken. Edit the .tres files in resources/characters/.
[GlobalClass]
public partial class CharacterData : Resource {
	[Export] public string Id = "";
	[Export] public string DisplayName = "";
	[Export] public Texture2D Portrait;          // 64x64, character select and victory
	[Export] public Texture2D HudPortrait;       // 32x32, next to the health bar
	[Export] public Texture2D AbilityReady;
	[Export] public Texture2D AbilityCooling;
	[Export] public Texture2D AbilityUnavailable;
	[Export] public PackedScene Scene;       // fighter scene to spawn in a match
	[Export] public bool Playable = true;    // false = shown as "SOON" on character select

	// Character select info panel. Each box fits two lines of 20 characters: the name, then a short description.
	[Export] public string AbilityName = "";
	[Export] public string AbilityText = "";
	[Export] public string PassiveName = "";
	[Export] public string PassiveText = "";
}
