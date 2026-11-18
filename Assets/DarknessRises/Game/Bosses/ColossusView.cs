using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Content;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Skills;
using Unity.Cinemachine;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The Colossus fight in its Boss Room. The rules live in <see cref="ColossusPattern"/> and <see cref="Relocation"/>;
    /// this feeds them time (the pattern in Enemy time, so Slow-mo slows it) and the player's Relocate directions,
    /// and shows what they decide: the raised foot's shadow, the fall, the shockwave, light and camera shake of the
    /// impact, and the planted foot with only its weak spot lit. The Boss Room camera frames the fight.
    /// </summary>
    /// <remarks>
    /// During the fight the character stands where Relocate puts it: this places it after the Route walker each frame.
    /// The fight's first Position is the Route's Boss Station, so the character walks on from there once it's over.
    /// </remarks>
    public sealed class ColossusView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("The middle of the Boss Room's floor. The definition's Positions are measured from here.")]
        private Transform roomCentre;

        [SerializeField, Tooltip("Frames the whole fight. Off until the fight starts; Cinemachine blends to it.")]
        private CinemachineCamera bossCamera;

        [SerializeField, Tooltip("The foot that comes down. Its pivot is the middle of its sole.")]
        private Transform foot;

        [SerializeField, Tooltip("Blocks Shots while the foot is down, so only the weak spot is hit.")]
        private Collider footCollider;

        [SerializeField, Tooltip("The raised foot's shadow on the floor. Black, so it only shows where the floor is lit.")]
        private Renderer shadow;

        [SerializeField, Tooltip("The second shadow: a feint's false one, or the second foot of a chain.")]
        private Renderer secondShadow;

        [SerializeField, Tooltip("Where Shamblers drop from its back, around the Boss Room's edges.")]
        private Transform[] addDropPoints = Array.Empty<Transform>();

        [SerializeField] private Collider weakSpotCollider;
        [SerializeField] private Renderer weakSpotGlow;
        [SerializeField] private Light weakSpotLight;

        [SerializeField, Tooltip("Shadowless burst of light at the impact.")]
        private Light impactLight;

        [SerializeField, Tooltip("Ring of dust that spreads out from the impact.")]
        private Renderer shockwave;

        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private AudioSource voice;

        private ColossusDefinition definition;
        private ColossusLookSettings look;
        private ColossusPattern pattern;
        private Relocation relocation;
        private EnemyTime enemyTime;
        private Transform character;
        private IEnemyAttackTarget attackTarget;
        private PlayerControls controls;
        private MaterialPropertyBlock properties;
        private Color shockwaveColor;
        private Color shadowColor;

        private AudioClip stompClip;
        private AudioClip hurtClip;
        private AudioClip defeatClip;
        private AudioClip relocateClip;

        private bool placesCharacter;
        private float impactLightSecondsLeft;
        private float shockwaveElapsedSeconds = float.PositiveInfinity;
        private float hitFlashSecondsLeft;
        private float defeatLiftElapsedSeconds = float.PositiveInfinity;
        private Vector3[] framedPositions;
        private float framedAspect;
        private Vector3 impactPoint;

        private Skill flare;
        private EnemyPools enemies;
        private SystemRandom random;
        private Vector3[] dropPoints;
        private Action<Enemy> onAddDied;

        /// <summary>The Colossus growls: Lightning should flash this many seconds from now. Its Boss mode does the flashing.</summary>
        public event Action<float> FlashRequested;

        /// <summary>The fight's rules while it runs; null before.</summary>
        public ColossusPattern Pattern => pattern;

        /// <summary>The character's Relocate while the fight runs; null before.</summary>
        public Relocation Relocation => relocation;

        /// <summary>Shamblers that dropped from its back and are still alive. They outlive the Colossus.</summary>
        public int AddsAlive => pattern != null ? pattern.AddsAlive : 0;

        /// <summary>The character's Flare. While it burns, it shows the next stomp's shadows before any Lightning.</summary>
        public void WatchFlare(Skill flareSkill)
        {
            flare = flareSkill;
        }

        private void Awake()
        {
            Hide();
        }

        /// <summary>
        /// Fights the Colossus with the character standing on the Route's Boss Station at <paramref name="bossStation"/>,
        /// until it is beaten and the character is back on that Station. Its hit points are scaled by
        /// <paramref name="rampStep"/>. From phase 2 its Shamblers come from <paramref name="pools"/>.
        /// </summary>
        public async UniTask FightAsync(
            ColossusDefinition colossus,
            RampStep rampStep,
            Vector3 bossStation,
            Transform fightingCharacter,
            EnemyTime time,
            EnemyPools pools,
            ILoadedContent content,
            CancellationToken cancellation)
        {
            enemies = pools;
            Begin(colossus, rampStep, bossStation, fightingCharacter, time, content);
            try
            {
                await UniTask.WaitUntil(this, view => view.pattern.IsDefeated, cancellationToken: cancellation);

                // The foot lifts out of the Room as the character goes back to where it came in.
                relocation.MoveTo(0);
                await UniTask.WaitUntil(
                    this,
                    view => !view.relocation.IsDashing && view.defeatLiftElapsedSeconds >= view.definition.Stomp.LiftSeconds,
                    cancellationToken: cancellation);
            }
            finally
            {
                End();
            }
        }

        /// <summary>A Shot hit the weak spot.</summary>
        public void HitWeakSpot(int damage)
        {
            if (pattern != null && pattern.TakeWeakSpotHit(damage))
            {
                hitFlashSecondsLeft = look.WeakSpotHitFlashSeconds;
            }
        }

        private void Begin(
            ColossusDefinition colossus,
            RampStep rampStep,
            Vector3 bossStation,
            Transform fightingCharacter,
            EnemyTime time,
            ILoadedContent content)
        {
            definition = colossus ? colossus : throw new ArgumentNullException(nameof(colossus));
            look = definition.Look;
            character = fightingCharacter;
            enemyTime = time;
            character.TryGetComponent(out attackTarget);
            character.TryGetComponent(out controls);
            properties ??= new MaterialPropertyBlock();

            stompClip = content.Clip(definition.StompClip);
            hurtClip = content.Clip(definition.HurtClip);
            defeatClip = content.Clip(definition.DefeatClip);
            relocateClip = content.Clip(definition.RelocateClip);

            // The first Position is where the character walked in: the Route's Boss Station.
            var floor = roomCentre.position;
            var positions = definition.PositionsAround(floor);
            positions[0] = new Vector3(bossStation.x, floor.y, bossStation.z);

            relocation = new Relocation(positions, 0, definition.Relocate);
            relocation.DashStarted += OnDashStarted;
            random ??= new SystemRandom();
            pattern = new ColossusPattern(
                definition.Stomp,
                definition.Phases,
                definition.Phase2,
                definition.Phase3,
                definition.CharacterTargetChance,
                rampStep.BossHitPoints(definition.HitPoints),
                relocation,
                random);
            pattern.FlashRequested += OnFlashRequested;
            pattern.ShadowShown += OnShadowShown;
            pattern.Landed += OnLanded;
            pattern.Hurt += OnHurt;
            pattern.Defeated += OnDefeated;
            pattern.AddsDue += OnAddsDue;
            onAddDied ??= OnAddDied;
            dropPoints = new Vector3[addDropPoints.Length];
            for (var i = 0; i < dropPoints.Length; i++)
            {
                dropPoints[i] = addDropPoints[i].position;
            }

            if (controls != null)
            {
                controls.ClearRelocate();
            }

            FrameFight(positions);
            PrepareEffects();
            placesCharacter = true;
            defeatLiftElapsedSeconds = float.PositiveInfinity;
            pattern.Begin();
        }

        private void End()
        {
            placesCharacter = false;
            if (pattern != null)
            {
                pattern.FlashRequested -= OnFlashRequested;
                pattern.ShadowShown -= OnShadowShown;
                pattern.Landed -= OnLanded;
                pattern.Hurt -= OnHurt;
                pattern.Defeated -= OnDefeated;
                pattern.AddsDue -= OnAddsDue;
                relocation.DashStarted -= OnDashStarted;
            }

            // The follow camera takes the end beat back.
            if (bossCamera != null)
            {
                bossCamera.gameObject.SetActive(false);
            }

            Hide();
        }

        // The Boss Room camera takes over from the follow camera: Cinemachine blends to the newest live camera.
        private void FrameFight(Vector3[] positions)
        {
            var settings = definition.Camera;
            var aspect = ViewAspect();
            framedPositions = positions;
            framedAspect = aspect;
            var centre = BossRoomFraming.Centre(positions);
            var distance = BossRoomFraming.Distance(
                positions,
                centre,
                settings.PitchDegrees,
                settings.FieldOfView,
                aspect,
                settings.MarginMetres,
                settings.MinDistance);

            bossCamera.transform.SetPositionAndRotation(
                BossRoomFraming.CameraPosition(centre, settings.PitchDegrees, distance),
                FixedAngleFraming.Rotation(settings.PitchDegrees));
            var lens = bossCamera.Lens;
            lens.FieldOfView = settings.FieldOfView;
            bossCamera.Lens = lens;
            bossCamera.gameObject.SetActive(true);
        }

        private float ViewAspect()
        {
            var main = Camera.main;
            return main != null ? main.aspect : definition.Camera.FallbackAspect;
        }

        private void PrepareEffects()
        {
            shadow.transform.localScale = new Vector3(look.ShadowRadius * 2f, shadow.transform.localScale.y, look.ShadowRadius * 2f);
            secondShadow.transform.localScale = shadow.transform.localScale;
            shadowColor = new Color(0f, 0f, 0f, look.ShadowOpacity);
            SetColor(shadow, shadowColor);
            SetColor(secondShadow, shadowColor);
            shockwaveColor = shockwave.sharedMaterial.GetColor(BaseColorId);

            impactLight.shadows = LightShadows.None;
            impactLight.color = look.ImpactLightColor;
            impactLight.range = look.ImpactLightRange;
            weakSpotLight.shadows = LightShadows.None;
            var glow = look.WeakSpotGlow;
            weakSpotLight.color = glow.maxColorComponent > 1f ? glow / glow.maxColorComponent : glow;
            weakSpotLight.range = look.WeakSpotLightRange;

            var impulse = impulseSource.ImpulseDefinition;
            impulse.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDuration = look.ImpactShakeSeconds;

            voice.playOnAwake = false;
            voice.volume = look.Volume;
        }

        private void Update()
        {
            if (pattern == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;

            // The fight stops with the character's death.
            var characterAlive = attackTarget == null || attackTarget.CanBeAttacked;
            if (!characterAlive)
            {
                relocation.IsEnabled = false;
            }
            else
            {
                // Back from a Continue in the middle of a Relocate window: the window is still open.
                // Flare lights the floor: the next stomp's shadows show before any Lightning.
                if (flare != null && flare.IsActive && pattern.CanRevealEarly)
                {
                    pattern.RevealEarly();
                }

                relocation.IsEnabled = pattern.IsRelocateWindowOpen;
                if (controls != null && controls.TryGetRelocateDirection(out var direction))
                {
                    relocation.TryRelocate(direction);
                }
            }

            // The character dashes at full speed; only the Colossus runs on Enemy time.
            relocation.Tick(deltaSeconds);
            if (pattern.IsDefeated)
            {
                defeatLiftElapsedSeconds += deltaSeconds;
            }
            else if (characterAlive)
            {
                pattern.Tick(enemyTime.Scaled(deltaSeconds));
            }

            ShowFoot();
            ShowEffects(deltaSeconds);
        }

        // After the Route walker has placed the character on its Station.
        private void LateUpdate()
        {
            if (!placesCharacter)
            {
                return;
            }

            character.position = relocation.Point;

            // A rotated phone or resized window keeps every Position in view.
            const float aspectTolerance = 0.01f;
            if (Mathf.Abs(ViewAspect() - framedAspect) > aspectTolerance)
            {
                FrameFight(framedPositions);
            }
        }

        private void OnDestroy()
        {
            End();
        }

        private void ShowFoot()
        {
            var step = pattern.Step;
            var target = pattern.Target;
            var raised = look.RaisedHeight;
            var stomp = definition.Stomp;

            float height;
            switch (step)
            {
                case ColossusStep.Falling:
                    var fall = Mathf.Min(look.FallSeconds, pattern.StepSeconds);
                    height = fall > 0f ? raised * Mathf.Clamp01(pattern.StepSecondsLeft / fall) : 0f;
                    break;
                case ColossusStep.Chaining:
                    // Up off the first Position and down onto the second.
                    var chain = pattern.StepSeconds > 0f ? 1f - pattern.StepSecondsLeft / pattern.StepSeconds : 1f;
                    height = raised * (1f - Mathf.Abs(2f * chain - 1f));
                    if (chain >= 0.5f)
                    {
                        target = pattern.SecondTarget;
                    }

                    break;
                case ColossusStep.Planted:
                    height = 0f;
                    break;
                case ColossusStep.Lifting:
                    height = raised * Lift(stomp.LiftSeconds - pattern.StepSecondsLeft, stomp.LiftSeconds);
                    break;
                case ColossusStep.Defeated:
                    height = raised * Lift(defeatLiftElapsedSeconds, stomp.LiftSeconds);
                    break;
                default:
                    height = raised;
                    break;
            }

            var shown = target >= 0 && height < raised;
            if (foot.gameObject.activeSelf != shown)
            {
                foot.gameObject.SetActive(shown);
            }

            if (shown)
            {
                foot.position = relocation.Positions[target] + Vector3.up * height;
            }

            var planted = pattern.IsWeakSpotExposed;
            footCollider.enabled = planted;
            weakSpotCollider.enabled = planted;
            weakSpotGlow.enabled = planted;
            weakSpotLight.enabled = planted;

            ShowShadow(shadow, pattern.ShownShadow);
            ShowShadow(secondShadow, pattern.ShownSecondShadow);
        }

        private void ShowShadow(Renderer shown, int position)
        {
            shown.enabled = position >= 0;
            if (position >= 0)
            {
                // Just above the floor and the Position's marker.
                shown.transform.position = relocation.Positions[position] + Vector3.up * (shown.transform.localScale.y * 2f);
            }
        }

        // Shamblers drop from its back between stomps, away from the character.
        private void OnAddsDue(int count)
        {
            if (enemies == null || definition.AddType == null || dropPoints.Length == 0)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var drop = ColossusAddDrops.Pick(dropPoints, character.position, definition.AddDropMinMetres, random);
                var add = enemies.Take(definition.AddType);
                add.Spawn(dropPoints[drop], character);
                add.Enemy.Died += onAddDied;
                pattern.AddSpawned();
            }
        }

        private void OnAddDied(Enemy add)
        {
            add.Died -= onAddDied;
            pattern.AddDied();
        }

        private static float Lift(float elapsedSeconds, float liftSeconds)
        {
            return liftSeconds > 0f ? Mathf.Clamp01(elapsedSeconds / liftSeconds) : 1f;
        }

        private void ShowEffects(float deltaSeconds)
        {
            if (weakSpotGlow.enabled)
            {
                hitFlashSecondsLeft = Mathf.Max(0f, hitFlashSecondsLeft - deltaSeconds);
                var boost = hitFlashSecondsLeft > 0f ? look.WeakSpotHitFlashBoost : 1f;
                SetColor(weakSpotGlow, look.WeakSpotGlow * boost);
                weakSpotLight.intensity = look.WeakSpotLightIntensity * boost;
            }

            if (impactLightSecondsLeft > 0f)
            {
                impactLightSecondsLeft = Mathf.Max(0f, impactLightSecondsLeft - deltaSeconds);
                var left = impactLightSecondsLeft / look.ImpactLightSeconds;
                impactLight.intensity = look.ImpactLightIntensity * left * left;
                impactLight.enabled = impactLightSecondsLeft > 0f;
            }

            if (shockwaveElapsedSeconds < look.ShockwaveSeconds)
            {
                shockwaveElapsedSeconds += deltaSeconds;
                var t = Mathf.Clamp01(shockwaveElapsedSeconds / look.ShockwaveSeconds);
                var diameter = look.ShockwaveRadius * 2f * Mathf.Sqrt(t);
                shockwave.transform.position = impactPoint + Vector3.up * (shockwave.transform.localScale.y * 2f);
                shockwave.transform.localScale = new Vector3(diameter, shockwave.transform.localScale.y, diameter);
                var color = shockwaveColor;
                color.a *= 1f - t;
                SetColor(shockwave, color);
                shockwave.enabled = t < 1f;
            }
        }

        private void OnFlashRequested(float rumbleLeadSeconds)
        {
            FlashRequested?.Invoke(rumbleLeadSeconds);
        }

        // A key pressed before the flash doesn't count: the player reads the shadow first.
        private void OnShadowShown(int target)
        {
            if (controls != null)
            {
                controls.ClearRelocate();
            }
        }

        private void OnLanded(int target, bool stomped)
        {
            impactPoint = relocation.Positions[target];
            impactLight.transform.position = impactPoint + Vector3.up * look.ShadowRadius;
            impactLight.intensity = look.ImpactLightIntensity;
            impactLight.enabled = true;
            impactLightSecondsLeft = look.ImpactLightSeconds;
            shockwaveElapsedSeconds = 0f;
            impulseSource.GenerateImpulseWithForce(look.ImpactShakeForce);
            Play(stompClip);

            if (stomped && attackTarget != null)
            {
                attackTarget.TakeAttack(definition.StompDamage);
            }
        }

        private void OnHurt()
        {
            Play(hurtClip);
        }

        private void OnDefeated()
        {
            defeatLiftElapsedSeconds = 0f;
            impulseSource.GenerateImpulseWithForce(look.ImpactShakeForce);
            Play(defeatClip);
        }

        private void OnDashStarted(int from, int to)
        {
            Play(relocateClip);
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                voice.PlayOneShot(clip);
            }
        }

        private void SetColor(Renderer target, Color color)
        {
            properties.SetColor(BaseColorId, color);
            target.SetPropertyBlock(properties);
        }

        private void Hide()
        {
            if (foot != null)
            {
                foot.gameObject.SetActive(false);
            }

            SetEnabled(footCollider, false);
            SetEnabled(weakSpotCollider, false);
            SetEnabled(shadow, false);
            SetEnabled(secondShadow, false);
            SetEnabled(weakSpotGlow, false);
            SetEnabled(weakSpotLight, false);
            SetEnabled(impactLight, false);
            SetEnabled(shockwave, false);
        }

        private static void SetEnabled(Collider component, bool enabled)
        {
            if (component != null)
            {
                component.enabled = enabled;
            }
        }

        private static void SetEnabled(Renderer component, bool enabled)
        {
            if (component != null)
            {
                component.enabled = enabled;
            }
        }

        private static void SetEnabled(Behaviour component, bool enabled)
        {
            if (component != null)
            {
                component.enabled = enabled;
            }
        }
    }
}
