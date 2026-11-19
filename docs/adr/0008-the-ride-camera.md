# The Ride camera: it leads the walk and settles at Stations

Amends [ADR 0001](0001-remake-from-scratch-on-unity-6-urp.md), which called for a fixed, tilted top-down camera.

Playtesters kept asking why they couldn't move. A camera that hangs high over a small character on an open floor reads as a twin-stick shooter with the movement missing, whatever [ADR 0004](0004-player-only-aims-and-shoots.md) intends. So the camera now shows the game as a ride. While the character walks, it sits low and close behind it, aims ahead of it along the Route and swings toward where the Route heads. When the character Plants at a Station, it settles into a higher, wider framing turned toward where the Room's Enemies enter, holds still for the fight, and swings back to lead on the Go cue. Boss Rooms keep their own framing, which looks north and shows the whole fight, so Relocate's directions stay what they were.

It is still a tilted camera above the character, not a first-person or chase camera, for three reasons. Enemies come from every side and the player can't turn to look, so the fight has to stay in view all round. Lightning's long shadows only read from above. And the Rooms are built to be seen from the south: their south walls are cut down and their windows are in the north walls. That last one sets the limit on the swing: the camera turns at most a set angle off north, to the east or the west, and never goes round behind a full-height wall. A Route heading north or south gets no swing.

Cinemachine still owns the camera (rule 7). There is one Cinemachine camera per framing (walking, Station, Boss Room) and Cinemachine blends between them. The framings come from plain C# rules (`RouteLookAhead`, `RouteHeading`, `RideSwing`, `StationFraming`, `BossRoomFraming`) fed by authored numbers in `CameraDefinition`, so a view only hands Cinemachine the pose.

## Considered Options

- **A full chase camera** (behind the character, turning all the way with the Route): rejected. Every doorway would put a full-height wall between the camera and the character, a leg heading south would look out of the Level, and Enemies entering behind the camera would arrive unseen.
- **Keeping the top-down camera and only adding banners**: rejected. The banners say "ride"; the picture still said "twin-stick".
- **A Station framing authored per Station**: not needed yet. It's derived from the Station's place and the Room's Spawn points, which gave every Station of the first Level a good framing. An authored override can be added on the Station marker if a Room needs one.

## Consequences

- A Room laid out for another viewing side would need its own "home" direction; today north is assumed everywhere (`FixedAngleFraming`).
- Anything that turns a screen direction into a world direction (Relocate's flick) must go through the camera or stay in a framing that looks north. The Go cue's arrow already goes through the camera.
- A lower camera hides more floor behind tall Obstacles, so Routes should keep pillars off the side the camera stands on.
