# Cockpit

A local-multiplayer 2D arena brawler built around chicken fighting. Two players pick a chicken and fight on one keyboard in a Filipino backyard *sabong* arena. Each chicken has directional ground and aerial attacks, a block or air dash, and a signature ability. You win by depleting your opponent's HP.

> Competition project — submission **October 25, 2026**. Feature freeze **October 23**.

---

## Requirements

| Tool | Version | Notes |
|------|---------|-------|
| [Godot Engine — **.NET** edition](https://godotengine.org/download) | 4.7.x | The standard (non-.NET) build **cannot** open this project. |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 | Needed to compile the C# scripts. You need the SDK, not just the runtime. |
| IDE (optional) | VS Code + C# Dev Kit, Rider, or Visual Studio | |

## Getting started

1. Clone the repository.
2. Open Godot (.NET), click **Import**, and select `project.godot`.
3. Press **Build** (the hammer icon, top-right) once so the C# assembly compiles.
4. Press **F5** to run. The game starts at the loading screen (`scenes/ui/screens/loading_screen.tscn`).
To test a match directly, open `scenes/gameplay/game.tscn` and press **F6**. It falls back to Banaba vs Joloano.

The game starts fullscreen. Press **F11** to toggle windowed mode (the choice is saved).

## Controls (default)

| Action | Player 1 | Player 2 |
|--------|----------|----------|
| Move left / right | `A` / `D` | `←` / `→` |
| Up / Down (attack direction, menus) | `W` / `S` | `↑` / `↓` |
| Jump | `K` or `Space` | `N` |
| Attack | `J` or `E` | `M` or `/` |
| Pause | `Esc` or `P` | `Esc` or `P` |
| Block / Air dash | *TBD* | *TBD* |
| Ability | *TBD* | *TBD* |

Input actions are named `p{N}_<action>`, for example `p1_attack`. Edit them in **Project Settings → Input Map**.

**Attack direction:** hold a direction while pressing Attack.
- **Grounded:** neutral, side, up and down attacks.
- **Airborne:** neutral, side, up and down air attacks.

---

## Project structure

```
cockpit/
├── assets/                  # Raw art & audio from the design team (no scenes/scripts here)
│   ├── characters/          #   One folder per chicken: banaba/, joloano/, paraokan/, da_chik/
│   │   └── placeholder/     #   Temporary programmer art
│   ├── arenas/              #   One folder per stage: backgrounds, tiles, platforms, crowd
│   ├── ui/                  #   UI designer's assets
│   │   ├── fonts/           #     NES Cyrillic, etc.
│   │   ├── hud/             #     Health bars, portraits/icons, ability cooldowns, round markers
│   │   ├── menus/           #     Title, character select, pause, victory screen art
│   │   └── icons/           #     Buttons, cursors, input prompts
│   ├── vfx/                 #   Hit sparks, feathers, dust, ability effects
│   └── audio/
│       ├── music/
│       └── sfx/
├── scenes/
│   ├── characters/          # fighter.tscn (base) + one inherited scene per chicken
│   ├── arenas/              # Stage scenes (floor, walls, ceiling, platforms)
│   ├── gameplay/            # game.tscn — the match scene that spawns fighters
│   └── ui/
│       ├── screens/         #   Full screens: title, character select, HUD, round end, victory
│       └── components/      #   Reusable widgets: health bar, character card, buttons
├── scripts/
│   ├── characters/          # Fighter logic (Fighters.cs)
│   ├── ui/                  # Screen and widget scripts
│   ├── autoload/            # Global singletons (game state, scene flow)
│   └── core/                # Shared helpers, data types
├── resources/               # .tres data: themes, character stats, attack data
└── project.godot
```

**Where do I go for…**

| Problem | Look in |
|---------|---------|
| Movement, attacks, damage, HP | `scripts/characters/Fighters.cs` |
| Attack hitbox sizes, damage, active frames | The `Attacks` table at the top of `Fighters.cs` |
| A chicken's sprites or animation timing | `scenes/characters/<name>.tscn` → `AnimatedSprite2D` → SpriteFrames |
| Collision and hurtbox shapes shared by all chickens | `scenes/characters/fighter.tscn` |
| Arena layout, spawn positions, walls and floor | `scenes/gameplay/game.tscn` |
| Keybindings, resolution, physics layers | `project.godot` (edit through Project Settings) |
| Menus, HUD and screen flow | `scenes/ui/` and `scripts/ui/` |

### Characters

Every chicken is an **inherited scene** of `scenes/characters/fighter.tscn`. To add a new one:

1. Right-click `fighter.tscn` and choose **New Inherited Scene**. Save it as `scenes/characters/<name>.tscn`.
2. Assign a new `SpriteFrames` resource to `AnimatedSprite2D`. Use the animation names listed below.
3. Adjust the `CollisionShape2D` and `AttackHitbox` offsets to fit the sprite.
4. Set `PlayerId` on the instance in the match scene.

### Screen flow

```
Loading ─► Main Menu ─► Character Select ─► Map Select ─► Match ─► Victory
(press     │ Play        (4 chickens left,    (2 arenas)    │        ├─ Rematch
 start)    │ Settings     4 right, VS,                      │        └─ Back to Menu
           │ Quit         picks in the middle)              ├─ Round intro ("ROUND N" / "FIGHT!")
           └─ Settings overlay (volume, fullscreen, Controls)  ├─ K.O. screen, then the next round
                                                            ├─ Deathmatch screen before the last round
                                                            └─ Pause (Esc/P): Resume, Settings, Quit
```

**Match rules:** first to **2 round wins**. At 1-1 the last round is the **Deathmatch**. A double KO is a draw and the round is replayed. The values live in `scripts/autoload/GameManager.cs` (`RoundsToWin`). The dots under the health bars are the round wins of the current match. The number in the HUD center is the match score: matches won since the last menu, so it grows across rematches.

**Menu controls:** Menus use `W/S` or arrows plus `Enter`/`Space`. Character select uses each player's own keys: up/down to choose, attack to confirm, jump to cancel, then **`Enter`** (once both confirmed) goes to map select. On map select either player moves with left/right and **`Enter`** starts the match. `Esc` goes back.

### UI code map

| File | Role |
|------|------|
| `scripts/autoload/GameManager.cs` | Selected chickens and arena, round wins, scene paths. Call `SelectCharacters`, `StartMatch`, `Rematch`, `ReturnToMenu`. |
| `scripts/autoload/SceneFlow.cs` | `GoTo(path)` fades between screens. Always use it to change screens. |
| `scripts/autoload/SettingsManager.cs` | Volumes and fullscreen (F11), saved to `user://settings.cfg`. Audio buses: Master, Music, SFX (`default_bus_layout.tres`). |
| `scripts/core/CharacterData.cs` + `resources/characters/*.tres` | One resource per chicken: name, portrait, ability icons, fighter scene, `Playable`. |
| `scripts/core/ArenaData.cs` + `resources/arenas/*.tres` | One resource per arena: name, map thumbnail, background, tint. Add a path to `MapSelect.cs` for a new one. |
| `scripts/core/MatchController.cs` | Round state machine in `game.tscn`: spawns fighters, intro, KO, score, victory. |
| `scripts/ui/LoadingScreen.cs` | Preloads scenes and data, then "PRESS START". Add new scenes to its `Preload` list. |
| `scripts/ui/GameHud.cs` | Health bars, portraits, score, round markers, center text. `SetAbilityState(player, state)` is ready for the ability system. |
| `scripts/ui/OverlayScreen.cs` | Base for popups (`SettingsScreen`, `ControlsScreen`). Open with `OverlayScreen.Open(...)`. |
| `scripts/ui/*.cs` | Main menu, character select, map select, pause, victory overlay. |
| `resources/ui_theme.tres` | Project-wide theme: NES Cyrillic font, buttons, panels, `TextButton` variation. |

### What the fighter code must provide (backend contract)

`Fighters.cs` exposes these for the UI. Keep them when changing fighter code:

- `HealthChanged(int current, int max)` signal: drives the health bars.
- `Died` signal: ends the round. The fighter stays in the scene tree; `MatchController` frees it next round.
- `InputLocked` (bool): frozen during round intro and after a KO.
- `StartFacingRight` (bool) and `PlayerId` (int): set by `MatchController` before the fighter enters the tree.

To make a new chicken selectable, create its scene, set `Playable = true` and `Scene` in its `resources/characters/<name>.tres`.

### Current placeholders

- Logo, buttons, panels, portraits and map thumbnails are the designer's wireframe skin. Swap the PNGs in `assets/ui/` and keep the filenames.
- Menu backgrounds are flat sky and field colors (`scenes/ui/components/menu_background.tscn`). Replace with the field art.
- The main menu roosters are placeholder boxes. The doc's feathery box outline is not applied yet (it needs a transparent, 9-slice PNG from the UI designer).
- Both arenas currently use the same background (the second is tinted). Set `Background` in `resources/arenas/*.tres` when new arenas exist.
- Font: NES Cyrillic (`assets/ui/fonts/NESCyrillic.ttf`). Use sizes that are multiples of 8 so it stays sharp.
- Paraokan and Da Chik show as "SOON" on character select until their scenes exist.
- No music or sound effects yet. Use the `Music` and `SFX` buses.
- The ability icon in the HUD always shows "ready" until abilities are implemented.

---

## Asset guidelines (for designers)

### Technical specs

| Spec | Value |
|------|-------|
| Base resolution | 640 × 360 (scaled up to fit the screen) |
| Character frame / sprite-sheet cell | 64 × 64 px |
| Visible character bounds | ~48 × 56 px |
| Tile size | 16 × 16 px |
| Format | PNG with a transparent background |
| Texture filtering | Nearest (already the project default; pixels stay crisp) |

### Naming

- Use lowercase letters, underscores and no spaces: `banaba_side_attack.png`, `hud_health_bar_fill.png`.
- Characters: `<character>_<animation>.png`, saved in `assets/characters/<character>/`.
- Draw characters **facing right only**. The game flips them for the left side, so you don't need `_left` or `_right` variants.
- UI files start with their area: `hud_`, `menu_`, `select_`, `btn_`, `icon_`, `font_`.

### Animation names the code expects

Use these exact names in every chicken's SpriteFrames so the same code drives every character:

| Group | Names |
|-------|-------|
| Movement | `idle`, `walk`, `run`, `jump`, `fall`, `double_jump`, `glide`, `wall_slide`, `wall_bounce`, `air_dash` |
| Ground attacks | `neutral_attack`, `side_attack`, `up_attack`, `down_attack`, `running_attack` |
| Air attacks | `neutral_air`, `side_air`, `up_air`, `down_air` |
| Defense / state | `block`, `hurt`, `ko`, `victory` |
| Ability | `ability` (add suffixes like `ability_start` / `ability_loop` if needed) |

The code already uses `idle` and all eight attack animations. The other names are planned.

### Adding files to the project

1. Drop the file into the matching `assets/` folder **while Godot is open**, or let Godot rescan the folder.
2. To move or rename a file that is already in the project, **use Godot's FileSystem dock**, not Explorer or Finder. Godot then updates every scene that references the file. If you move a file outside Godot, scenes can lose track of it.
3. Commit the `.import` and `.uid` files that Godot generates next to your files. Other machines need them to find the assets.

---

## Workflow

- **Branches:** work on a feature branch (`feat/title-screen`, `art/banaba-run`) and merge into `main`.
- **Commits:** use [Conventional Commits](https://www.conventionalcommits.org/), for example `feat: add side attack`, `fix: adjust hitbox`, `art: add joloano idle`, or `chore: reorganize folders`.
- **Scene conflicts:** `.tscn` files merge badly. Avoid editing the same scene at the same time. Coordinate in the group chat before touching `game.tscn` or `fighter.tscn`.

## Team

| Role | Area |
|------|------|
| Character artist | Chickens, animations |
| Environment artist | Arenas, platforms, crowd |
| UI / VFX artist | HUD, menus, icons, effects |
| Backend developer | Gameplay systems: movement, combat, abilities |
| Frontend developer | Asset integration, UI, screen flow |
