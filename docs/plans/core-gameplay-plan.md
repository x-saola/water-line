# Core Gameplay — Implementation Plan

## Context

Waterline's Level Editor (already built) authors and saves levels as JSON
(`LevelConfig` etc. under `Assets/_Project/Scripts/Data/Level/`). Nothing
consumes that data yet — there is no runtime grid, no pipe-drag input, no
obstacle logic, no win/lose evaluation. `GameState_Play`, `GamePlayController`
and `GamePlayUI` exist but are leftover scaffolding from a *different* game
genre this Unity template was cloned from (a merge/collection game — fields
like `_pondZone`, `_mergeStreakImageFill`, `_categoryProgressImageFill`,
`Data.ClassicLevelModeData.GetRandomLevel/IncrementLevelAttempts`, int-indexed
`LoadLevelAssetAsync(int level)` have no Waterline equivalent). This plan
builds the actual pipe-connection-puzzle runtime — grid, drag input, Rock/
Boulder/Valve/+1 Pipe behavior, timer, win/lose, end-state UI, save — wired
through the project's real (not CLAUDE.md's stale-documented) framework seams.

**Confirmed with the user:** build the full core loop in one pass (all phases
below, verified as I go, not a small check-in slice). No booster system.
Win screen shows **Retry** and **Next** (not an auto-advance, and not
Home-first) — "Next" is a lightweight sequential level lookup on disk
(`level_{n+1}.json`), not a full level-select/progression screen, since the
GDD defines none. `GamePlayUI` is **refactored in place** (not replaced by a
parallel new class) — see below.

**Correction to CLAUDE.md:** `Assets/Atom/` does not exist in this repo. The
real framework is `Assets/Delta/Core/` (`Delta.Core` — ServiceLocator,
EventBus, StateMachine, CameraStack) and `Assets/Delta/Services/`
(`Delta.Services` — UserData, Tracking, Ad, Audio, IAP, PlayFab, Vibration).
All paths below are verified against actual files, not CLAUDE.md's table.

## Framework seams being reused (verified, not guessed)

- **ServiceLocator** (`Assets/Delta/Core/ServiceLocator/ServiceLocator.cs`):
  static `Register<T>`/`Get<T>`/`TryGet<T>`. New gameplay logic does **not**
  need registration — it's owned/constructed directly by `GamePlayController`
  (a scene-found `MonoBehaviour`, same pattern the existing controller
  already uses).
- **EventBus** (`Assets/Delta/Core/EventBus/EventBus.cs` +
  `EventBinding.cs`): `EventBus<T> where T : struct`. Reuse `AtomEvent`
  exactly as `GameState_Play.cs` already does (`AtomEvent.Trigger(AtomEventType.X)`)
  for app-wide lifecycle signals. Per-move obstacle side effects (valve
  opened, boulder pushed, +1 pipe claimed) are plain C# events on
  `GridManager`, not bus events — single-consumer, matches how
  `ILevelController` already exposes `event Action OnLevelStarted`.
- **GameStateMachine** (`Assets/Delta/Core/StateMachine/`): unchanged.
  `GameState_Play.OnEnter` → `SceneManager.LoadSceneAsync("Gameplay")` →
  `OnSceneLoaded` → `FindFirstObjectByType<GamePlayController>()` →
  `Initialize(...)`. Shell kept; bodies rewritten (see Reuse Map).
- **UIManager**: `UIManager.Instance.ShowUIOnTop<T>(string name)`.
  `GamePlayUI` (`Assets/Delta/Modules/GameplayUI/Scripts/GameplayUI.cs`) is
  **refactored in place**, not replaced — its merge-game fields (`_pondZone`,
  `_mergeStreakImageFill/ParticleSystem/UIEffect`, `_categoryProgress*`,
  `_stackContainer`, `_boosterButtonZone`) are stripped and replaced with
  Waterline HUD needs (per-pipe move counters, timer readout); its existing
  `_settingsBtn`/`OnSettings` is reused as-is since Settings is generic. No
  prefab currently backs this script (confirmed by search) — a new prefab
  is built from scratch bound to the refactored script, but the class name
  and `IGameplayUIController` implementation stay, so `GameState_Play`'s
  `ShowUIOnTop<GamePlayUI>("GameplayUI")` call needs no signature change.
  `WinLevelUI`/`LoseLevelUI` (`Assets/Delta/Modules/WinLoseUI/Scripts/`) **do**
  have real prefabs and are generic enough to reuse, with one addition (below).
