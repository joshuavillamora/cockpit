# Contributing

How the team sets up the project and works together.

## Setup

| Tool | Version | Notes |
|------|---------|-------|
| [Godot Engine, **.NET** edition](https://godotengine.org/download) | 4.7.x | The standard (non-.NET) build cannot open this project. |
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 or newer | The project targets .NET 8. You need the SDK, not only the runtime. |
| Code editor (optional) | VS Code with C# Dev Kit, Rider, or Visual Studio | |

1. Clone the repository.
2. Open Godot (.NET), choose **Import**, and select `project.godot`.
3. Press **Build** (the hammer icon, top right) once so the C# code compiles.
4. Press **F5** to run the game, or open `scenes/gameplay/game.tscn` and press **F6** to jump straight into a match.

If Godot reports missing resources the first time, close it, reopen the project, and let it finish importing.

## Branches

- `main` always holds a version that runs.
- Do your work on a feature branch and merge it into `main` through a pull request.
- Name branches by area: `frontend/main`, `feat/<feature>`, `art/<asset>`, `fix/<bug>`.
- Before opening a pull request, merge the latest `main` into your branch and make sure the game still builds and runs.

## Commits

Use [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): short description`.

| Type | Use for |
|------|---------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `art` | New or changed art and audio |
| `docs` | Documentation |
| `refactor` | Code changes that do not change behavior |
| `chore` | Folder moves, project settings, tooling |

Examples: `feat(match-ui): add pause menu`, `fix: adjust side attack frames`, `art: add joloano idle`.

Keep each commit to one logical change.

## Working with Godot files

- **Scene files (`.tscn`) merge badly.** Avoid editing the same scene at the same time as someone else. Tell the team before touching `game.tscn` or `fighter.tscn`.
- **Do not edit `.import` or `.uid` files by hand, but do commit them.** Godot creates them, and other machines need them.
- **Move or rename files inside Godot** (FileSystem dock), so references update.
- **Do not commit the `.godot/` folder.** It is already in `.gitignore`.
- If a script looks up a node with `%Name` and you rename that node in the editor, update the script as well.

## Code style

- Follow the style of the surrounding code: tabs for indentation, braces on the same line, short comments that explain *why*.
- Add comments only where the reason is not obvious.
- The class name must match the file name (`MainMenu.cs` contains `class MainMenu`).
- Change screens with `SceneFlow.Instance.GoTo(...)`, never by loading scenes directly.

## Documentation

| If you change… | Update… |
|---|---|
| Controls or input actions | [README](../README.md) controls table and `ControlsScreen.cs` |
| Folder layout or screens | [Project Structure](project-structure.md) |
| What the UI needs from the fighter | [Backend Integration](backend-integration.md) |
| Art rules | [Art Guidelines](art-guidelines.md) |

## Team

| Role | Area |
|------|------|
| Character artist | Chickens and animations |
| Environment artist | Arenas, platforms, crowd |
| UI / VFX artist | HUD, menus, icons, effects |
| Backend developer | Gameplay: movement, combat, abilities |
| Frontend developer | Asset integration, UI, screen flow |
