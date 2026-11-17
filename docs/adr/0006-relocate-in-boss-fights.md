# Relocate: the one movement choice, only in Boss fights

Amends [ADR 0004](0004-player-only-aims-and-shoots.md).

Boss fights that call for it let the player Relocate: a quick dash to one of the Boss Room's marked Positions, chosen by direction (a flick on touch; WASD or the arrow keys on PC). It's a discrete choice, not steering. The character still never walks where the player points, and there is no free movement anywhere. We added it because a Boss like the Colossus becomes about reading Lightning and choosing where to stand, and that gives the player a sense of control the rest of the game deliberately withholds. We kept it to Boss fights so ADR 0004 still holds for Rooms and Stations: outside a Boss fight, every threat is answered by shooting. Stations may get Relocate later if it proves itself.
