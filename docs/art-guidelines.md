# Art and Asset Guidelines

For the character, arena and UI artists. Following these rules lets the programmers drop your files straight into the game.

## Technical specs

| Spec | Value |
|------|-------|
| Base resolution | 640 × 360 px (scaled up to fit the screen) |
| Character frame / sprite-sheet cell | 64 × 64 px |
| Visible character bounds | about 48 × 56 px |
| Tile size | 16 × 16 px |
| Format | PNG with a transparent background |
| Style | Hand-made pixel art, no vector art or realistic textures |
| Texture filtering | Nearest (already the project default, so pixels stay crisp) |

All characters, effects and environments must use the same pixel scale and outline style.

## Naming

- Use lowercase letters and underscores, with no spaces: `banaba_side_attack.png`, `hud_health_bar_fill.png`.
- Characters: `<character>_<animation>.png`, saved in `assets/characters/<character>/`.
- Draw characters **facing right only**. The game flips them for the left side, so there are no `_left` or `_right` variants.
- UI files start with their area, for example `hud_`, `menu_`, `select_`, `btn_`, `icon_`.

## Where files go

| Asset | Folder |
|-------|--------|
| Chickens | `assets/characters/<name>/` |
| Arena backgrounds, tiles, platforms | `assets/arenas/<arena>/` |
| Menu art (logo, buttons, panels) | `assets/ui/menus/` |
| HUD art (health bars, portraits, round markers) | `assets/ui/hud/` |
| Icons and cursors | `assets/ui/icons/` |
| Fonts | `assets/ui/fonts/` |
| Hit sparks, feathers, dust, ability effects | `assets/vfx/` |
| Music and sound effects | `assets/audio/music/`, `assets/audio/sfx/` |

## Animation names the code expects

Use these exact names for every chicken so the same code drives all of them:

| Group | Names |
|-------|-------|
| Movement | `idle`, `walk`, `run`, `jump`, `fall`, `double_jump`, `glide`, `wall_slide`, `wall_bounce`, `air_dash` |
| Ground attacks | `neutral_attack`, `side_attack`, `up_attack`, `down_attack`, `running_attack` |
| Air attacks | `neutral_air`, `side_air`, `up_air`, `down_air` |
| Defense / state | `block`, `hurt`, `ko`, `victory` |
| Ability | `ability` (add suffixes like `ability_start` or `ability_loop` if needed) |

The code currently uses `idle` and the eight attack animations. The other names are planned.

For attack animations, also tell the programmers **which frames deal damage** (the active frames).

## UI notes

- **Font:** NES Cyrillic. Text looks sharpest at sizes that are multiples of 8 (8, 16, 24, 32, and so on). Anything smaller than 16 px is hard to read.
- **Buttons and panels** are stretched as 9-slice images: the corners stay sharp and the middle stretches. Keep the border of the art simple and symmetrical.
- **Box outlines:** the design calls for a "feathery" outline on box-like elements. Please deliver it as a transparent PNG with clear corners.
- **Replacing a placeholder:** save over the existing file with the same filename. No code change is needed.

## Adding files to the project

1. Put the file in the matching `assets/` folder. Godot imports it automatically when the editor is open.
2. To move or rename a file that is already in the project, **use the FileSystem dock inside Godot**, not Explorer or Finder. Godot then updates every scene that uses the file. Moving it outside Godot can break references.
3. Commit the `.import` file that Godot creates next to your image. Other machines need it.

## Rights and disclosure

- Only submit art made by the team, or art you have the right to use.
- If any AI tool helped make an asset, tell the frontend developer so it can be listed in the project's asset and AI disclosure form.
- Do not use stock photos or other people's artwork, even as placeholders in files that ship with the game.
