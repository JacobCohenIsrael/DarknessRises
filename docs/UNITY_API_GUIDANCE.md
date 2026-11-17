# Unity API guidance for agent tasks

Adapted from 2d-space-survival, which targets the same Editor version.

Use documentation that matches this project's installed Unity Editor and packages when writing runtime code, Editor tools and tests. Familiar examples from older Unity releases can compile with obsolete-API warnings even when they were once recommended practice.

## Pick the right documentation

1. Read `ProjectSettings/ProjectVersion.txt` for the required Editor version, and confirm it's installed with `unity editors --installed --json`. Resolve any mismatch before choosing replacements.
2. For engine and Editor APIs, use the official Manual and Script Reference for the matching version. The project uses Unity 6.6 (`6000.6`): [Manual](https://docs.unity.com/en-us/engine/6000.6/manual) and [Script Reference](https://docs.unity.com/en-us/engine/6000.6/script-reference). Unversioned or older search results may describe different overloads.
3. For package APIs, read `Packages/manifest.json` and `Packages/packages-lock.json`, then the installed package's docs and source under `Library/PackageCache`, or its official docs for that resolved version. This applies to Input System, Cinemachine, URP and UniTask.
4. When the web reference is incomplete, confirm signatures in the installed Editor's managed assembly XML docs under `Editor/Data/Managed`. The project Editor's compiler and analyzers have the final say.

## Object searches in Unity 6.6

`Object.FindObjectsByType` is still supported, but the overloads that take `FindObjectsSortMode`, and the enum itself, are obsolete. `FindFirstObjectByType` is obsolete too, because it depends on instance ID ordering.

| Search intent | Supported API |
| --- | --- |
| All active instances | `Object.FindObjectsByType<T>()` |
| Include inactive instances | `Object.FindObjectsByType<T>(FindObjectsInactive.Include)` |
| Any active instance | `Object.FindAnyObjectByType<T>()` |
| Any instance, including inactive | `Object.FindAnyObjectByType<T>(FindObjectsInactive.Include)` |

Prefer serialized references and owned collections over searches, especially in hot paths. If identity matters, use the owner's serialized reference. If order matters, sort explicitly; search order is not a contract.

## Reflection across script reloads

To enumerate Unity-loaded assemblies, use `UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()`. Unity's analyzer flags `AppDomain.CurrentDomain.GetAssemblies()` (`UAC0005`) because it can return assemblies Unity unloaded during a code reload. Use `UnityEditor.TypeCache` to discover derived or attributed types.

## Managed allocation checks

`System.GC.GetAllocatedBytesForCurrentThread()` always returns 0 on Unity's Mono (Boehm GC), so asserting on its delta passes even when code allocates. Assert a synchronous block with `Assert.That(() => ..., Is.Not.AllocatingGCMemory())`. That needs `using UnityEngine.TestTools.Constraints;` for the extension method and `using Is = UnityEngine.TestTools.Constraints.Is;` to avoid the NUnit `Is` ambiguity. Mono creates a method's string literals when it compiles the method; if a measured lambda contains literals, compile it first with `lambda.Method.MethodHandle.GetFunctionPointer()` instead of adding a warm-up call.

## Shader compiler warnings

Check the matching [HLSL pragma reference](https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/shaders/reference/sl-pragma-directives). Unity 6.6 deprecates `enable_d3d11_debug_symbols`. Use `enable_debug_symbols` only while debugging shaders, and remove debug-symbol directives afterwards so builds keep shader optimizations.

## Verify the change

Headless: `unity test . --mode EditMode` compiles the project at startup and fails on compile errors; its Editor log has fresh compiler and analyzer diagnostics.

With the Editor open: run `unity recompile` and check its reported errors before trusting a change.

A change is done when its APIs compile without project-owned warnings and the affected tests pass. Fix the cause rather than suppressing diagnostics. Report package-owned warnings separately; never edit generated package caches.