- **Persistence**: `IUserDataService` (Load/Save/Delete +
  currency/item helpers, each raising `ResourceChangedEvent`). `UserData`
  (`Assets/_Project/Scripts/Data/UserData.cs`) gets a new field,
  `ClassicLevelModeData` (int-indexed, no time field, merge-game shaped)
  is left untouched/unused by Waterline rather than retrofitted.
- **Tracking**: `ITrackingService.TrackGameStart/TrackGameOver` — call
  sites already exist in `GameState_Play`, only the *values* passed change.

## `WinLevelUI`/`LoseLevelUI` — verified real API

`WinLevelUI` (`Assets/Delta/Modules/WinLoseUI/Scripts/WinLevelUI.cs`) has
`_continueButton`/`_homeButton` + `OnContinueButtonClicked`/
`OnHomeButtonClicked` actions, `SetActiveContinueButton(bool)`/
`SetActiveHomeButton(bool)`, plus an unrelated Remove-Ads/IAP offer panel
(leave inactive — out of scope). **No Retry button exists on Win today.**
To satisfy "Retry/Next": add a `_retryButton` field + `OnRetryButtonClicked`
action + `SetActiveRetryButton(bool)` to `WinLevelUI.cs`, add the button
GameObject to its prefab (mirrors `LoseLevelUI`'s existing `_retryButton`
pattern exactly), wire `OnContinueButtonClicked` → "Next" semantics, hide
Home by default (`SetActiveHomeButton(false)`) since it wasn't asked for.
`LoseLevelUI` already has `_retryButton`/`OnRetry` — reuse verbatim, call
`SetLoseCause("Out of Time")`, `ShowReviveOption(false)`.

## System breakdown

