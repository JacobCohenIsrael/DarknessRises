# An endless chain of Levels, each with a clear start and end

Supersedes [ADR 0002](0002-endless-ramp-shop-between-runs.md).

A Run is endless, but it's built from Levels instead of one continuous Ramp. Each Level feels like a short story: the character enters, walks a Route through a few Rooms, fights at Stations, and faces a Boss at the end. Beating the Boss leads straight into the next, harder Level. The Run ends only when the player dies and takes Exit, or leaves from the pause menu. The Shop stays between Runs, never between Levels. We picked this over a single endless Ramp because a start, an end and a Boss give each stretch a shape and a payoff, while chaining Levels keeps the "how far can you get" pull of endless survival.

Levels and Routes are hand-built for now. Procedural Rooms and Routes are a later decision.
