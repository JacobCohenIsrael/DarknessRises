# Sounds that outlive a Level live in the boot scene

Each Level is its own scene, loaded and released through Addressables ([ADR 0007](0007-addressables-for-content-loading.md)), and every sound in it dies when the boot unloads it. For most sounds that's right. For the storm bed it isn't: a thunder loop that runs the whole time Lightning reaches the character cut dead at every Level change, then faded back in from silence. Music, when it comes, has the same problem.

So a sound meant to carry across scenes is played by an object in the **boot scene**, which lives as long as the app:

- **`StormPlayer`** plays the storm bed: one looping Ambient source shaped by a `StormBed`. The boot owns one and hands it to each Level (`GameRoot.Play(run, save, storm)`). That Level's `LightningView` drives it while the Level plays (audible or not, the Rumble, Slow-mo's pitch). The bed's state stays with the player, so the next Level picks the storm up exactly where the last one left it, at the same point in the loop and at the same volume. On the way to the Shop the boot makes it inaudible and it fades out.
- The player **loads its own clip** into a `ContentScope` it owns. A Level's content is released when it unloads; the player's handle keeps the clip loaded. The Level no longer loads the storm clip itself.
- A Level scene still has its own `StormPlayer`, used only when the scene is played on its own in the Editor.
- **Volumes** ([ADR 0010](0010-audio-categories-in-code.md)): the boot scene's `SceneAudioVolumes` has no settings of its own. The boot makes it **follow** the volumes of the Level or Shop that is showing: the carried sounds play at that scene's gains, move with its sliders, and play on over the paused listener when its Volume panel asks. Between scenes it keeps the last gains it had. This keeps the boot scene free of the root configuration, which is Addressables content.

Runs on scaled time like everything else, so the pause still freezes the storm, and `AudioListener.pause` still holds it.

## Considered Options

- **`DontDestroyOnLoad` on the Level's storm object**: rejected. The object would belong to no scene and no owner, its clip would still be released with the Level's content, and the next Level would build another one.
- **Crossfading a new storm in each Level**: rejected. It still restarts the loop and dips on every change, and it plays the clip twice while the two overlap.
- **Giving the boot the root configuration so it owns volumes outright**: rejected. That would pull the whole configuration and its definitions into the build's own data, beside their Addressables copies.

## Consequences

- Music is played by a boot-owned player in the same way, routed to the Music category, and each scene says what it wants to hear.
- A carried sound's clip should sit in the Shared group, which every scene loads, so holding it costs no extra bundle.
