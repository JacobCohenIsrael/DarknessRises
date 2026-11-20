# Darkness Rises

A top-down survival shooter. The character walks through pitch-black Rooms on its own while the player aims and shoots at Enemies that can only be seen by Lightning, Gunfire, the Aura and Skills. Each Level ends with a Boss and leads into a harder one, endlessly.

## World

**Level**:
A short, self-contained stretch of a Run with a clear start and end: a Route through a few Rooms, ending in a Boss fight. It opens with the character walking in through a door and closes with a calm moment after the Boss, before the character walks out into the next Level. On screen its title card announces it arcade-style as a stage ("STAGE 1"), with one setup line under it on a Run's first Level; in code and docs it's always a Level.
_Avoid_: stage (outside the title card's wording), map, chapter

**Room**:
One enclosed space in a Level, connected to the next by a doorway. Each Room has its own lighting; for example, only Rooms with windows get Lightning.
_Avoid_: arena, area, map

**Route**:
The path the character walks through a Level on its own, from the start to the Boss.
_Avoid_: path, rail, track

**Station**:
A point on the Route where the character stops and fights Waves. It moves on only once they're cleared.
_Avoid_: checkpoint, encounter, arena

**Station rhythm**:
The beats every Station with Waves plays, always in the same order, so the game reads as a ride that stops, fights and goes: the character Plants, a Ready banner warns of the fight, the Waves come, a Clear banner calls the win, a moment of quiet follows, and a Go cue points along the Route before the character sets off. A Station without Waves has none: the character walks straight through.
_Avoid_: encounter flow, station phases

**Plant**:
How the character stops at a Station: it slows over the last metres, settles into a braced stride with its Weapon up, and holds it until the Clear banner. It never stops dead, and it picks its pace back up as it sets off.
_Avoid_: halt, brake

**Ride camera**:
The camera that shows the game as a ride. While the character walks it sits low and close behind it, aims ahead of it along the Route and swings toward where the Route heads. When the character Plants it settles higher and wider over the Station, turned toward where the Room's Enemies enter, and holds still until the Go cue. In a Boss Room it pulls back to frame the whole fight.
_Avoid_: follow camera, chase camera, fixed camera

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

**Rejoin**:
The character's walk back onto the Route once the Boss is beaten: from the Position where it stands, straight to the farthest point of the Route it can reach that way before the next Station, then on along the Route. It never snaps the character back to the Boss's Station.
_Avoid_: return, snap back, teleport

**Obstacle**:
A solid object in a Room, such as a pillar or furniture, that blocks movement and casts shadows.
_Avoid_: prop, cover

## Light

**Darkness**:
The default state of a Room: no light at all except what the light sources below make.

**Lightning**:
A brief flash that lights a whole Room, in Rooms that let it in, from a slightly different angle each time. It comes every few seconds, with nothing to announce it, and its Rumble follows. Under it the storm bed, a thunder loop, plays all the time Lightning reaches the character, fading out in Rooms without windows and back in Rooms with them. It rolls on from one Level into the next without a break, and fades out in the Shop. In a Boss fight, the Boss decides when it flashes, the storm bed keeps playing, and the Boss's own warning (the Colossus's growl) comes before each flash.
_Avoid_: strike, flash

**Rumble**:
The swell of the storm after Lightning: the storm bed rises, holds and settles back, a short random delay after each flash, as from a strike at a varying distance, with a small pitch change so no two sound quite alike. It never warns of a flash.
_Avoid_: thunder (as a name in code)

**Ambient bed**:
Now and then, a quiet sound from somewhere out in the dark, up near the ceiling, such as bats stirring: a few seconds cut from a long recording, fading in and out, and staying where it started while the character walks past. Background, not a Tell: it never comes from an Enemy or a Critter. It plays while the character walks, fights a Wave or takes the quiet after a Clear, never in a Boss fight. Not the storm bed, which is Lightning's.
_Avoid_: ambience loop, background music

**Gunfire**:
The light made by the player's Weapon: always its Muzzle flash, and for some Weapons its Shots too.

**Muzzle flash**:
The short burst of light at the Weapon each time it fires. It lights only the area close to the character.

**Gunshot**:
The sound of one Shot of a Primary weapon, picked from the Weapon's clips with a slightly random pitch. Rapid Gunshots overlap on a few voices; when all are busy, a new one cuts the oldest short. Not to be confused with Gunfire, which is light.
_Avoid_: fire sound, shot sound

**Firing loop**:
The sound of a Weapon that fires continuously, such as the Flamethrower, instead of a Gunshot per Shot: one loop that starts with the first Shot and fades out when firing stops.