New folder: **`Assets/_Project/Scripts/Gameplay/`** (doesn't exist yet),
namespace `Delta.ProjectName`.

| System | File(s) | Responsibility |
|---|---|---|
| LevelLoader | `Gameplay/Level/LevelLoader.cs` | `string levelId` or in-memory `LevelConfig` → validated `LevelConfig` (runs `LevelValidator.Validate`). `TryGetNextLevelId(string currentId, out string nextId)` scans `Assets/_Project/Levels/` for `level_{n+1}.json`, mirroring the Level Editor's own `NextLevelNumber()` pattern. |
| WaterlinePlaySession | `Gameplay/Level/WaterlinePlaySession.cs` | Static in-memory handoff (`PendingLevelConfig`) — the seam a future Level Editor "Play this level" button targets. |
| GridManager | `Gameplay/Grid/GridManager.cs`, `RuntimeCellState.cs` | Board layout + **runtime** per-cell state, kept separate from authored `ObstacleConfig` per the spec's explicit requirement. |
| PipeRuntimeState | `Gameplay/Grid/PipeRuntimeState.cs` | Per-pipe path/budget/moves-used. |
| MoveResolver | `Gameplay/Grid/MoveResolver.cs` | Pure legality + effect resolution for one step (extend or retract) — no MonoBehaviour/UnityEngine coupling, fully unit-testable. |
| Obstacle rules | `Gameplay/Grid/Obstacles/{IObstacleEntryRule,RockRule,BoulderRule,ValveRule,PlusOnePipeRule,EmptyCellRule,ObstacleRuleResolver}.cs` | One composable rule class per `ObstacleType`. |
| PipeInputController | `Gameplay/Input/PipeInputController.cs` | MonoBehaviour; pointer/touch drag → orthogonal target cell → `MoveResolver`. |
| LevelTimer | `Gameplay/Timer/LevelTimer.cs` | Starts on first *accepted* move, not on load; count-up if untimed, countdown if `TimeLimitSeconds>0`. |
| Win/Lose rules + evaluator | `Gameplay/WinLose/{IWinRule,ILoseRule,AllPipesMeetRule,AllValvesOpenRule,TimeExpiredRule,WinEvaluator,LoseEvaluator,GameplaySessionState}.cs` | Independently-evaluable rule classes, evaluator runs **all** of them (not short-circuited) so each is separately testable/inspectable. |
| Presentation | `Gameplay/Presentation/{GridView,PipeView,WaterFlowAnimator}.cs` | Renders grid/pipes, plays win seal+flow animation (skips to end-state under reduced-motion). |
| GamePlayController (rewrite) | `Assets/_Project/Scripts/Controllers/GameplayController.cs` | Orchestrates all of the above; scene-found `MonoBehaviour`, kept class name to minimize `GameState_Play` touch surface. |
| GamePlayUI (refactor) | `Assets/Delta/Modules/GameplayUI/Scripts/GameplayUI.cs` + new prefab | Live HUD: per-pipe move counters, timer readout, reuses existing settings button/event. |
| WaterlineProgressData (new) | `Assets/_Project/Scripts/Data/WaterlineProgressData.cs` | `LevelId`-keyed completion/best-time, added as a new field on `UserData` (not a `ClassicLevelModeData` retrofit). |

### Runtime cell/pipe state model

`GridCell` has no `Equals`/`GetHashCode` override — use `(int Row, int
Column)` tuples as dictionary keys throughout, not `GridCell` itself.

- `HashSet<(int,int)> RockCells` — precomputed once, immutable for the run.
- `Dictionary<(int,int), bool> BoulderPresent` — seeded from authored
  Boulder cells; **mutated on push** (remove old key, add new key);
  **never reverted by retraction**.
- `HashSet<(int,int)> OpenValves` — starts empty, add-only, monotonic;
  **never reverted by retraction**.
- `Dictionary<(int,int), PipeId> PlusOnePipeClaims` — added on first
  pass-through; **removed only when the claiming pipe retracts back
  through that exact cell** — the one runtime state that *does* revert.
- `Dictionary<(int,int), List<PipeId>> Occupants` — which pipe(s) currently
  hold a live path cell here (0–1 normally; 2–3 at a shared head).

`PipeRuntimeState`: `PipeId Id`, `List<GridCell> Path` (index 0 = authored
`StartCell`), `int MovesUsed`, `int MoveBudget` (mutable: base +
claimed +1 Pipe bonuses, minus any released), `GridCell HeadCell => Path[^1]`.

## Input → state flow

**Extend:** `PipeInputController` determines dragged pipe → computes
`targetCell` → `MoveResolver.TryStep(pipe, targetCell, grid)` runs ordered,
independently-testable checks: orthogonal-adjacency (else ignored, not
blocked) → retraction check (`targetCell == pipe.Path[^2]`, routes to
retraction flow) → budget check → own-path re-entry check (distinct from
retraction — only the *immediately previous* cell may be re-entered) →
other-pipe legality (buried cell of another live pipe = blocked; another
pipe's **current head** = allowed — this is the meeting mechanic, see
below) → obstacle dispatch via `ObstacleRuleResolver`:
- Rock → always blocked.
- Boulder → `pushToCell = targetCell + direction`; blocked if out-of-grid,
  Rock, or already Boulder-occupied (**no chain-push**); else push
  succeeds, mutate `BoulderPresent`, allow entry.
- Valve → always allowed; opens permanently on first entry.
- +1 Pipe → always allowed; unclaimed → claim, refund this move, permanent
  `MoveBudget++`; already claimed → ordinary passable cell.
- none → always allowed.

All pass → `pipe.Path.Add(targetCell)`, `MovesUsed++` (unless refunded).
First accepted move of the level (any pipe) → `LevelTimer.Start()`. After
every accepted step/retraction → re-run `WinEvaluator`.

**Retract:** pop `pipe.Path`, `MovesUsed--`, release occupancy. If the
popped cell was a +1 Pipe claimed by *this* pipe → un-claim + `MoveBudget--`.
Boulder position and Valve open-state are **never** touched by retraction —
each obstacle type needs its own explicit retraction behavior, there's no
generic "undo the last mutation."

**Meeting-cell interpretation (a genuine spec ambiguity — flagged, not
blocking):** no cell is pre-designated as "the" meeting cell. A pipe may
always step onto another pipe's *current head*; win is simply "all pipes'
heads are equal." Pass-through (B enters A's head, then continues past it)
is allowed and doesn't retroactively invalidate anything. This is the
simplest reading consistent with "every pipe occupies the same single cell
simultaneously" and matches how this genre of puzzle typically works —
revisit if playtesting shows otherwise.

## Win/lose evaluation

```
GameplaySessionState { Pipes, Grid, Level, Timer }
IWinRule.IsSatisfied(state): AllPipesMeetRule, AllValvesOpenRule (vacuously
  true if !Level.HasValves)
WinEvaluator.Evaluate(state) -> { IsWin, Dictionary<Type,bool> RuleResults }
  runs ALL rules (no short-circuit) so each is independently assertable
ILoseRule.IsSatisfied(state): TimeExpiredRule (the only lose rule — no
  move-based lose per spec; built as a list so a future rule slots in
  cleanly)
LoseEvaluator.Evaluate(state) -> same shape
```
`GamePlayController` runs `WinEvaluator` after every accepted move/retraction
and `LoseEvaluator` on every timer tick.

## Reuse map — precise, based on reading the actual current files

- **`GameState_Play.cs`** — keep `OnEnter`/`OnExit`/`OnAppPaused`/
  `Settings`/`BackToMainMenu`/`CheckShowInterstitialAd`/
  `CheatReloadGameplay` largely as-is (generic, sound). **Rewrite**
  `OnSceneLoaded`/`InitializeGameplay` (delete `GetRandomLevel`/
  `SetRandomLevel`/`IsEarlyLevel` tutorial-chaining/commented booster-
  tutorial dead code; replace int-level resolution with `LevelId`/
  `LevelConfig` loading via `LevelLoader`), **rewrite** `OnLevelStarted`
  (drop `Data.ClassicLevelModeData.IncrementLevelAttempts(int)`, use
  `LevelId` string directly), **rewrite** `OnLevelWin`/`OnLevelLose` (drop
  `SetLevelComplete`/auto-advance-`CurrentLevel` logic; wire the new
  Retry/Next buttons; lose cause becomes `"Out of Time"` not
  `"Out of moves"`/`"OutOfMove"` — Waterline has no move-based lose).
- **`GamePlayController.cs`** — rewrite internals from scratch, **keep the
  class name** `GamePlayController` (minimizes `GameState_Play`'s
  `FindFirstObjectByType<GamePlayController>()` touch surface).
- **`ILevelController.cs`** — change `Task<int> LoadLevelAssetAsync(int)`
  → `Task<bool> LoadLevelAsync(string levelId)`, `void SetupLevel(int)` →
  `void SetupLevel(LevelConfig config)`, add `string CurrentLevelId`.
  Nothing else implements/consumes this interface — safe to change directly.
- **`GameplayUI.cs` (`GamePlayUI`)** — refactor in place: strip merge-game
  serialized fields, replace with Waterline HUD bindings (move counters,
  timer text); keep class name and `IGameplayUIController` implementation.
  Build a new prefab bound to it (none currently exists).
- **`WinLevelUI.cs` + prefab** — add Retry button/action (see above); reuse
  everything else as-is.
- **`LoseLevelUI.cs` + prefab** — reuse as-is, no changes needed.
- **`Assets/_Project/Scenes/Gameplay.unity`** — currently has *only* a
  camera GameObject. Must add a `GamePlayController` GameObject (confirmed
  missing — `FindFirstObjectByType` returns null today, the scene is
  currently broken at this step) plus a board root for `GridView`/`PipeView`.
- **`UserData.cs`** — add `public WaterlineProgressData WaterlineProgress = new();`,
  leave `ClassicLevelModeData` untouched/unused.

## Phased build order (all in this pass, each verified before moving on)

0. **Test scaffolding** — no asmdef exists anywhere under `Assets/_Project/`
   or `Assets/Delta/` (everything compiles into the default
   `Assembly-CSharp`). Create `Assets/_Project/Scripts/Gameplay/Gameplay.asmdef`
   covering (or referencing an asmdef that covers) `Data/Level/` too, since
   asmdef code can't reference non-asmdef code. Pair with
   `Assets/_Project/Tests/EditMode/GameplayTests.asmdef`.
1. **Load + render static grid.** Fix the `Gameplay.unity` blocker. `GridManager`
   builds runtime state from a `LevelConfig`; `GridView` renders cells/obstacles/
   anchors. Verify in Play mode: correct grid, no input yet.
2. **Pipe extend/retract input**, no obstacle behaviors yet (Rock blocks,
   Boulder/Valve/+1Pipe treated as plain passable). Verify: drag works, all
   base legality rules hold (own-path, other-pipe-head, budget, bounds).
3. **Layer in obstacle behaviors** one at a time: Boulder push (+ blocked
   chain-push + persists across retraction) → Valve open (+ persists) →
   +1 Pipe claim (+ reverts on same-pipe retraction). EditMode-tested before
   touching visuals.
4. **Win/lose evaluation + timer.** Verify: route pipes to meet + open all
   valves → win fires; let a timed level expire → lose fires.
5. **End-state UI/animation + retry/next.** Win: input lock, seal+flow
   animation (reduced-motion skip), `WinLevelUI` with Retry/Next + completion
   time. Lose: `LoseLevelUI` "Out of Time" + Retry (full reset to authored
   layout). Add the `WinLevelUI` Retry button. Wire `LevelLoader.TryGetNextLevelId`.
6. **Save/analytics.** Fix `ITrackingService` call-site values,
   `WaterlineProgressData` (best time never regresses, attempts increments),
   `IUserDataService.Save()`.

**Explicitly out of scope** (confirmed with the user): Boosters entirely.
Any level-select/progression/unlock/difficulty UI — "Next" is a bare
sequential disk lookup, not a screen.

## Open questions / judgment calls being made (flagging, not blocking)

1. Meeting-cell interpretation (head-to-head, pass-through allowed) — see
   above; simplest reading, revisit after playtesting if it feels wrong.
2. Boulder pushed onto a Valve/+1 Pipe cell — GDD is silent; since both are
   "passable" (not obstacles), a push there is allowed under the literal
   push rule ("blocked if next cell not free" — Rock/Boulder are the only
   things that make a cell "not free"). This can permanently cover a Valve/
   +1Pipe, risking an unwinnable board — same category as the GDD's own
   acknowledged "pipe runs out of moves" unwinnable case, left to designer
   care rather than defensively blocked.
3. Stuck-board soft-lock detection — not built (GDD leaves this to manual
   retry; real complexity is reachability across remaining budgets ×
   push permutations).
4. Levels load from `Assets/_Project/Levels/*.json` via `Application.dataPath`
   — works in Editor Play Mode (all phases here), will **not** work in a
   Player build (Assets folder isn't packaged) — flagging for a later
   Addressables/Resources migration before shipping, not this plan's concern.
5. No `Assets/_Project/Levels/*.json` files exist yet — 2-3 test levels are
   authored using the already-built Level Editor itself (Save button) as
   part of verification, which doubles as an end-to-end editor→runtime
   contract check, and lets "Next" be meaningfully tested.

## Verification

- EditMode tests (new `GameplayTests.asmdef`) for `MoveResolver`, each
  `IObstacleEntryRule`, `GridManager` mutation semantics (persist-vs-revert
  per obstacle type), `WinEvaluator`/`LoseEvaluator` + each rule, and a
  `LevelLoader` JSON round-trip test using the exact same
  `JsonSerializerSettings` as `LevelEditorWindow.cs` — run via the
  `tests-run` MCP tool.
- Author 2-3 real levels with the Level Editor (`Delta.LevelEditor.LevelEditorWindow.Open()`
  via `script-execute`, same as used earlier this session), including at
  least one with a Boulder, a Valve, a +1 Pipe, and a Pipe C, plus one timed
  and one untimed level.
- Drive `GameState_Init → GameState_MainMenu → GameState_Play` in the Unity
  Editor via `editor-application-set-state` (Play mode) and
  `console-get-logs` after each phase to confirm no compile/runtime errors,
  same verification loop used for the Level Editor work earlier this session.
- Manual/scripted playthrough of one level to confirm: extend/retract,
  Boulder push + no-chain-push + persists-on-retract, Valve opens once,
  +1 Pipe claim/release round-trip, win (all heads meet + valves open) and
  lose (timer expiry) both fire, Retry resets to authored layout, Next loads
  `level_{n+1}` when present.
