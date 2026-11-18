# Dev builds and the debug menu

A dev build is a Unity development build of the same version as the release, with the debug commands and a touch
debug menu compiled in, for playtesting on a phone. Release builds have none of it.

## Making one

```bash
unity build . --target Android --execute-method DarknessRises.Game.Editor.Builds.PlayerBuilds.BuildAndroidDev
```

```bash
unity build . --target WebGL --execute-method DarknessRises.Game.Editor.Builds.PlayerBuilds.BuildWebGLDev
```

Outputs: `Builds/Android/DarknessRises-{version}-dev.apk` and `Builds/WebGL/DarknessRises-{version}-dev-webgl.zip`.
To build the current version without bumping it, add `--versioning-strategy custom --build-version {bundleVersion}`.
They use `BuildOptions.Development` only: no script debugging and no profiler autoconnect, so they play like the release.

## The compile guard

All debug code is wrapped in `#if UNITY_EDITOR || DEBUG`. In Unity 6000.6.5f1, player scripts compiled as a development
build get `DEBUG` (alongside `DEVELOPMENT_BUILD`), and release player scripts get neither. Checked for #57 by compiling the
Android player scripts both ways and by looking for the debug types in the built APKs' IL2CPP metadata.

## The menu

The purple **DEV** button sits at the top right, just left of the pause button. It opens a panel that pauses the game
through the normal pause rules (the pause menu waits underneath) and has:

- Weapon: Pistol / Machine gun / Flamethrower (green marks the equipped one)
- God mode on/off and Lightning on/off (green when on)
- Skip to Boss and Jump to Ramp level (the authored debug jump; again for the next Level). Both close the panel.
- Close, which resumes

The buttons call the same methods as the keyboard debug keys (1/2/3, F1, F2, F3, L). The menu is built from code by
`GameRoot` at runtime (`Debugging/DebugMenuView`), so no scene references it. Its sizes and colours are constants in
`Debugging/DebugMenuStyle`: it is a development tool, not gameplay, so it stays out of the authored configuration.
