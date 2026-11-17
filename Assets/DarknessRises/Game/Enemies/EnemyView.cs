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
    /// <remarks>
    /// Everything it does runs on <see cref="EnemyTime"/>, never the global time scale, so Slow-mo slows Enemies
    /// alone: frame time, NavMesh agent speed, and its sounds through the Enemy mixer group.
    /// </remarks>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyView : MonoBehaviour, IShotTarget
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private NavMeshAgent agent;

        [SerializeField, Tooltip("What Shots hit. Turned off once the Enemy dies.")]
        private Collider hitbox;

        [SerializeField, Tooltip("Positional source for the Enemy's sounds.")]
        private AudioSource voice;

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
        private EnemyTime time;
        private float agentTimeScale = EnemyTime.NormalScale;

        public Enemy Enemy { get; } = new();

        /// <summary>Called once, when the pool creates this Enemy.</summary>
        public void Prepare(IObjectPool<EnemyView> owner, EnemyDefinition enemyDefinition, EnemyTime enemyTime)
        {
            pool = owner;
            definition = enemyDefinition;
            time = enemyTime;

            ScaleAgentTime(EnemyTime.NormalScale);
            agent.stoppingDistance = definition.Attack.Range;
            agent.autoBraking = true;

            var audio = definition.Audio;
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.maxDistance = audio.HearingRange;
            voice.volume = audio.Volume;
            voice.outputAudioMixerGroup = audio.OutputGroup;

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
            ScaleAgentTime(time.Scale);
            secondsUntilPathRefresh = 0f;

            Enemy.Spawn(definition.HitPoints);
            Play(definition.Audio.SpawnClip);
        }

        public void TakeShot(int damage)
        {
            Enemy.TakeHit(damage);
        }

        private void Update()
        {
            var deltaSeconds = time.Scaled(Time.deltaTime);
            if (Enemy.IsAlive)
            {
                if (time.Scale != agentTimeScale)
                {
                    ScaleAgentTime(time.Scale);
                }

                Hunt(deltaSeconds);
            }
            else
            {
                SinkAndFade(deltaSeconds);
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

        /// <summary>
        /// Runs the agent's movement at <paramref name="scale"/> times normal speed: speeds and its current velocity
        /// scale with time, acceleration with time squared, so a slowed Enemy moves exactly like a slowed-down film.
        /// </summary>
        private void ScaleAgentTime(float scale)
        {
            if (agent.isOnNavMesh && agentTimeScale > 0f)
            {
                agent.velocity *= scale / agentTimeScale;
            }

            agentTimeScale = scale;
            agent.speed = definition.Speed * scale;
            agent.acceleration = definition.Acceleration * scale * scale;
            agent.angularSpeed = definition.TurnSpeed * scale;
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
            renderers = GetComponentsInChildren<Renderer>();
        }
    }
}