**Hit spark**:
The small burst of sparks and light where a Shot hits something. It lights whatever was hit for a moment. A Shot that hits something it can't hurt, such as the Colossus's foot off its weak spot, gives a dull Hit spark: greyer, dimmer and smaller.
_Avoid_: impact, hit effect

**Aura**:
The faint, always-on light around the character. It shows the character's own position and little else.
_Avoid_: glow, personal light

**Tell**:
A cue that gives away an Enemy in Darkness without lighting it, such as glinting eyes or a sound from its direction. An Enemy's eyes glint only near the character, and only once they've opened: they're shut when it comes in, crack open, blink, and open fully over about a second (a Runner's sooner than a Shambler's), so an Enemy that spawns close isn't given away at once. Its footsteps are a Tell too: heard from where it walks, faster the faster it moves (a Shambler's drag, a Runner's patter), and silent while it stands to attack, reels or lies dead. Only the nearest few walking Enemies are heard.
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

**Boss health bar**:
The Boss's name and hit points across the top of the HUD, shown only during a Boss fight. The hit points just lost show as a lighter trail that drains after a moment, and ticks mark where its phases start.
_Avoid_: boss HP, life bar

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

**Knockback**:
The short push a Shot or a Grenade's blast gives a regular Enemy it damages: along the Shot's direction, or outwards from the blast. It eases out over a moment, stays on the NavMesh, and repeated hits add up only to a cap. Each Weapon sets how hard it pushes and each Enemy type how heavy it is. Bosses take none. Unlike a Stagger it never changes what the Enemy is doing: it happens on any damaging hit, and a hit during a Wind-up both Staggers and pushes.
_Avoid_: recoil, impact, push-back (that is the Continue's burst)

**Wave**:
A group of Enemies spawned together, usually when the character reaches a Station.
_Avoid_: spawn, batch, horde

**Straggler**:
A lone Enemy that comes at the character while it walks between Stations. The character keeps walking while the player deals with it.
_Avoid_: wanderer, random encounter

**Critter**:
A small, harmless animal, such as a rat, that darts through the dark to make the player jump: mostly while the character walks between Stations and in the quiet after a Clear, rarely during a Wave fight, never in a Boss fight. It scurries from one dark spot to another across the character's view, at the edge of the light, and is gone in a second or two. Light shows it like anything else, its small eyes glint like an Enemy's, and its scurry is heard from where it runs. It isn't an Enemy: it never hurts the character, Aim assist ignores it, and a Shot that kills it (with a squeak) counts as neither a hit nor a miss, so it earns no Score or Credits and leaves the Combo, accuracy and Grade as they were.
_Avoid_: rat (it's one kind), animal, vermin, pest

**Phantom eyes**:
A pair of eyes that looks out of the dark but isn't really there: the character's imagination. They look just like an Enemy's glinting eyes, but they appear only far out (beyond the Aura and beyond where real eyes glint), only while the character walks or in the quiet after a Clear, and only when no real Enemy is near. They come every 5 to 10 seconds of such safe time, up to a few pairs at once (each at its own spot), and hold a few seconds at most, blinking now and then; a pair that held its time, or that light sent away, sometimes opens again a moment later at another dark spot nearby. A whisper, a breath or a hush may come from their spot, never the same one twice in a row and never right on the heels of another voice, and sometimes a voice comes from a dark spot with no eyes at all. Any light reaching their spot (Lightning, Gunfire, a Hit spark, the Flare, a Grenade's blast) makes them vanish at once, so a flash shows nothing there; the Crosshair or a tap coming close, or the character walking nearer than they ever appear, makes them blink out; a real Enemy coming near or a fight starting sends them away at once. A Shot at them hits nothing, like any miss. So eyes near the character, or in a fight, are always real.
_Avoid_: ghost, fake eyes, false Tell, hallucination

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
A touch pattern that triggers an action. A tap fires once, as the finger lifts, and a finger that stays down (a hold) starts firing almost at once and keeps firing. Two quick one-finger taps are just two taps (two Shots). A two-finger double-tap (two quick two-finger taps in the same place) reloads, and neither of its taps fires. A one-finger flick in any direction Relocates that way in a Boss fight and does nothing elsewhere. A two-finger flick up triggers the Flare and down triggers Slow-mo. A two-finger tap alone does nothing. New Gestures (circles, squares, lines) can be added later for new Skills.
_Avoid_: swipe command, touch combo

**Two-finger flick**:
A Gesture: two fingers touch down together and flick the same way, far and fast, then lift. Its direction is the average of the two fingers'. Up triggers the Flare, down triggers Slow-mo, sideways does nothing.
_Avoid_: two-finger swipe, double flick

**Grenade button**:
On touch only, the on-screen button that throws the Grenade and shows its charges. Tapping it arms the Grenade, and the next tap in the Room throws it at that point instead of firing. Tapping it again cancels. Holding it picks it up so the player can drag it anywhere inside the screen's safe area, and the device remembers where it was dropped. It's greyed out with no charges. On keyboard and mouse it's hidden, and right-click throws.
_Avoid_: grenade icon, bomb button

**Weapon button**:
The on-screen button that switches Primary weapons, in the ammo corner of the HUD. It shows the equipped Primary's name and its ammo reserve, and a tap or a click equips the next unlocked Primary that has ammo. On keyboard and mouse, keys 1 to 3 also pick a Primary and the mouse wheel steps through them. It's hidden while the Pistol is the only Primary unlocked.
_Avoid_: weapon wheel, weapon picker, switch button

**Crosshair**:
The reticle on the HUD where the player aims. It kicks with every Shot. With a mouse it follows the pointer all the time and stands in for the cursor; on touch it appears where the finger lands with the first Shot and fades a moment after the last.
_Avoid_: cursor, pointer, hit marker (that marks a hit on an Enemy)

**Shoot hint**:
The first-run prompt ("TAP TO SHOOT") that bobs over the first Enemy of a Run's first Level and goes away as soon as the player fires.
_Avoid_: tutorial, tooltip

**Aim assist**:
On touch only, Shots bend slightly toward the nearest visible Enemy close to the player's finger. An Enemy counts as visible while light reaches it: a Lightning flash, Gunfire, a Hit spark, the Aura, the Flare or a Grenade's blast.

## Weapons

**Weapon**:
What the character fires. Each Weapon has its own fire rate, ammo, Muzzle flash and Shots. The character carries every Primary weapon the player has unlocked, with one of them equipped, and one Secondary weapon.
_Avoid_: gun

**Primary weapon**:
The Weapon fired most of the time, such as the Pistol, Machine gun or Flamethrower. It uses a magazine and reloads from its ammo reserve. Every Primary the player has unlocked comes along on every Run, and the player switches between them as they play; one that isn't unlocked can't be selected. A Run starts with the Pistol equipped: it's owned from the start and never runs out of ammo.
_Avoid_: main weapon

**Ammo reserve**:
The rounds a Primary weapon carries beyond its magazine, which its reloads take from. The Pistol's is unlimited; the Machine gun's and the Flamethrower's are finite. Reserves are full when a Run starts, each new Level refills a share of them, and a Continue fills them. The HUD shows the reserve under the magazine.
_Avoid_: spare ammo, stock, clips

**Run dry**:
What a Primary weapon does when its magazine and its ammo reserve are both empty. The character switches to the Pistol on its own, the Weapon button flashes to show why, and the dry Weapon can't be selected until a refill gives it ammo again. A magazine that is merely empty hasn't run dry: it reloads.
_Avoid_: out of ammo, empty (for a Weapon that can still reload)

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
Coming back to life where the character died, with full hit points, full ammo reserves and a burst of light that pushes nearby Enemies back, in exchange for watching an ad. Allowed once per Run. The death screen offers it arcade-style, as "CONTINUE?" over a countdown; when the countdown runs out, the Run ends as if the player took Exit. With no Continue left the screen says "GAME OVER".
_Avoid_: revive, respawn, second chance

**Restart**:
Ending the current Run and starting a new one from the first Level, straight away: it skips the end screen and the Shop, and its Credits are banked first.
_Avoid_: retry

**Exit**:
Ending the Run, after dying or from the pause menu. The end screen shows the Run's Score, the Level reached and Enemies killed, then the Credits the Run earned and the new total in the bank, counted up. It offers the end-of-Run ad, and it leads to the Shop.
_Avoid_: quit, give up

**Pause**:
Freezing the Level while the character is alive in it: Enemies, Lightning, Skill cooldowns, Grenade refill and sound all stop, and the pause menu offers Resume, Sound, Restart and Exit. Sound opens the Volume panel, and while it's open the Ambient sounds play on so the player hears that slider. The player pauses, or the game does when the app loses focus; only the player resumes.
_Avoid_: stop, break

**Score**:
The points a Run earns for its kills. Each Enemy type and Boss is worth its own points, multiplied by the Combo at the moment it dies. It shows on the HUD, pops up over every kill, and ends up on the end screen. It's kept per Run and buys nothing: it isn't Credits.
_Avoid_: points (for the Run's total), credits

**Combo**:
A streak of consecutive Shots that each hurt an Enemy. Every few hits in a row its multiplier steps up, to a cap; a Shot that hurts nothing ends it, and so does going too long without a hit. A Shot that kills a Critter is neither: the streak carries on as it was.
_Avoid_: streak, chain, multiplier (that's the Combo's number)

**Grade**:
The mark a Station's fight earns: S, A, B or C, from its accuracy, its time per kill and the hits the character took, against authored requirements.
_Avoid_: rank, rating, stars

**Result card**:
The card under the Clear banner: the Station's kills, accuracy and time, the Score it earned and its Grade.
_Avoid_: scoreboard, summary

**Hit marker**:
The red mark that flashes where a Shot hurts an Enemy: the hit confirmation. Not the Hit spark, which is light in the Room and shows for any hit, walls included.
_Avoid_: hit indicator, crosshair

**Credits**:
Currency earned during a Run and spent in the Shop. Each Enemy type and Boss pays its own Credits for a kill, and a cleared Level (its Boss beaten) pays a bonus that grows with the Level number. Credits are flat: the Combo never multiplies them. Not the Score: the Score says how well a Run went, Credits are what it pays.
_Avoid_: coins, gold, money, score

**Bank**:
The Credits the player holds between Runs, kept in the save. A Run's Credits are banked when the Run ends, whichever way it ends (Exit, the Continue countdown running out, or Restart), and when the app goes to the background mid-Run, so they're never lost.
_Avoid_: wallet, balance, purse

**End-of-Run ad**:
The one rewarded ad the end screen offers after a Run: watching it through multiplies the Credits that Run earned. It's optional, pays once per Run, and never blocks moving on. An ad that isn't watched through pays nothing and leaves the offer standing. A Run that earned too few Credits isn't offered one.
_Avoid_: bonus ad, double-up, reward video

**Save**:
The player's durable progress on the device: the bank, and what was bought in the Shop. It survives app restarts and updates.
_Avoid_: profile, account, progress (that's how far along the Route the character is)

**Shop**:
Where Credits are spent: the player unlocks Weapons there and buys Upgrades. It is open only between Runs, never between Levels: the end screen leads to it, and its PLAY starts the next Run. A first install goes straight into the first Level; once a Run has been finished, the app opens in the Shop.
_Avoid_: store, upgrade screen, menu, lobby

**Unlock**:
Buying a Weapon in the Shop, once, with Credits. An unlocked Weapon is owned for good and comes along on every Run.
_Avoid_: purchase (for a Weapon), acquire

**Upgrade**:
A lasting improvement bought in the Shop with Credits, level by level, each level dearer than the last. An Upgrade sets one number: a Primary weapon's damage, magazine, reload time or fire rate, or the ammo reserve of one whose ammo runs out; the Grenade's charges, refill time or blast radius; the Flare's or Slow-mo's duration or cooldown; the character's hit points or the reach of the Aura. A Weapon's Upgrades can be bought only once the Weapon is unlocked. Upgrades apply from the next Run; the Aura's are kept small so Darkness stays dark.
_Avoid_: perk, boost, power-up, skill tree, level-up

**Run's stats**:
The numbers a Run plays with: the authored ones with the player's Upgrades applied. They're worked out when the Run's Level starts and never change what was authored.
_Avoid_: buffs, modifiers, build

## Audio

**Audio category**:
One of the three groups every sound in the game belongs to, Sound, Ambient or Music, each with its own volume slider. Every sound source is put in one when it's built, and a sound plays at its own volume times its category's.
_Avoid_: channel, bus, mixer group

**Sound**:
The Audio category of everything the player's actions and the threats make: Gunshots and Firing loops, reloads, the Grenade, Enemies' voices, Wind-ups, attacks and footsteps, Critters, the Boss, the Station banners, and the Continue, Shop and HUD sounds. As a category name it's capitalized; "a sound" is still any sound.
_Avoid_: SFX, effects

**Ambient**:
The Audio category of the world around the character: the storm bed and its Rumble, the Ambient bed, and the Phantom eyes' whispers and breath.
_Avoid_: atmosphere, background, environment

**Music**:
The Audio category of music. The game has none yet; the category and its slider are ready for it.

**Volume panel**:
The panel with a slider for each Audio category, from 0 % (silent) to 100 % (as authored), opened from the pause menu's Sound button or the Shop's SOUND button. A slider changes its sounds as it moves, letting go of the Sound slider plays a short sample, and the settings stay on the device.
_Avoid_: settings menu, options, mixer
