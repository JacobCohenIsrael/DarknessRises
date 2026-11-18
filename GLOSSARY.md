# Darkness Rises

A top-down survival shooter. The character walks through pitch-black Rooms on its own while the player aims and shoots at Enemies that can only be seen by Lightning, Gunfire, the Aura and Skills. Each Level ends with a Boss and leads into a harder one, endlessly.

## World

**Level**:
A short, self-contained stretch of a Run with a clear start and end: a Route through a few Rooms, ending in a Boss fight. It opens with the character walking in through a door and closes with a calm moment after the Boss, before the character walks out into the next Level.
_Avoid_: stage, map, chapter

**Room**:
One enclosed space in a Level, connected to the next by a doorway. Each Room has its own lighting; for example, only Rooms with windows get Lightning.
_Avoid_: arena, area, map

**Route**:
The path the character walks through a Level on its own, from the start to the Boss.
_Avoid_: path, rail, track

**Station**:
A point on the Route where the character stops and fights Waves. It moves on only once they're cleared.
_Avoid_: checkpoint, encounter, arena

**Spawn point**:
An authored place in a Room where Enemies enter, such as a dark doorway, gap or vent. A Spawn point that is lit at that moment is skipped, so Enemies never appear in view.
_Avoid_: spawner, portal

**Boss Room**:
The last Room of a Level, where its Boss is fought. The Route stops there until the Boss is beaten, the camera pulls back to frame the whole fight, and the Level's end beat follows.
_Avoid_: arena, boss arena

**Position**:
A marked spot in a Boss Room that the character can Relocate to.
_Avoid_: spot, node, waypoint

**Relocate**:
The character's quick dash to a Position the player picks by direction. It exists only in Boss fights that call for it; everywhere else the player never steers.
_Avoid_: dodge, dash, move

**Obstacle**:
A solid object in a Room, such as a pillar or furniture, that blocks movement and casts shadows.
_Avoid_: prop, cover

## Light

**Darkness**:
The default state of a Room: no light at all except what the light sources below make.

**Lightning**:
A brief flash that lights a whole Room, in Rooms that let it in. It comes roughly every five seconds, announced by a Rumble. In a Boss fight, the Boss decides when it flashes.
_Avoid_: strike, flash, thunder

**Rumble**:
The sound that warns that Lightning is about to flash.

**Gunfire**:
The light made by the player's Weapon: always its Muzzle flash, and for some Weapons its Shots too.

**Muzzle flash**:
The short burst of light at the Weapon each time it fires. It lights only the area close to the character.

**Hit spark**:
The small burst of sparks and light where a Shot hits something. It lights whatever was hit for a moment.
_Avoid_: impact, hit effect

**Aura**:
The faint, always-on light around the character. It shows the character's own position and little else.
_Avoid_: glow, personal light

**Tell**:
A cue that gives away an Enemy in Darkness without lighting it, such as glinting eyes or a sound from its direction.
_Avoid_: hint, indicator

## Threat

**Enemy**:
A hostile creature that hunts the character.
_Avoid_: monster, mob, creature

**Enemy type**:
A kind of Enemy, defined by how it looks, sounds, moves and attacks. Each Enemy belongs to one Enemy type.
_Avoid_: class, variant, species

**Shambler**:
A slow, tough Enemy type, slower than the character's walking pace. It appears from the first Level.

**Runner**:
A fast, fragile Enemy type, faster than the character's walking pace. It starts appearing partway into the Ramp.

**Boss**:
A unique, far tougher Enemy that waits at the end of a Level. Every Boss fight makes Lightning a central part of how it's fought.
_Avoid_: miniboss, champion

**Colossus**:
The first Boss: a giant too tall to see whole, whose feet stomp down into its Room. Lightning reveals the shadow of where the next stomp will land, and the glowing weak spot on a planted foot is where it takes damage.

**Feint**:
A Boss stomp that shows two shadows but brings down only one foot; a second, late flash of Lightning reveals which. The Colossus feints from its second phase.
_Avoid_: fake, decoy

