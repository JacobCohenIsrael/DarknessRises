using DarknessRises.Game.Content;
using DarknessRises.Game.Enemies.Tells;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// A pooled Enemy in the scene. It chases the character with its NavMesh agent, and shows what its
    /// <see cref="EnemyBehaviour"/> decides: a Wind-up (flaring eyes, rearing back, a sound), the attack (a lunge that
    /// hurts the character if it lands) and a Stagger (knocked back). It forwards Shots to its <see cref="Enemies.Enemy"/>
    /// rule, and when that dies, lies still, sinks into the floor and fades out, then goes back to its pool.
    /// </summary>
    /// <remarks>
    /// Everything it does runs on <see cref="EnemyTime"/>, never the global time scale, so Slow-mo slows Enemies
    /// alone: frame time (including the Wind-up, recovery and Stagger), NavMesh agent speed, and its sounds through
    /// the Enemy mixer group.
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

        [SerializeField, Tooltip("Shows the Enemy's Tells: glinting eyes and positional sounds.")]
        private EnemyTellView tells;

        [SerializeField, Tooltip("Every renderer that sinks and fades out when the Enemy dies.")]
        private Renderer[] renderers = new Renderer[0];

        [SerializeField, Tooltip("Transparent material the renderers switch to while the dead Enemy fades out.")]
        private Material fadeMaterial;

        [SerializeField, Tooltip("Renderers that flare during the Wind-up, e.g. the eyes. Also listed in the renderers above.")]
        private Renderer[] windUpRenderers = new Renderer[0];

        [SerializeField, Tooltip("Bright material the Wind-up renderers switch to while the Enemy winds up.")]
        private Material windUpMaterial;

        private IObjectPool<EnemyView> pool;
        private EnemyDefinition definition;
        private Material[] livingMaterials;
        private Material[] windUpLivingMaterials;
        private Material fadeInstance;
        private Color fadeColor;
        private Transform target;
        private IEnemyAttackTarget attackTarget;
        private float secondsUntilPathRefresh;
        private float secondsDead;
        private Vector3 deathPosition;
        private bool isHoldingStill;
        private bool isWindUpShown;
        private float facingYaw;
        private EnemyTime time;
        private AudioClip spawnClip;
        private AudioClip hurtClip;
        private AudioClip deathClip;
        private AudioClip windUpClip;
        private AudioClip attackClip;
        private float agentTimeScale = EnemyTime.NormalScale;

        // A push-back (Continue's burst): slides at this velocity, in Enemy time, until the seconds run out.
        private Vector3 pushVelocity;
        private float pushSecondsLeft;

        // Its Enemy type's, scaled by the Ramp of the Level it fights in.
        private int hitPoints;
        private float speed;

        public Enemy Enemy { get; } = new();

        public EnemyBehaviour Behaviour { get; private set; }

        /// <summary>
        /// Called once, when the pool creates this Enemy. Its sounds come from <paramref name="content"/>, already loaded.
        /// <paramref name="ramp"/> scales its authored hit points and speed for the Level it fights in.
        /// </summary>
        public void Prepare(
            IObjectPool<EnemyView> owner,
            EnemyDefinition enemyDefinition,
            EnemyTime enemyTime,
            ILoadedContent content,
            RampStep ramp)
        {
            pool = owner;
            definition = enemyDefinition;
            time = enemyTime;
            hitPoints = ramp.EnemyHitPoints(definition.HitPoints);
            speed = ramp.EnemySpeed(definition.Speed);

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
            spawnClip = content.Clip(audio.SpawnClip);
            hurtClip = content.Clip(audio.HurtClip);
            deathClip = content.Clip(audio.DeathClip);
            windUpClip = content.Clip(audio.WindUpClip);
            attackClip = content.Clip(audio.AttackClip);

            // Before the living materials are remembered, so the eyes' glint material is what each spawn restores.
            tells.Prepare(definition.Tells, audio, time, content);

            livingMaterials = new Material[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                livingMaterials[i] = renderers[i].sharedMaterial;
            }

            windUpLivingMaterials = new Material[windUpRenderers.Length];
            for (var i = 0; i < windUpRenderers.Length; i++)
            {
                windUpLivingMaterials[i] = windUpRenderers[i].sharedMaterial;
            }

            fadeInstance = new Material(fadeMaterial) { name = $"{definition.name} Fade" };
            fadeColor = fadeInstance.GetColor(BaseColorId);

            var attack = definition.Attack;
            Behaviour = new EnemyBehaviour(Enemy, attack.WindUpSeconds, attack.Damage, attack.CooldownSeconds, attack.StaggerSeconds);
            Behaviour.WindUpStarted += OnWindUpStarted;
            Behaviour.AttackLanded += OnAttackLanded;
            Behaviour.AttackMissed += OnAttackMissed;

            Enemy.Hurt += OnHurt;
            Enemy.Died += OnDied;
        }

        /// <summary>
        /// Brings the Enemy in at <paramref name="position"/>, alive, hunting <paramref name="hunted"/>. It attacks the
        /// <see cref="IEnemyAttackTarget"/> on the hunted object, if there is one.
        /// </summary>
        public void Spawn(Vector3 position, Transform hunted)
        {
            target = hunted;
            target.TryGetComponent(out attackTarget);
            var facing = target.position - position;
            facing.y = 0f;
            var rotation = facing.sqrMagnitude > 0f ? Quaternion.LookRotation(facing, Vector3.up) : Quaternion.identity;

            // Placed before it wakes, so its agent starts on the NavMesh at the Spawn point.
            transform.SetPositionAndRotation(position, rotation);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = livingMaterials[i];
            }

            isWindUpShown = false;
            hitbox.enabled = true;
            gameObject.SetActive(true);
            agent.enabled = true;
            agent.Warp(position);
            agent.isStopped = false;
            agent.updateRotation = true;
            ScaleAgentTime(time.Scale);
            isHoldingStill = false;
            secondsUntilPathRefresh = 0f;
            pushSecondsLeft = 0f;

            Enemy.Spawn(hitPoints);
            Behaviour.Begin();
            tells.Wake(target);
            Play(spawnClip);
        }

        public void TakeShot(int damage)
        {
            Enemy.TakeHit(damage);
        }

        /// <summary>
        /// Shoves the living Enemy across the floor at <paramref name="velocity"/> for <paramref name="seconds"/> of Enemy
        /// time, staying on the NavMesh. It stops chasing while it slides, then picks the hunt up again.
        /// </summary>
        public void PushBack(Vector3 velocity, float seconds)
        {
            if (!Enemy.IsAlive || seconds <= 0f)
            {
                return;
            }

            pushVelocity = velocity;
            pushSecondsLeft = seconds;
        }

        private void Update()
        {
            // Enemy time: Slow-mo slows the Wind-up, recovery and Stagger along with the chase.
            var deltaSeconds = time.Scaled(Time.deltaTime);
            if (!Enemy.IsAlive)
            {
                SinkAndFade(deltaSeconds);
                return;
            }

            if (time.Scale != agentTimeScale)
            {
                ScaleAgentTime(time.Scale);
            }

            Behaviour.Tick(deltaSeconds, IsCharacterInRange());
            ShowWindUp(Behaviour.State == EnemyBehaviourState.WindingUp);

            if (pushSecondsLeft > 0f)
            {
                Slide(deltaSeconds);
            }
            else if (Behaviour.IsChasing)
            {
                Hunt(deltaSeconds);
            }
            else
            {
                HoldStill(deltaSeconds);
            }
        }

        private void OnDestroy()
        {
            Enemy.Hurt -= OnHurt;
            Enemy.Died -= OnDied;
            if (Behaviour != null)
            {
                Behaviour.WindUpStarted -= OnWindUpStarted;
                Behaviour.AttackLanded -= OnAttackLanded;
                Behaviour.AttackMissed -= OnAttackMissed;
            }

            Destroy(fadeInstance);
        }

        private bool IsCharacterInRange()
        {
            if (attackTarget == null || !attackTarget.CanBeAttacked)
            {
                return false;
            }

            var offset = target.position - transform.position;
            offset.y = 0f;
            var range = definition.Attack.Range;
            return offset.sqrMagnitude <= range * range;
        }

        private void Hunt(float deltaSeconds)
        {
            if (isHoldingStill)
            {
                // Back on its feet: straighten up and let the agent steer again.
                isHoldingStill = false;
                transform.rotation = Quaternion.Euler(0f, facingYaw, 0f);
                agent.updateRotation = true;
                agent.isStopped = false;
                secondsUntilPathRefresh = 0f;
            }

            secondsUntilPathRefresh -= deltaSeconds;
            if (secondsUntilPathRefresh > 0f)
            {
                return;
            }

            secondsUntilPathRefresh = definition.PathRefreshSeconds;
            agent.SetDestination(target.position);
        }

        // Winding up, recovering or reeling: stands its ground, turns to the character and leans to show what it's doing.
        private void HoldStill(float deltaSeconds)
        {
            StopAgent();

            var toCharacter = target.position - transform.position;
            toCharacter.y = 0f;
            if (toCharacter.sqrMagnitude > 0f)
            {
                var targetYaw = Quaternion.LookRotation(toCharacter, Vector3.up).eulerAngles.y;
                facingYaw = Mathf.MoveTowardsAngle(facingYaw, targetYaw, definition.TurnSpeed * deltaSeconds);
            }

            transform.rotation = Quaternion.Euler(Lean(), facingYaw, 0f);
        }

        // Pushed back: the agent stops steering and the Enemy slides along the NavMesh. Once the push is over it chases
        // again (Hunt restarts the agent) or carries on with its Wind-up or recovery (HoldStill).
        private void Slide(float deltaSeconds)
        {
            StopAgent();
            var step = Mathf.Min(deltaSeconds, pushSecondsLeft);
            pushSecondsLeft -= step;
            if (agent.isOnNavMesh)
            {
                agent.Move(pushVelocity * step);
            }
        }

        private void StopAgent()
        {
            if (isHoldingStill)
            {
                return;
            }

            isHoldingStill = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.updateRotation = false;
            facingYaw = transform.eulerAngles.y;
        }

        // Degrees of pitch: positive leans forward, negative rears back.
        private float Lean()
        {
            var attack = definition.Attack;
            var progress = Behaviour.StateProgress;
            return Behaviour.State switch
            {
                EnemyBehaviourState.WindingUp => -attack.WindUpLeanDegrees * progress,
                EnemyBehaviourState.Recovering => attack.AttackLungeDegrees * (1f - progress),
                EnemyBehaviourState.Staggered => -attack.StaggerLeanDegrees * (1f - progress),
                _ => 0f,
            };
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
            agent.speed = speed * scale;
            agent.acceleration = definition.Acceleration * scale * scale;
            agent.angularSpeed = definition.TurnSpeed * scale;
        }

        private void ShowWindUp(bool shown)
        {
            if (shown == isWindUpShown)
            {
                return;
            }

            isWindUpShown = shown;
            for (var i = 0; i < windUpRenderers.Length; i++)
            {
                windUpRenderers[i].sharedMaterial = shown ? windUpMaterial : windUpLivingMaterials[i];
            }
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

        private void OnWindUpStarted()
        {
            Play(windUpClip);
        }

        private void OnAttackLanded(int damage)
        {
            Play(attackClip);
            attackTarget?.TakeAttack(damage);
        }

        private void OnAttackMissed()
        {
            Play(attackClip);
        }

        private void OnHurt(Enemy _)
        {
            Play(hurtClip);
        }

        private void OnDied(Enemy _)
        {
            Play(deathClip);
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
