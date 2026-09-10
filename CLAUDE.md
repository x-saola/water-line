# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Atom** is a Unity mobile game template/framework for Android and iOS. It uses:
- **Unity 6000.3.10f1 (LTS)** — required version
- **URP** (Universal Render Pipeline) for graphics
- **Git LFS** — required for Firebase SDK binary files

## Code Architecture

### Game Boot Sequence

`Bootstrap.unity` → `GameBootstrapper` (DontDestroyOnLoad) → in order:
1. `DeltaApp.Initialize()` — sets up backend (Firebase, PlayFab, ads)
2. `ServiceLocator.Register(...)` — registers all services
3. Controllers added as components (`PurchaseController`, `GameStateMachine`)
4. `GameStateMachine.ChangeState(new GameState_Init())`

`GameState_Init` waits for Firebase ready, then transitions → `GameState_MainMenu` → loads MainMenu scene → `GameState_Play`.

### Key Files

| File | Purpose |
|------|---------|
| `Assets/_Project/Scripts/GameFlow/GameBootstrapper.cs` | App entry point |
| `Assets/_Project/Scripts/GameState/` | State machine states |
| `Assets/_Project/Scripts/Data/UserData.cs` | Player data model |
| `Assets/_Project/Scripts/Controllers/` | GameplayController, PurchaseController |
| `Assets/Delta/GameOps/DeltaApp.cs` | Backend singleton (Firebase, ads, analytics) |
| `Assets/Delta/GameOps/AnalyticsManager.cs` | Multi-platform analytics facade |
| `Assets/Atom/Core/Service/ServiceLocator.cs` | Dependency injection |

### Namespaces

- `Delta.ProjectName` — game-specific code
- `Atom.Core` — framework core (state machine, events, service locator, save system)
- `Atom.Services` — service implementations
- `Delta.GameOps` — backend/analytics/ads integration

## Scenes

- `Bootstrap.unity` — initialization, never unloaded
- `MainMenu.unity` — main menu with all menu modules
- `Gameplay.unity` — active gameplay
