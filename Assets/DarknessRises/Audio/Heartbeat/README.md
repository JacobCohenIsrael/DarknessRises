# Heartbeat

The low-health heartbeat of the Hurt edge (see GLOSSARY).

| File | Source file | Loop | Tempo | Strong beat, soft beat |
|---|---|---|---|---|
| `Heartbeat Slow.wav` | `heartbeat_slow_0.wav` | 1.800 s, 2 beats | 66.7 BPM | 0.00 s, 0.31 s |
| `Heartbeat Fast.wav` | `heartbeat_fast_0.wav` | 1.125 s, 2 beats | 106.7 BPM | 0.00 s, 0.19 s |

- **Source:** "Heartbeat sounds" by bart, OpenGameArt: https://opengameart.org/content/heartbeat-sounds
- **License:** CC0 (public domain). No attribution required; credited here anyway.
- **Changes:** each file was normalized to a -1 dBFS peak (+12.6 dB slow, +12.7 dB fast; the originals peak at about -13.6 dBFS), and its metadata chunks were dropped. Otherwise unchanged: 44.1 kHz stereo 32-bit float, looping.

The tempos and beat times above are authored in `GameConfiguration.hurtFeedback`, which syncs the edge pulse to the clips.
