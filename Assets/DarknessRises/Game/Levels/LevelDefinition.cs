using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// One hand-built Level: its title, its Rooms in the order the Route walks through them, and its start and end beats.
    /// The Rooms' shapes are built in the Level scene; this holds what is fought in them. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Level Definition", fileName = "LevelDefinition", order = 40)]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Shown on the title card as the Level starts.")]
        private string title = "Level 1";

        [SerializeField, Tooltip("Shown once the character has walked out the far door.")]
        private string clearedTitle = "Level 1 cleared";

        [SerializeField, Tooltip(
            "The Level's Rooms, in the order the Route walks through them. " +
            "Matched by order to the Rooms of the Level scene. The Route's last Station is the end beat's, not a fight.")]
        private LevelRoomDefinition[] rooms = Array.Empty<LevelRoomDefinition>();

        [SerializeField, Min(0f), Tooltip(
            "A light at or above this intensity lights the Spawn points within its reach, so no Enemy enters there. " +
            "Lights that are off never count.")]
        private float spawnPointLitIntensity = 0.05f;

        [SerializeField, Min(0f), Tooltip(
            "Stragglers only enter through Spawn points at least this many metres ahead of the character along the way it walks, " +
            "so they come at it rather than trail behind.")]
        private float stragglerMinMetresAhead = 2f;

        [SerializeField] private LevelStartSettings start = new();
        [SerializeField] private LevelEndSettings end = new();

        public string Title => title;
        public string ClearedTitle => clearedTitle;
        public IReadOnlyList<LevelRoomDefinition> Rooms => rooms;
        public float SpawnPointLitIntensity => spawnPointLitIntensity;
        public float StragglerMinMetresAhead => stragglerMinMetresAhead;
        public LevelStartSettings Start => start;
        public LevelEndSettings End => end;
    }
}
