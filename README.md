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
4. Press **F5** to run. The main scene is `scenes/gameplay/game.tscn`.

The window starts fullscreen. Press **Alt+Enter** or change `display/window/size/mode` in Project Settings if you want a window while developing.

## Controls (default)

| Action | Player 1 | Player 2 |
|--------|----------|----------|
| Move left / right | `A` / `D` | `←` / `→` |
| Up / Down (attack direction) | `W` / `S` | `↑` / `↓` |
| Jump | `K` or `Space` | `N` |
| Attack | `J` or `E` | `M` or `/` |
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

### Screen flow (planned)

```
Title / Main Menu ─► Character Select ─► Gameplay (HUD) ─► Round End (K.O.) ─┐
   ▲   (Play, Settings)                      ▲                              │
   │                                         └──────── next round ◄─────────┤
   │                                                                        ▼
   └──────── Back to Main Menu ◄──── Victory (Rematch / Main Menu) ◄── Deathmatch
```

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
