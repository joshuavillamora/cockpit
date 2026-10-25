# Project Structure

Where everything lives and how to find it. For how the code works, see the [Frontend Guide](frontend-guide.md).

## Folder map

```
cockpit/
├── project.godot            Autoloads, input keys, main scene, theme
├── default_bus_layout.tres  Audio buses (Master, Music, SFX)
├── assets/                  Raw art and fonts (no logic)
│   ├── ui/menus|hud|icons|fonts   Designer UI art and the NES Cyrillic font
│   └── characters/ arenas/ vfx/
├── resources/               Data: ui_theme.tres, characters/*.tres, arenas/*.tres
├── scenes/                  Layouts
│   ├── ui/screens/          One scene per screen
│   ├── ui/components/       menu_background.tscn
│   ├── gameplay/game.tscn   HUD, pause, victory (the arena scene is added at runtime)
│   └── characters/          fighter.tscn (base), banaba, joloano
└── scripts/                 Logic
    ├── autoload/            GameManager, SceneFlow, SettingsManager, UiFx
    ├── core/                CharacterData, ArenaData, MatchController
    ├── ui/                  One script per screen or popup
    └── characters/          Fighters.cs (backend)
```

`scenes/` is layout, `scripts/` is behavior, `resources/` is data, `assets/` is raw art. A scene and its script share a name (`main_menu.tscn` ↔ `MainMenu.cs`).

### Screen → files

| Screen | Scene (`scenes/ui/screens/`) | Script | Main assets |
|---|---|---|---|
| Loading | `loading_screen.tscn` | `scripts/ui/LoadingScreen.cs` | `ui_logo`, `ui_healthbar_*` |
| Main Menu | `main_menu.tscn` | `MainMenu.cs` | `ui_logo` |
| Settings (popup) | `settings_screen.tscn` | `SettingsScreen.cs` | theme (`ui_popup_panel`), `ui_popup_divider` |
| Controls (popup) | `controls_screen.tscn` | `ControlsScreen.cs` | theme, `ui_keycap`, `ui_keycap_wide` |
| Character Select | `character_select.tscn` | `CharacterSelect.cs` | `ui_portrait_*`, `ui_select_frame_*`, `ui_info_panel`, `ui_tag_ability/passive`, keycaps |
| Map Select | `map_select.tscn` | `MapSelect.cs` | `ui_map_frame`, `ui_map_thumb_*` |
| Lobby | `lobby_tutorial.tscn` | `scripts/core/LobbyController.cs` | `assets/arenas/lobby/` (background, ground from tileset, door), `ui_hud_portrait_*`, `ui_tutorial_box`, `ui_round_marker_*` (tip dots), keycaps |
| Training dummy (in the lobby) | `scenes/gameplay/training_dummy.tscn` | `scripts/core/TrainingDummy.cs` | `assets/characters/dummy/dummy.png` |
| Match | `scenes/gameplay/game.tscn` | `scripts/core/MatchController.cs`, `MatchCamera.cs` | the chosen arena scene from `scenes/arenas/` |
| HUD | `game_hud.tscn` | `GameHud.cs` | `ui_healthbar_*`, `ui_meter_*`, `ui_counter_frame`, `ui_round_marker_*`, `ui_hud_portrait_*`, `ui_banner_round_1/2/3`, `ui_banner_fight`, `ui_banner_ko`; the blank `ui_banner_round` and `ui_banner_blank_ko` carry the DEATHMATCH and DRAW text |
| Pause | `pause_menu.tscn` | `PauseMenu.cs` | theme (`ui_popup_panel`) |
| Victory | `victory_overlay.tscn` | `VictoryOverlay.cs` | theme |

Currently unused by any screen: `ui_menu_rooster_left/right`, `ui_emblem_p1/p2`, `ui_panel` (replaced by `ui_popup_panel`) and `ui_ability_*` (the final art is imported; no wireframe places it yet).

**Asset versions:** the designer's V4 pack is the full set; V5 is the final pack and replaces V4 where both have a file (`ui_button_focus`, `ui_banner_ko`). Layouts follow the V5 wireframes (frames 1-12).

