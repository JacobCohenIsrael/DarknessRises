# Addressables for content and scene loading

The game is a memory-tight mobile title that will keep shipping new content (Levels, Rooms, Enemy types, Bosses) after release. Content is loaded and released through the Addressables package, not through direct scene references, `Resources` or Build Settings scene lists. Each Level's scenes, Rooms, Enemy prefabs, effects and audio are addressable, loaded when a Level needs them and released when it ends, so memory holds only what the current Level uses. Scenes load additively through Addressables and unload with their content when the Level ends. Every load takes the owning scope's `CancellationToken` (UniTask), and every handle is released by the scope that loaded it.

We picked Addressables over `Resources` because remote content catalogs let new Levels and fixes ship as content updates without a new store build, and because explicit load/release gives predictable memory on low-end phones. `Resources` stays off-limits for game content. A small bootstrap scene in Build Settings is the only scene that isn't addressable.

Pooled objects (Enemies, Shots, effects) are still pooled; their pools prewarm from loaded Addressables assets and release them when the pool is destroyed.
