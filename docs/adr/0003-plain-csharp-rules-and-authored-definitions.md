# Plain C# rules, authored definitions, thin MonoBehaviours

Game rules and changing state (the Ramp and Wave schedule, player hit points, magazines and reloads, Skill cooldowns) live in plain C# objects. Every Enemy type, Weapon, Skill and tuning value is a ScriptableObject definition that is never changed at runtime; runtime state is copied into ordinary objects. MonoBehaviours stay thin: they translate Unity callbacks, hold scene references, show state and forward commands. A new Enemy type or Weapon is a new definition asset, and code is written only for a genuinely new behaviour.

All tuning hangs off one root configuration asset, so changing a number never needs a code change. Owners talk through local C# `Action` events; there is no global event bus or mutable service locator. Code lives under `Assets/DarknessRises` with namespaces matching folders, in two assemblies to start: `DarknessRises.Game` and `DarknessRises.Game.EditModeTests`. More assemblies get split out only when something needs them.

## Considered Options

- **Weapon programs** (composable step trees, as in 2d-space-survival's ADR 0009): rejected for now. Four Weapons with fire rate, ammo, Shot and light settings don't need composition. We'll revisit if Weapon behaviours start to diverge.
- **A configuration editor window**: rejected for now. The Inspector on the root configuration asset is enough for one designer.