**Keycaps:** `scripts/ui/Keycap.cs` draws one key on the keycap art (`Keycap.Create("A")`). Longer names use the wide cap, stretched to fit; arrow keys draw a triangle because the font has no arrow glyphs. The key lists live in `ControlsScreen.Rows`, which the lobby also reads.

**Dark frames** (`assets/ui/dark/`) are animation frames, not a theme: `ui_button_normal_dark` (button click flash, `UiFx`), `ui_lobby_door_dark` (lobby door while a player waits in it), `ui_button_focus_dark` (imported for later), `ui_select_frame_p1/p2_dark` (character select, on confirm), `ui_round_marker_full_dark` and `ui_counter_frame_dark` (HUD, when a round is won or the score changes), `ui_meter_fill_ready_dark` (HUD, the full-meter pulse). The rest of the dark set has not been imported yet.

### How a button press travels (Play → Match)

1. `main_menu.tscn` has `PlayButton`; `MainMenu.cs` finds it with `%PlayButton`.
2. `Pressed` calls `SceneFlow.Instance.GoTo(GameManager.CharacterSelectScene)`.
3. `SceneFlow` fades and loads `character_select.tscn` (script `CharacterSelect.cs`).
4. On Enter, `GameManager.SelectCharacters(...)` stores the picks and loads `map_select.tscn`.
5. On Enter, `GameManager.SelectArena(arena)` loads the training lobby (`lobby_tutorial.tscn`). When both players walk into the door, `GameManager.EnterMatch()` loads `game.tscn`.
6. `MatchController` reads `Selected` and `SelectedArena`, spawns the chickens, and runs the rounds; `GameHud` listens to the fighters.

## Where to change things

| I want to… | Go to |
|---|---|
| Move or resize something on a screen | The screen's `.tscn`, ideally in the editor |
| Change a button's text | The `.tscn` (`text = "..."`) |
| Change colors, font or button look | `resources/ui_theme.tres` |
| Swap art | Replace the PNG in `assets/ui/...`, keep the filename |
| Change what a screen does | Its script in `scripts/ui/` |
| Change rounds to win | `GameManager.RoundsToWin` |
| Add a chicken | New `.tres` in `resources/characters/`, then `CharacterSelect.CharacterPaths` and `LoadingScreen.Preload` |
| Add a map | New arena scene in `scenes/arenas/` and `.tres` in `resources/arenas/`, then `MapSelect.ArenaPaths` and `Preload` |
| Change keys | Project Settings → Input Map |
| Update the controls list (controls screen and lobby) | `ControlsScreen.cs` (`Rows`) |
| Change intro / K.O. / Deathmatch text | `GameHud.cs` (`PlayRoundIntro`, `ShowKo`) |
| Change what happens on KO / round end | `MatchController.cs` (`OnFighterDied`) |
| Change the lobby tips | `LobbyController.cs` (`Tips`) |
| Swap a sound | Replace the file in `assets/audio/sfx/` (keep the name) and update `CREDITS.md` |
| Change a sound's loudness | `AudioManager.cs` (`Volumes`) |
| Change the music | `assets/audio/music/main.mp3` |
| Move or resize the lobby door | `lobby_tutorial.tscn` → `Door` (and its `CollisionShape2D`), `DoorArt`, and the right `Wall` |
| Change health bar color | `GameHud.SetHealth` |
| Change volume / fullscreen behavior | `SettingsManager.cs` |
| Change combat, damage, movement | `scripts/characters/Fighters.cs` (backend's file) |

## Navigation tips

- **Scene → script:** the root node of a scene shows a script icon in the editor; click it.
- **Script → scene:** each `GetNode<...>("%Something")` is a node with that name in the matching scene.
- **Who uses this name?** Search the whole project (VS Code: Ctrl+Shift+F). Searching `MapSelectScene` finds `GameManager` defining it and `LoadingScreen` preloading it.
- **Reading order:** `GameManager` → `SceneFlow` → `MainMenu` → `CharacterSelect` → `MatchController` → `GameHud`. That follows the player's path.
