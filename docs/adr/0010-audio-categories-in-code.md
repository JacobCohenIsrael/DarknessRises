# Audio categories in code, not an AudioMixer

Players set the volume of Music, Sound and Ambient with sliders (issue #87), so every sound in the game has to belong to one of those Audio categories and follow its slider, including sounds added later.

The usual tool is an `AudioMixer` with a group per category and exposed volume parameters. We don't use one. The game ships to WebGL (itch.io) as well as Android, and Unity's Web platform doesn't support the AudioMixer, so sources routed to mixer groups would ignore the sliders there. The project already keeps every other audio effect (Slow-mo's pitch, the storm's swell) on plain `AudioSource` volume and pitch for the same reason. A `.mixer` asset also can't be built from code through a public API, and the project never hand-edits asset YAML.

So the categories are applied in code:

- **`AudioCategorySource`**, a thin component beside every `AudioSource`, holds its category: the one obvious field per source. Builders add it with `AudioRouting.Route(source, category)` right where they make the source; a view that makes sources at runtime uses `AudioCategorySource.Add`. The source plays at the view's own volume times its category's gain, so views set volume with `source.SetVolume(v)` (`AudioSourceVolume`), never `AudioSource.volume`.
- **`AudioVolumes`**, plain C# ([ADR 0003](0003-plain-csharp-rules-and-authored-definitions.md)), holds each category's percent, turns it into a gain on a perceptual curve (`VolumeCurve`: evenly spaced decibels from an authored quietest step up to 0 dB, and 0 % silent), clamps, and raises a local `Changed` event.
- **`SceneAudioVolumes`** sits on a root object of every scene with sound (a Level, the Test Room, the Shop). It reads the scene's `AudioVolumes` from the device, keeps the list of the scene's enabled categorized sources and re-applies a category's gain to them when its slider moves, so a sound already playing changes with it.
- **The check**: scene builders end with `AudioRouting.RequireRouted(scene)` and prefab upgrades with `RequireRouted(prefab)`, which fail on a source without a category or a scene with sound but no `SceneAudioVolumes`. The EditMode suite checks the built scenes and prefabs the same way, and fails on runtime code that writes `.volume` past a source's category.

A source finds its scene's `SceneAudioVolumes` itself, once, by looking at its scene's root objects. A scene's objects wake while it loads, before it counts as loaded and its roots can be searched, so a source built into a scene that loads additively (a Level, the Shop, and the boot itself in a build) can't find them when it wakes. It looks again until its scene has loaded, and the volumes **attach** every source of their scene in their own `Start` (issue #108: until then such sources, the Boss's Lightning warnings and the storm among them, ignored the sliders). Handing the volumes down instead would mean threading them through every view and pool that makes a sound (Enemy pools, Grenade explosions, the Gunshot voices), and any sound added later that missed the hand-off would silently ignore the sliders. The lookup is scoped to the source's own scene and finds that scene's composition-level object, the way the boot finds a scene's `GameRoot`; it isn't a global registry, and nothing outside the scene can reach it. Runtime objects are created in the scene that owns them (ADR 0007's scopes), so pooled sources find the right one.

The settings are kept with the device settings (`IDeviceSettings`, names `volume.sound`, `volume.ambient`, `volume.music`), not in the save: `PlayerPrefs` on a phone or a desktop, the browser's `localStorage` on the Web player ([ADR 0009](0009-the-save.md), its amendment). They describe the device and its speakers, not the player's progress, which is exactly the carve-out [ADR 0009](0009-the-save.md) leaves for `PlayerPrefs`; they need no versioning or migration, and a cloud save shouldn't carry a phone's volume to a tablet. They're stored as whole percents on every change and written to disk when a slider is released, when the panel closes, when the scene goes and when the app goes to the background.

`AudioListener.volume` stays out of it: the editor-only `HeadlessAudioMute` owns it to keep headless runs silent (AGENTS.md rule 13). The pause menu's freeze still uses `AudioListener.pause`; while the Volume panel is open there, the Ambient sources are let through it (`ignoreListenerPause`) so the Ambient slider is heard, and the sample of the Sound slider always is.

## Considered Options

- **An AudioMixer asset, made once by hand or through the Editor's internal `AudioMixerController` API**: rejected. It does nothing on WebGL, and the internal API can change under us in any Editor update.
- **One mixer for Android and code on WebGL**: rejected. Two ways to apply a volume, and only one of them tested on each platform.
- **Handing `AudioVolumes` to every view and pool**: rejected for the reasons above; it's the dependency every new sound would have to remember.
- **The volumes in the save (a v3 record and a migration step)**: rejected; device settings aren't progress (ADR 0009).

## Consequences

- A new sound is one `AudioRouting.Route` call in its builder (or `AudioCategorySource.Add` at runtime) and `SetVolume` in its view. Forgetting the first fails the build and the tests; forgetting the second fails the tests.
- A scene that plays sound needs `AudioVolumesBuilder.Build` among its builders.
- Music, when it comes, is routed to `AudioCategory.Music` and works with the slider that is already there.
