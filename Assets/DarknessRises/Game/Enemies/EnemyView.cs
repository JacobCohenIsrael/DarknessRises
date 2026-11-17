using DarknessRises.Game.Enemies.Tells;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// A pooled Enemy in the scene. It chases the character with its NavMesh agent, forwards Shots to its
    /// <see cref="Enemies.Enemy"/> rule, and when that dies, lies still, sinks into the floor and fades out,
    /// then goes back to its pool.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyView : MonoBehaviour, IShotTarget
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private NavMeshAgent agent;

        [SerializeField, Tooltip("What Shots hit. Turned off once the Enemy dies.")]
        private Collider hitbox;

        [SerializeField, Tooltip("Positional source for the Enemy's sounds.")]
        private AudioSource voice;

        [SerializeField, Tooltip("Shows the Enemy's Tells: glinting eyes and positional sounds.")]
        private EnemyTellView tells;

        [SerializeField, Tooltip("Every renderer that sinks and fades out when the Enemy dies.")]
        private Renderer[] renderers = new Renderer[0];

        [SerializeField, Tooltip("Transparent material the renderers switch to while the dead Enemy fades out.")]
        private Material fadeMaterial;

        private IObjectPool<EnemyView> pool;
        private EnemyDefinition definition;
        private Material[] livingMaterials;
        private Material fadeInstance;
        private Color fadeColor;
        private Transform target;
        private float secondsUntilPathRefresh;
        private float secondsDead;
        private Vector3 deathPosition;

        public Enemy Enemy { get; } = new();

        /// <summary>Called once, when the pool creates this Enemy.</summary>
        public void Prepare(IObjectPool<EnemyView> owner, EnemyDefinition enemyDefinition)
        {
            pool = owner;
            definition = enemyDefinition;

            agent.speed = definition.Speed;
            agent.acceleration = definition.Acceleration;
            agent.angularSpeed = definition.TurnSpeed;
            agent.stoppingDistance = definition.Attack.Range;
            agent.autoBraking = true;

            var audio = definition.Audio;
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.maxDistance = audio.HearingRange;
            voice.volume = audio.Volume;

            // Before the living materials are remembered, so the eyes' glint material is what each spawn restores.
            tells.Prepare(definition.Tells, audio.HearingRange);

            livingMaterials = new Material[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                livingMaterials[i] = renderers[i].sharedMaterial;
            }

            fadeInstance = new Material(fadeMaterial) { name = $"{definition.name} Fade" };
            fadeColor = fadeInstance.GetColor(BaseColorId);

            Enemy.Hurt += OnHurt;
            Enemy.Died += OnDied;
        }

        /// <summary>Brings the Enemy in at <paramref name="position"/>, alive, hunting <paramref name="hunted"/>.</summary>
        public void Spawn(Vector3 position, Transform hunted)
        {
            target = hunted;
            var facing = target.position - position;
            facing.y = 0f;
            var rotation = facing.sqrMagnitude > 0f ? Quaternion.LookRotation(facing, Vector3.up) : Quaternion.identity;

            // Placed before it wakes, so its agent starts on the NavMesh at the Spawn point.
            transform.SetPositionAndRotation(position, rotation);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = livingMaterials[i];
            }

            hitbox.enabled = true;
            gameObject.SetActive(true);
            agent.enabled = true;
            agent.Warp(position);
            agent.isStopped = false;
            secondsUntilPathRefresh = 0f;

            Enemy.Spawn(definition.HitPoints);
            tells.Wake(target);
            Play(definition.Audio.SpawnClip);
        }

        public void TakeShot(int damage)
        {
            Enemy.TakeHit(damage);
        }

        private void Update()
        {
            if (Enemy.IsAlive)
            {
                Hunt(Time.deltaTime);
            }
            else
            {
                SinkAndFade(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            Enemy.Hurt -= OnHurt;
            Enemy.Died -= OnDied;
            Destroy(fadeInstance);
        }

        private void Hunt(float deltaSeconds)
        {
            secondsUntilPathRefresh -= deltaSeconds;
            if (secondsUntilPathRefresh > 0f)
            {
                return;
            }

            secondsUntilPathRefresh = definition.PathRefreshSeconds;
            agent.SetDestination(target.position);
        }

        private void SinkAndFade(float deltaSeconds)
        {
            secondsDead += deltaSeconds;
            var death = definition.Death;
            var sinking = (secondsDead - death.LingerSeconds) / death.SinkSeconds;
            if (sinking <= 0f)
            {
                return;
            }

            if (sinking >= 1f)
            {
                pool.Release(this);
                return;
            }

            transform.position = deathPosition + Vector3.down * (death.SinkDepth * sinking);
            var color = fadeColor;
            color.a = fadeColor.a * (1f - sinking);
            fadeInstance.SetColor(BaseColorId, color);
        }

        private void OnHurt(Enemy _)
        {
            Play(definition.Audio.HurtClip);
        }

        private void OnDied(Enemy _)
        {
            Play(definition.Audio.DeathClip);
            tells.Silence();

            // Out of the way of Shots and the living, and free to sink through the floor.
            hitbox.enabled = false;
            agent.enabled = false;
            deathPosition = transform.position;
            secondsDead = 0f;

            fadeInstance.SetColor(BaseColorId, fadeColor);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = fadeInstance;
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                voice.PlayOneShot(clip);
            }
        }

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
            hitbox = GetComponent<Collider>();
            voice = GetComponent<AudioSource>();
            tells = GetComponent<EnemyTellView>();
            renderers = GetComponentsInChildren<Renderer>();
        }
    }
}
