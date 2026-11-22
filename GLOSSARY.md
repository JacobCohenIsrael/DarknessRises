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
A brief flash that lights a whole Room, in Rooms that let it in, from a different angle each time, now and then a low one that throws long shadows across the floor and up the walls. It comes every few seconds, with nothing to announce it, and its Rumble follows. Under it the storm bed, a thunder loop, plays all the time Lightning reaches the character, fading out in Rooms without windows and back in Rooms with them. It rolls on from one Level into the next without a break, and fades out in the Shop. In a Boss fight, the Boss decides when it flashes, the storm bed keeps playing, and the Boss's own warning (the Colossus's growl) comes before each flash.
_Avoid_: strike, flash

**Looming strike**:
Lightning that comes in low from behind a near Enemy that no light shows yet, straight toward the character, so the Enemy's own shadow stretches toward and over the player and up the walls, bigger and closer than the Enemy is. It's always a real Enemy's shadow: when none qualifies, the flash is an ordinary one. It lights the Room as any flash does, and the Boss's flashes can loom too, from the Enemies that drop from its back.
_Avoid_: shadow scare, fake shadow

**Rumble**:
The swell of the storm after Lightning: the storm bed rises, holds and settles back, a short random delay after each flash, as from a strike at a varying distance, with a small pitch change so no two sound quite alike. It never warns of a flash.
_Avoid_: thunder (as a name in code)

**Ambient bed**:
Now and then, a quiet sound from somewhere out in the dark, up near the ceiling, such as bats stirring: a few seconds cut from a long recording, fading in and out, and staying where it started while the character walks past. Background, not a Tell: it never comes from an Enemy or a Critter. It plays while the character walks, fights a Wave or takes the quiet after a Clear, never in a Boss fight. Not the storm bed, which is Lightning's.
_Avoid_: ambience loop, background music

**Gunfire**:
The light made by the player's Weapon: always its Muzzle flash, and for some Weapons its Shots too.

