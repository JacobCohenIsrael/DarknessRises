using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The Colossus, the first Boss: its hit points and phase thresholds, its stomp timings (flash to impact, Relocate
    /// window, foot planted time), the damage a stomp does, the Boss Room's Positions, and how the fight looks and
    /// sounds. Never changed at runtime (ADR 0003).
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Colossus Definition", fileName = "Colossus", order = 45)]
    public sealed class ColossusDefinition : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("Hit points in Level 1. The Ramp scales them for later Levels.")]
        private int hitPoints = 50;

        [SerializeField] private ColossusPhaseThresholds phases = new();

        [SerializeField] private ColossusStompTiming stomp = new();

        [SerializeField, Min(0), Tooltip("Hit points a stomp takes from the character it lands on.")]
        private int stompDamage = 2;

        [SerializeField, Range(0f, 1f), Tooltip(
            "The chance that a stomp aims at the Position the character stands on. Otherwise it aims at another one.")]
        private float characterTargetChance = 0.75f;

        [SerializeField, Tooltip(
            "The Boss Room's Positions, in metres on the floor (x east, y north) from the Boss Room's centre. " +
            "The first is where the character walks in and where it walks out from.")]
        private Vector2[] positions =
        {
            new(0f, -4.5f),
            new(0f, 1f),
            new(-7.5f, 1f),
            new(7.5f, 1f),
            new(0f, 6.5f),
        };

        [SerializeField] private RelocateSettings relocate = new();

        [SerializeField] private BossCameraSettings camera = new();

        [SerializeField] private ColossusLookSettings look = new();

        [Header("Sounds")]
        [SerializeField, Tooltip("Its growl: the Rumble before each flash in its fight.")]
        private AssetReferenceT<AudioClip> growlClip;

        [SerializeField, Tooltip("The foot landing.")]
        private AssetReferenceT<AudioClip> stompClip;

        [SerializeField, Tooltip("A hit on the weak spot.")]
        private AssetReferenceT<AudioClip> hurtClip;

        [SerializeField, Tooltip("The Colossus beaten.")]
        private AssetReferenceT<AudioClip> defeatClip;

        [SerializeField, Tooltip("The character's dash to a Position.")]
        private AssetReferenceT<AudioClip> relocateClip;

        public int HitPoints => hitPoints;
        public ColossusPhaseThresholds Phases => phases;
        public ColossusStompTiming Stomp => stomp;
        public int StompDamage => stompDamage;
        public float CharacterTargetChance => characterTargetChance;

        /// <summary>The Positions as offsets on the floor from the Boss Room's centre: x east, y north.</summary>
        public IReadOnlyList<Vector2> Positions => positions;

        public RelocateSettings Relocate => relocate;
        public BossCameraSettings Camera => camera;
        public ColossusLookSettings Look => look;
        public AssetReferenceT<AudioClip> GrowlClip => growlClip;
        public AssetReferenceT<AudioClip> StompClip => stompClip;
        public AssetReferenceT<AudioClip> HurtClip => hurtClip;
        public AssetReferenceT<AudioClip> DefeatClip => defeatClip;
        public AssetReferenceT<AudioClip> RelocateClip => relocateClip;

        /// <summary>The Positions in world space, around <paramref name="roomCentre"/> on its floor.</summary>
        public Vector3[] PositionsAround(Vector3 roomCentre)
        {
            var world = new Vector3[positions.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                world[i] = roomCentre + new Vector3(positions[i].x, 0f, positions[i].y);
            }

            return world;
        }

        private void OnValidate()
        {
            if (positions == null || positions.Length == 0)
            {
                positions = new[] { Vector2.zero };
            }
        }
    }
}
