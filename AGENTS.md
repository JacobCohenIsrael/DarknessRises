# Darkness Rises

A top-down survival shooter for mobile. The character walks through pitch-black Rooms on its own while the player aims and shoots at Enemies that can only be seen by Lightning, Gunfire, the Aura and Skills. Levels end with a Boss and chain endlessly. The game is being remade from scratch in Unity 6000.6.5f1 with URP, starting with the core loop. Meta progression (Credits, Shop, upgrades) and ads come later.

## Read by branch

- **Domain terms or names:** read `GLOSSARY.md` and use its terms in code, tests and docs. Update it when a durable term changes.
- **Architecture or ownership:** read `docs/adr/`. Supersede an ADR instead of silently contradicting it.
- **Unity C# APIs or compiler warnings:** read `docs/UNITY_API_GUIDANCE.md` and check APIs against this Editor and package versions.

## Working rules

1. New code lives under `Assets/DarknessRises`. Keep every namespace aligned with its folder. The 2019 prototype survives only in git history (`master`); treat it as reference, not an API.
2. Rules and changing state live in plain C#. ScriptableObjects are authored definitions that never change at runtime. MonoBehaviours stay thin. See [ADR 0003](docs/adr/0003-plain-csharp-rules-and-authored-definitions.md).
3. Everything tunable is authored data under the root configuration asset. No magic numbers in gameplay code.
4. Use local C# `Action` events with clear ownership. No global event bus or mutable service locator.
5. Pool Enemies, Shots and effects, and keep hot paths free of allocations. This is a mobile game.
6. Player commands go through named Input System actions in a source-controlled `.inputactions` asset, with keyboard/mouse bindings now and touch later. Don't poll devices directly for gameplay.
7. Cinemachine owns the fixed-angle camera, including camera shake (impulse).
8. Async game flows use UniTask, not coroutines. Every async operation takes and passes on a `CancellationToken`; the scope that owns the lifetime owns its `CancellationTokenSource`.
9. Only Lightning casts real-time shadows. Every other light is shadowless unless a device test shows there's budget for it.
10. Ask before reusing a legacy visual or audio asset. The Free Pack sounds are approved as placeholders.
11. Tests: only edit-mode tests of core rules for now. Move fast; feel gets tested by playing.
12. Load and release content through Addressables, including scenes (additive), so memory holds only what the current Level needs and content can update without a new store build. Never put game content in `Resources`. Every handle is released by the scope that loaded it. See [ADR 0007](docs/adr/0007-addressables-for-content-loading.md).
13. Development and tests never open a visible window on the user's workstation: no Unity Editor or game window, and no console (cmd/PowerShell) window. Run Unity headless only: `unity test` / `unity run` (batch mode), and `unity open . --batchmode --until-ready` when live-Editor commands or screenshots are needed (offscreen rendering). Start any other process hidden, and close every Editor you started when you're done. Batch mode alone isn't enough: Play Mode and `capture game_view` open a floating Game window and take focus. The editor-only `HeadlessWindowGuard` hides it and gives focus back, so keep it, and merge the integration branch before entering Play Mode. Tests are also silent: a headless Editor plays sound through the user's speakers, so the editor-only `HeadlessAudioMute` sets the master volume to zero in batch mode. Check audio by reading `AudioSource` state, never by listening, and don't raise `AudioListener.volume` in a headless Editor.

## Unity automation

Agents work with Unity through these tools:

- **`unity` CLI**: drives Unity from the command line, both headless and against a running Editor.
  - Tests: `unity test . --mode EditMode`
  - Batch commands: `unity run .` with the method to execute
  - Live Editor: `unity status`, then `unity command` / `unity recompile` against the open Editor
- **Unity Pipeline package** (`com.unity.pipeline`): installed in the project with `unity pipeline install`. It's what lets the CLI connect to a running Editor; keep it installed and upgrade it with `unity pipeline upgrade`.
- **Claude Unity plugin**: load the `unity:using-unity` skill before writing Unity code, and follow the guides it points to.

Unity allows one Editor per project. If it's already open, use the live commands instead of starting another one, and never close an Editor you didn't start. Let Unity edit scenes, prefabs and assets; don't hand-edit their YAML.

## Agent skills

### Issue tracker

Issues and specs are GitHub issues on `JacobCohenIsrael/DarknessRises`, managed with the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