**Wind-up**:
The visible and audible warning an Enemy gives just before it attacks.
_Avoid_: telegraph, tell (a Tell is something else)

**Stagger**:
What happens when an Enemy is hit during its Wind-up: the attack is cancelled and the Enemy reels for a moment.
_Avoid_: interrupt, stun

**Wave**:
A group of Enemies spawned together, usually when the character reaches a Station.
_Avoid_: spawn, batch, horde

**Straggler**:
A lone Enemy that comes at the character while it walks between Stations. The character keeps walking while the player deals with it.
_Avoid_: wanderer, random encounter

## Skills

**Skill**:
An ability the player triggers, which then goes on cooldown.
_Avoid_: power, ability, spell

**Flare**:
A Skill that makes the character give off a burst of strong light for a few seconds.
_Avoid_: glow

**Slow-mo**:
A Skill that slows Enemies and their sounds for a few seconds while the character keeps full speed.
_Avoid_: slow, time warp, bullet time

## Input

**Gesture**:
A touch pattern that triggers an action, such as a tap, a flick or a two-finger tap. A double-tap is recognized but does nothing for now. New Gestures (circles, squares, lines) can be added later for new Skills.
_Avoid_: swipe command, touch combo

**Grenade button**:
On touch only, the on-screen button that throws the Grenade and shows its charges. Tapping it arms the Grenade, and the next tap in the Room throws it at that point instead of firing. Tapping it again cancels. Holding it picks it up so the player can drag it anywhere inside the screen's safe area, and the device remembers where it was dropped. It's greyed out with no charges. On keyboard and mouse it's hidden, and right-click throws.
_Avoid_: grenade icon, bomb button

**Aim assist**:
On touch only, Shots bend slightly toward the nearest visible Enemy close to the player's finger. An Enemy counts as visible while light reaches it: a Lightning flash, Gunfire, a Hit spark, the Aura, the Flare or a Grenade's blast.

## Weapons

**Weapon**:
What the character fires. Each Weapon has its own fire rate, ammo, Muzzle flash and Shots. The character carries one Primary weapon and one Secondary weapon.
_Avoid_: gun

**Primary weapon**:
The Weapon fired most of the time, such as the Pistol, Machine gun or Flamethrower. It uses a magazine and reloads.
_Avoid_: main weapon

**Secondary weapon**:
A Weapon with limited uses, fired with a separate input, such as the Grenade.
_Avoid_: special, sidearm

**Grenade**:
A Secondary weapon the character throws. It explodes, damaging Enemies in an area and lighting it up for a moment. The character carries a few Grenade charges, which refill one at a time.

**Shot**:
One thing a Weapon fires that travels and can hit an Enemy. A Shot always glows enough to be seen; some Weapons' Shots also light their surroundings.
_Avoid_: bullet, projectile

## Structure

**Run**:
One attempt: an endless chain of Levels played back to back, until the player dies and doesn't Continue, or leaves from the pause menu.
_Avoid_: game, session, match

**Ramp**:
The rise in difficulty along a Level and from each Level to the next.
_Avoid_: stage, difficulty curve

**Continue**:
Coming back to life where the character died, with full hit points and a burst of light that pushes nearby Enemies back, in exchange for watching an ad. Allowed once per Run.
_Avoid_: revive, respawn, second chance

**Restart**:
Ending the current Run and starting a new one from the first Level.
_Avoid_: retry

**Exit**:
Ending the Run, after dying or from the pause menu. The end screen shows the Level reached and Enemies killed; it leads to the Shop once the Shop exists.
_Avoid_: quit, give up

**Pause**:
Freezing the Level while the character is alive in it: Enemies, Lightning, Skill cooldowns, Grenade refill and sound all stop, and the pause menu offers Resume, Restart and Exit. The player pauses, or the game does when the app loses focus; only the player resumes.
_Avoid_: stop, break

**Credits**:
Currency earned by killing Enemies during a Run and spent in the Shop.
_Avoid_: coins, gold, money, score

**Shop**:
Where Credits are spent. It is open only between Runs, never between Levels.
_Avoid_: store, upgrade screen
