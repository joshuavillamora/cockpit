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
│   ├── gameplay/game.tscn   Arena, spawns, HUD, pause, victory
│   └── characters/          fighter.tscn (base), banaba, joloano
└── scripts/                 Logic
    ├── autoload/            GameManager, SceneFlow, SettingsManager
    ├── core/                CharacterData, ArenaData, MatchController
    ├── ui/                  One script per screen or popup
    └── characters/          Fighters.cs (backend)
```

`scenes/` is layout, `scripts/` is behavior, `resources/` is data, `assets/` is raw art. A scene and its script share a name (`main_menu.tscn` ↔ `MainMenu.cs`).

### Screen → files

| Screen | Scene (`scenes/ui/screens/`) | Script | Main assets |
|---|---|---|---|
| Loading | `loading_screen.tscn` | `scripts/ui/LoadingScreen.cs` | `ui_logo`, `ui_healthbar_*` |
| Main Menu | `main_menu.tscn` | `MainMenu.cs` | `ui_logo`, `ui_menu_rooster_*` |
| Settings (popup) | `settings_screen.tscn` | `SettingsScreen.cs` | theme |
| Controls (popup) | `controls_screen.tscn` | `ControlsScreen.cs` | theme |
| Character Select | `character_select.tscn` | `CharacterSelect.cs` | `ui_portrait_*`, `ui_select_frame_*` |
| Map Select | `map_select.tscn` | `MapSelect.cs` | `ui_map_frame`, `ui_map_thumb_*` |
| Match | `scenes/gameplay/game.tscn` | `scripts/core/MatchController.cs` | arena background |
| HUD | `game_hud.tscn` | `GameHud.cs` | `ui_healthbar_*`, `ui_round_marker_*`, portraits, ability icons |
| Pause | `pause_menu.tscn` | `PauseMenu.cs` | theme |
| Victory | `victory_overlay.tscn` | `VictoryOverlay.cs` | theme |

Currently unused by any screen: `ui_banner_ko`, `ui_banner_round`, `ui_counter_frame`, `ui_cursor`, `ui_emblem_p1/p2`.

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
| Add a map | New `.tres` in `resources/arenas/`, then `MapSelect.ArenaPaths` and `Preload` |
| Change keys | Project Settings → Input Map |
| Update the controls list | `ControlsScreen.cs` (`Rows`) |
| Change intro / K.O. / Deathmatch text | `GameHud.cs` (`PlayRoundIntro`, `ShowKo`) |
| Change what happens on KO / round end | `MatchController.cs` (`OnFighterDied`) |
| Change health bar color | `GameHud.SetHealth` |
| Change volume / fullscreen behavior | `SettingsManager.cs` |
| Change combat, damage, movement | `scripts/characters/Fighters.cs` (backend's file) |

## Navigation tips

- **Scene → script:** the root node of a scene shows a script icon in the editor; click it.
- **Script → scene:** each `GetNode<...>("%Something")` is a node with that name in the matching scene.
- **Who uses this name?** Search the whole project (VS Code: Ctrl+Shift+F). Searching `MapSelectScene` finds `GameManager` defining it and `LoadingScreen` preloading it.
- **Reading order:** `GameManager` → `SceneFlow` → `MainMenu` → `CharacterSelect` → `MatchController` → `GameHud`. That follows the player's path.
