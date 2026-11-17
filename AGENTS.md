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

## Unity automation

Run Unity headless through the `unity` CLI for agent work:

- Tests: `unity test . --mode EditMode`
- Batch commands: `unity run .` with the method to execute
- Live Editor: `unity status`, then `unity command` / `unity recompile` against the open Editor

Unity allows one Editor per project. If it's already open, use the live commands instead of starting another one, and never close an Editor you didn't start. Let Unity edit scenes, prefabs and assets; don't hand-edit their YAML.

## Agent skills

### Issue tracker

Issues and specs are GitHub issues on `JacobCohenIsrael/DarknessRises`, managed with the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
