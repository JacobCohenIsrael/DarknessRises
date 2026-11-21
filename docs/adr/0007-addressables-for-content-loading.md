# Addressables for content and scene loading

The game is a memory-tight mobile title that will keep shipping new content (Levels, Rooms, Enemy types, Bosses) after release. Content is loaded and released through the Addressables package, not through direct scene references, `Resources` or Build Settings scene lists. Each Level's scenes, Rooms, Enemy prefabs, effects and audio are addressable, loaded when a Level needs them and released when it ends, so memory holds only what the current Level uses. Scenes load additively through Addressables and unload with their content when the Level ends. Every load takes the owning scope's `CancellationToken` (UniTask), and every handle is released by the scope that loaded it.

We picked Addressables over `Resources` because remote content catalogs let new Levels and fixes ship as content updates without a new store build, and because explicit load/release gives predictable memory on low-end phones. `Resources` stays off-limits for game content. A small bootstrap scene in Build Settings is the only scene that isn't addressable.

Pooled objects (Enemies, Shots, effects) are still pooled; their pools prewarm from loaded Addressables assets and release them when the pool is destroyed.

## Amendment: destinations are downloaded, measured, before they load (issue #113)

Content is labelled with the destinations that can need it: `Level` (every Level scene and its assets, every Enemy type, the Shared content) and `Shop` (the Shop's scene and the Shared content). The editor setup sets these labels by group; nothing is labelled by hand. Before the boot loads a destination's scene, it asks Addressables how many bytes that destination's label and scene still need (`GetDownloadSizeAsync`) and downloads them in one operation (`DownloadDependenciesAsync`), so the loading screen can show the whole wait in bytes. The loads that follow come from the device.

Downloading is not loading: a download-only operation fills the device's bundle cache (on the web, the browser's) and loads nothing into memory, and the handle is released as soon as the download ends. Memory still holds only what the current Level's scope loads. A Level's download takes every Enemy type any Level can use, not just the first Level's, so later Levels never download mid-Run. Each destination is downloaded at most once a session. Bundles that ship inside the app (an APK's) measure nothing and download nothing.