**Muzzle flash**:
The short burst of light at the Weapon each time it fires. It lights only the area close to the character. Each Weapon has its own look: the Pistol's is a short white flash, the Machine gun's a yellower, flickering one with a few sparks, and the Flamethrower's an orange glow at its nozzle. The look never changes how far Gunfire lights.

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
A pair of eyes that looks out of the dark but isn't really there: the character's imagination. They look just like an Enemy's glinting eyes, but they appear only far out (beyond the Aura and beyond where real eyes glint), and never near a real Enemy: their own spot must be clear of every living Enemy too. In safe time (while the character walks or in the quiet after a Clear, with no real Enemy within about 9 m of the character) they come every few seconds, up to a few pairs at once (each at its own spot), hold a few seconds at most, and blink now and then; a pair that held its time, or that light sent away, often opens again a moment later at another dark spot nearby. A whisper, a breath or a hush may come from their spot, never the same one twice in a row and never right on the heels of another voice, and sometimes a voice comes from a dark spot with no eyes at all. During a Wave fight, rarely, one silent pair opens at the very edge of the dark; never in the Boss fight. Any light reaching their spot (Lightning, Gunfire, a Hit spark, the Flare, a Grenade's blast) makes them vanish at once, so a flash shows nothing there; they mostly open just as a Lightning flash fades. The Crosshair or a tap coming close, or the character walking nearer than they ever appear, makes them blink out; a real Enemy coming near the character or their spot, or a fight starting (for safe-time eyes) or ending (for a fight's), sends them away at once. A Shot at them hits nothing, like any miss. So eyes near the character, or next to a real Enemy, are always real.
_Avoid_: ghost, fake eyes, false Tell, hallucination

## Skills

**Skill**:
An ability the player triggers, which then goes on cooldown.
_Avoid_: power, ability, spell

**Flare**:
A Skill that makes the character give off a burst of strong light for a few seconds.
_Avoid_: glow

**Adrenaline**:
A Skill: for a few seconds the character's reflexes speed up, so Enemies and their sounds slow down while the character keeps full speed. A faint heartbeat plays while it lasts. It was called Slow-mo until 2026-11-22.
_Avoid_: Slow-mo, slow, time warp, bullet time

**Lure**:
A Skill: the character throws a rattling noisemaker to a point in the Room. Once it lands, every Enemy that comes within its radius turns and goes for it instead of the character, and strikes it once it gets there. When its time runs out, or an Enemy's strike breaks it, they all go back to the character. An Enemy already winding up at the character finishes that attack first. Bosses ignore it. It's heard by its rattle, and it gives off a small, dim, light-blue light, and its can glows, from the throw until it ends, when both fade out. The light is a beacon showing where it lies, not a second Flare: it reaches only the Enemies standing at it, who count as visible there, for Aim assist too. On keyboard and mouse F throws it at the pointer; on touch a sideways Two-finger flick throws it that way.
_Avoid_: decoy, bait, noisemaker (in code)

## Input

**Gesture**:
A touch pattern that triggers an action. A tap fires once, as the finger lifts, and a finger that stays down (a hold) starts firing almost at once and keeps firing. Two quick one-finger taps are just two taps (two Shots). A two-finger double-tap (two quick two-finger taps in the same place) reloads, and neither of its taps fires. A one-finger flick in any direction Relocates that way in a Boss fight and does nothing elsewhere. A two-finger flick up triggers the Flare, down triggers Adrenaline, and left or right throws the Lure that way. A two-finger tap alone does nothing. New Gestures (circles, squares, lines) can be added later for new Skills.
_Avoid_: swipe command, touch combo

**Two-finger flick**:
A Gesture: two fingers touch down together and flick the same way, far and fast, then lift. Its direction is the average of the two fingers'. Up triggers the Flare, down triggers Adrenaline, and left or right throws the Lure that way, an authored distance from the character. Each direction owns the same angle either side of it, so a flick halfway between two does nothing.
_Avoid_: two-finger swipe, double flick

**Grenade button**:
On touch only, the on-screen button that throws the Grenade and shows its charges. Tapping it arms the Grenade, and the next tap in the Room throws it at that point instead of firing. Tapping it again cancels. Holding it picks it up so the player can drag it anywhere inside the screen's safe area, and the device remembers where it was dropped. It's greyed out with no charges. On keyboard and mouse it's hidden, and right-click throws.
_Avoid_: grenade icon, bomb button

**Weapon button**:
The on-screen button that switches Primary weapons, in the ammo corner of the HUD. It shows the equipped Primary's icon (a silhouette of its gun), its name and its ammo reserve, and a tap or a click equips the next unlocked Primary that has ammo. On keyboard and mouse, keys 1 to 3 also pick a Primary and the mouse wheel steps through them. It's hidden while the Pistol is the only Primary unlocked.
_Avoid_: weapon wheel, weapon picker, switch button

**Switch beat**:
What a switch of Primary weapons looks and sounds like: the gun in the character's hands dips out of line, the newly equipped one comes up in its place, and a click plays as it does. It's quick, about a quarter of a second. Nothing fires while the old gun goes down; the new one can fire as soon as it shows, while it's still coming up.
_Avoid_: weapon swap animation, holster

**Skill button**:
One Skill on the HUD, in a row at the bottom left: the Skill's icon on a dark disc with its name under it. While the Skill is active a ring of its colour runs down around the disc; while it recharges the icon dims under a dark sweep with the seconds left, and the button gives a small pulse when it's ready again. On keyboard and mouse a small badge in its corner shows the Skill's key; on touch there's none. Each Skill's icon, colour, name and key are authored on its definition, so a new Skill gets its button from data alone.
_Avoid_: skill icon, ability slot, cooldown disc

**Crosshair**:
The reticle on the HUD where the player aims. It kicks with every Shot. With a mouse it follows the pointer all the time and stands in for the cursor; on touch it appears where the finger lands with the first Shot and fades a moment after the last. While the Primary weapon reloads, a thin ring around it fills with the reload; on touch it stays where the Crosshair last was.
_Avoid_: cursor, pointer, hit marker (that marks a hit on an Enemy)

**Hurt edge**:
Red at the screen's edges that says the character is hurt. Every landed hit flashes it for a moment, stronger for a bigger hit. On the last few hit points (short of full) a slow heartbeat loops and the edge pulses in time with it; on the very last hit point a fast heartbeat crossfades in. The heartbeat stops at once on death, a Continue or the Level's end.
_Avoid_: blood screen, damage overlay, red vignette

**Shoot hint**:
The first-run prompt ("TAP TO SHOOT") that bobs over the first Enemy of a Run's first Level and goes away as soon as the player fires.
_Avoid_: tutorial, tooltip

**Aim assist**:
Shots bend slightly toward the nearest visible Enemy close to where the player aims: the finger on touch, the pointer with a mouse. Each input has its own pull, gentle at first and stronger in later Levels up to a cap; the mouse's is the slighter. The pull fades out toward the edge of its reach, so the aim never jumps. An Enemy counts as visible while light reaches it: a Lightning flash, Gunfire, a Hit spark, the Aura, the Flare, the Lure's small light or a Grenade's blast. Critters and Phantom eyes never pull the aim.

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
One thing a Weapon fires that travels and can hit an Enemy. A Shot always glows enough to be seen; some Weapons' Shots also light their surroundings. It hits anything from the character outward, so an Enemy pressed up against the character, even leaning over the gun in its attack, is still hit point-blank.
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
The points a Run earns for its kills. Each Enemy type and Boss is worth its own points, multiplied by the Combo at the moment it dies, and by the Score boost while one runs. Each Station's fight adds its Accuracy bonus on top. It shows on the HUD, pops up over every kill, and ends up on the end screen beside the Best Score. It buys nothing: it isn't Credits.
_Avoid_: points (for the Run's total), credits

**Accuracy bonus**:
What a Station's fight adds to its Score for its accuracy: the Station's Score multiplied by the best authored tier the accuracy reaches (x1.1 from 45 %, x1.25 from 65 %, x1.5 from 80 %, x1.75 from 90 %, x2 for a perfect fight), less the Score itself. A fight with too few Shots to say anything (none at all, a fight won by Grenades, or Shots that only killed Critters) gets x1. On the Result card it stamps in ("ACCURACY x1.5") and the Score counts up to the total.
_Avoid_: accuracy multiplier (that's its number), precision bonus

**Score boost**:
A timed multiplier on every Score earned (x2 for 60 s of play, authored), started by watching its own rewarded ad, offered by a button on the Result card. It doubles each kill after the Combo, and so the Accuracy bonus of what it doubled. Its timer runs only while the game plays: not while paused, during an ad, on a loading screen or while the character is dead. It carries over from Level to Level and ends when its time is up or the Run ends. While one runs, no Result card offers another. On the HUD it shows beside the Score with its seconds left, pulsing as it's about to end.
_Avoid_: double score, score multiplier, power-up

**Best Score**:
The highest Score of any Run, kept in the save (since save version 4; older saves start at 0). The end screen shows it beside the Run's Score and stamps NEW BEST! when the Run beat it; the Shop shows it under its title.
_Avoid_: high score, record

**Combo**:
A streak of consecutive Shots that each hurt an Enemy. Every few hits in a row its multiplier steps up, to a cap; a Shot that hurts nothing ends it, and so does going too long without a hit. A Shot that kills a Critter is neither: the streak carries on as it was.
_Avoid_: streak, chain, multiplier (that's the Combo's number)

**Grade**:
The mark a Station's fight earns: S, A, B or C, from its accuracy, its time per kill and the hits the character took, against authored requirements.
_Avoid_: rank, rating, stars

**Result card**:
The card under the Clear banner: the Station's kills, accuracy and time, the Score it earned and its Grade, with its Accuracy bonus stamped in. While the Score boost is on offer it has a button to watch its ad, and stays up a little longer after the Go cue; it never holds the game up.
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
The rewarded ad the end screen offers after a Run: watching it through multiplies the Credits that Run earned. It's separate from the Score boost's ad, which the Result card offers. It's optional, pays once per Run, and never blocks moving on. An ad that isn't watched through pays nothing and leaves the offer standing. A Run that earned too few Credits isn't offered one.
_Avoid_: bonus ad, double-up, reward video

**Loading screen**:
The black screen that covers everything while a Level or the Shop loads, until that scene is ready to be seen: a Level's content is in and its start beat begins, or the Shop shows the Save. If the wait goes on, it says LOADING. While a first visit is still downloading, a thin bar under the label fills with the bytes that are in (the sizes in MB under it); it never fills on its own and never goes backwards. With nothing to download it shows dots that grow instead. A Level or the Shop is never shown half-made.
_Avoid_: splash screen (that is Unity's own), loader

**Save**:
The player's durable progress on the device: the bank, and what was bought in the Shop. It survives app restarts and updates. On the Web it's kept in the browser, so it also survives a new upload of the game; it stays with that browser.
_Avoid_: profile, account, progress (that's how far along the Route the character is)

**Device settings**:
What belongs to the device rather than to the player's progress: the Volume panel's sliders and VIBRATION switch, where the Grenade button was dropped, whether a one-time hint was shown. They're kept apart from the Save, and like it they survive app restarts, updates and new uploads on the Web.
_Avoid_: preferences, options, config

**Shop**:
Where Credits are spent: the player unlocks Weapons there and buys Upgrades. It is open only between Runs, never between Levels: the end screen leads to it, and its PLAY starts the next Run. A first install goes straight into the first Level; once a Run has been finished, the app opens in the Shop.
_Avoid_: store, upgrade screen, menu, lobby

**Unlock**:
Buying a Weapon in the Shop, once, with Credits. An unlocked Weapon is owned for good and comes along on every Run.
_Avoid_: purchase (for a Weapon), acquire

**Upgrade**:
A lasting improvement bought in the Shop with Credits, level by level, each level dearer than the last. An Upgrade sets one number: a Primary weapon's damage, magazine, reload time or fire rate, or the ammo reserve of one whose ammo runs out; the Grenade's charges, refill time or blast radius; the Flare's or Adrenaline's duration or cooldown, or the Lure's time or cooldown; the character's hit points or the reach of the Aura. A Weapon's Upgrades can be bought only once the Weapon is unlocked. Upgrades apply from the next Run; the Aura's are kept small so Darkness stays dark.
_Avoid_: perk, boost, power-up, skill tree, level-up

**Run's stats**:
The numbers a Run plays with: the authored ones with the player's Upgrades applied. They're worked out when the Run's Level starts and never change what was authored.
_Avoid_: buffs, modifiers, build

## Audio

**Audio category**:
One of the three groups every sound in the game belongs to, Sound, Ambient or Music, each with its own volume slider. Every sound source is put in one when it's built, and a sound plays at its own volume times its category's.
_Avoid_: channel, bus, mixer group

**Sound**:
The Audio category of everything the player's actions and the threats make: Gunshots and Firing loops, reloads, the Grenade, Enemies' voices, Wind-ups, attacks and footsteps, Critters, the Boss, the Station banners, the heartbeat of the Hurt edge, and the Continue, Shop and HUD sounds. As a category name it's capitalized; "a sound" is still any sound.
_Avoid_: SFX, effects

**Ambient**:
The Audio category of the world around the character: the storm bed and its Rumble, the Ambient bed, and the Phantom eyes' whispers and breath.
_Avoid_: atmosphere, background, environment

**Music**:
The Audio category of music. The game has none yet; the category and its slider are ready for it.

**Volume panel**:
The panel with a slider for each Audio category, from 0 % (silent) to 100 % (as authored), opened from the pause menu's Sound button or the Shop's SOUND button. A slider changes its sounds as it moves, letting go of the Sound slider plays a short sample, and the settings stay on the device. Under the sliders, where the device can vibrate, is the VIBRATION switch for Haptics.
_Avoid_: settings menu, options, mixer

## Haptics

**Haptics**:
The phone vibrating with the game's moments: a Grenade going off, a Shot hitting an Enemy, the Colossus's stomp, being hurt, the low-health heartbeat, buying in the Shop. Each moment asks for a Haptic effect by name; whether it plays is decided by rules that keep it from turning into one long buzz (a stronger effect cuts a weaker one, a minimum gap, a cap per second), and nothing plays while the game is paused, during an ad or on the loading screen. The VIBRATION switch in the Volume panel turns them off; it's a Device setting, and only shows where the device can vibrate (Android, and Chrome on an Android phone).
_Avoid_: rumble, vibration (except as the switch's label), force feedback

**Haptic effect**:
One authored vibration, such as the Grenade's or a hit's: its pulses (each a pause, a length and a strength), how it ranks against the others, and how often it may come.
_Avoid_: pattern (that's its pulses), buzz
