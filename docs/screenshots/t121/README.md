# #121 evidence: the Web build's audio output, before and after

Measured, never listened to. Each run plays a **development Web build of 0.1.18** (the integration branch at `9aeb3ea`,
then with this fix) in headless Chrome 157 with `--mute-audio`, a fresh profile and no window, on WebGPU.

- **The output:** a script injected before the page's own routes everything Unity connects to the page's audio output
  through an AudioWorklet meter. It logs the summed signal every 50 ms of audio time: peak and RMS in dBFS, and how many
  samples reached full scale (the browser clips those hard).
- **The sources:** a temporary probe in the dev build (not committed, not in any release) logs every playing
  AudioSource every 50 ms: scene/path, clip (`-` for one-shots), effective volume `v` and category gain `g`. It also
  logs a `STOP` when a source playing above 0.02 is gone at the next sample (`destroyed` = its scene unloaded).
- **The run:** Sound and Ambient at 100 %, god mode, skip to the Boss, let the fight run 8 s, beat it, ride the end
  beat into Level 2, play 12 s, then Exit and leave the end screen for the Shop.

Times in the timelines are seconds from the Boss being beaten (`RunPhase.LevelEnd`).

| file | what |
|---|---|
| `before-summary.txt`, `before-timeline.txt` | 0.1.18 as released |
| `after-summary.txt`, `after-timeline.txt` | with this PR (scene fades + Web limiter) |
| `after-fade-only-summary.txt`, `after-fade-only-pileup.txt` | the fades without the limiter: a fight pile-up at +7.4 dBFS |

## Before (0.1.18)

```
 4.51 peak  -9.2 rms -18.3 clip   0 end/LevelEnd   Level/.../Room Boss Room/Colossus[-] v=1.000 g=1.00
 6.31 peak  -3.6 rms -14.8 clip   0 end/LevelEnd   Level/.../Room Boss Room/Colossus[-] v=1.000 g=1.00
 7.97 peak   0.2 rms  -8.7 clip   5 end/LevelEnd   Level/.../Room Boss Room/Colossus[-] v=1.000 g=1.00
 8.42 peak   0.2 rms  -8.8 clip   6 end/LevelEnd   Level/.../Room Boss Room/Colossus[-] v=1.000 g=1.00
 8.97 peak   0.1 rms -12.5 clip   1 end/LevelStart Level/.../Room Boss Room/Colossus[-] v=1.000 g=1.00
 8.98 ** STOP destroyed Level/Level/Rooms/Room Boss Room/Colossus[-] at vol=1.000
 9.07 peak  -inf rms  -inf                         (nothing)
```

The only source playing from the Boss's death to the unload is the Colossus's voice, playing its defeat one-shot,
"Tsunami hitting a Large City" (45.8 s, imported normalized: it builds from -43 dBFS to full scale at 12 s and stays
there until 29 s). It reached full scale over the title card and was cut dead with the Level scene. Clipped windows:
40 (237 samples). The storm was already hushed to 0 by #109.

On the way to the Shop, Enemy footsteps and the Ambient bed were still at 0.45 to 0.55 when the Level unloaded, and
were destroyed with it (`STOP destroyed` at 23.59), masked by the storm.

## After

```
 0.26 peak  -3.0 rms -12.8  Colossus[-] v=1.000; Storm Bed v=0.842   (the storm's hush)
 2.96 peak -10.3 rms -18.8  Colossus[-] v=1.000
 3.26 peak -12.4 rms -21.3  Colossus[-] v=0.833   <- the walk out: the Level's sounds fade with the screen
 4.16 peak -22.0 rms -30.3  Colossus[-] v=0.223
 4.76 peak  -inf                                  silent over the black screen, the card and the unload
 9.41 peak -27.8 rms -37.4  Storm Bed v=0.055     the next Level: the storm creeps back
```

No clipped samples anywhere in the run (max peak -2.1 dBFS), and no source stopped dead at the Level-to-Level unload.
On the way to the Shop the Level's sounds fade over 0.6 s before it unloads; the last few were at 0.14 to 0.21 when a
main-thread hitch during the change ended the fade, about 5 dB under the fading storm.
