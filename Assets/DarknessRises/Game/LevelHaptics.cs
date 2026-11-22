using System;
using DarknessRises.Game.Bosses;
using DarknessRises.Game.Death;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Haptics;
using DarknessRises.Game.Health;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Pausing;
using DarknessRises.Game.Scoring;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Waves;
using DarknessRises.Game.Weapons;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game
{
    /// <summary>
    /// The Level's Haptics: listens to the moments the Level's owners already report (a Grenade going off, a Shot
    /// landing, a stomp, the heartbeat) and asks the game's <see cref="IHaptics"/> for the effect each one feels like.
    /// Holds every effect while the Level is frozen or an ad plays. Made by <see cref="GameRoot"/> once the gameplay has
    /// started, and disposed with the scene; every handler is made once, so nothing is allocated while playing.
    /// </summary>
    public sealed class LevelHaptics : IDisposable
    {
        private readonly IHaptics haptics;
        private readonly HapticsDefinition definition;
        private readonly GrenadeThrowerView grenades;
        private readonly PrimaryWeaponView primaryWeapon;
        private readonly LevelView level;
        private readonly RouteWavesView routeWaves;
        private readonly CharacterHealth health;
        private readonly HurtVignetteView hurtVignette;
        private readonly Combo combo;
        private readonly CharacterSkills skills;
        private readonly LightningView lightning;
        private readonly ScoreBoost scoreBoost;
        private readonly EndScreenView endScreen;
        private readonly GamePause pause;

        private readonly Action<Vector3> onGrenadeExploded;
        private readonly Action<ShotOutcome, Vector3> onShotLanded;
        private readonly Action onSwapped;
        private readonly Action<int> onRanDry;
        private readonly Action onReloadFinished;
        private readonly Action<EnemyView> onEnemyKilled;
        private readonly Action<ColossusPattern> onBossFightBegan;
        private readonly Action<float> onBossFlashRequested;
        private readonly Action<int, bool> onBossLanded;
        private readonly Action onBossDefeated;
        private readonly Action onHurt;
        private readonly Action onDied;
        private readonly Action onRestored;
        private readonly Action<HeartbeatTier> onHeartbeat;
        private readonly Action<int> onComboChanged;
        private readonly Action<Skill> onFlare;
        private readonly Action<Skill> onAdrenaline;
        private readonly Action<Skill> onLureThrown;
        private readonly Action<Lure> onLureLanded;
        private readonly Action<EnemyView> onLoomed;
        private readonly Action onScoreBoostStarted;
        private readonly Action onNewBest;
        private readonly Action onCreditsLanded;
        private readonly Action onAdBonusLanded;
        private readonly Action<PauseState> onPauseChanged;

        private readonly WeaponSwitchBeat switchBeat;
        private readonly PrimaryWeaponLoadout loadout;
        private ColossusPattern boss;
        private int lastHitPoints;
        private int lastComboMultiplier;

        public LevelHaptics(
            IHaptics haptics,
            HapticsDefinition definition,
            GrenadeThrowerView grenades,
            PrimaryWeaponView primaryWeapon,
            LevelView level,
            RouteWavesView routeWaves,
            CharacterHealth health,
            HurtVignetteView hurtVignette,
            Combo combo,
            CharacterSkills skills,
            LightningView lightning,
            ScoreBoost scoreBoost,
            EndScreenView endScreen,
            GamePause pause)
        {
            this.haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));
            this.definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            this.grenades = grenades;
            this.primaryWeapon = primaryWeapon;
            this.level = level;
            this.routeWaves = routeWaves;
            this.health = health;
            this.hurtVignette = hurtVignette;
            this.combo = combo;
            this.skills = skills;
            this.lightning = lightning;
            this.scoreBoost = scoreBoost;
            this.endScreen = endScreen;
            this.pause = pause;

            onGrenadeExploded = _ => haptics.Play(HapticEffect.GrenadeExplosion);
            onShotLanded = OnShotLanded;
            onSwapped = () => haptics.Play(HapticEffect.WeaponSwitch);
            onRanDry = _ => haptics.Play(HapticEffect.RanDry);
            onReloadFinished = () => haptics.Play(HapticEffect.ReloadDone);
            onEnemyKilled = _ => haptics.Play(HapticEffect.EnemyKilled);
            onBossFightBegan = OnBossFightBegan;
            onBossFlashRequested = OnBossFlashRequested;
            onBossLanded = (_, _) => haptics.Play(HapticEffect.BossStomp);
            onBossDefeated = () => haptics.Play(HapticEffect.BossDefeated);
            onHurt = OnHurt;
            onDied = OnDied;
            onRestored = OnRestored;
            onHeartbeat = tier => haptics.Play(tier == HeartbeatTier.Fast ? HapticEffect.HeartbeatFast : HapticEffect.HeartbeatSlow);
            onComboChanged = OnComboChanged;
            onFlare = _ => haptics.Play(HapticEffect.Flare);
            onAdrenaline = _ => haptics.Play(HapticEffect.Adrenaline);
            onLureThrown = _ => haptics.Play(HapticEffect.LureThrow);
            onLureLanded = _ => haptics.Play(HapticEffect.LureLand);
            onLoomed = _ => haptics.Play(HapticEffect.LightningNear);
            onScoreBoostStarted = () => haptics.Play(HapticEffect.ScoreBoost);
            onNewBest = () => haptics.Play(HapticEffect.NewBest);
            onCreditsLanded = () => haptics.Play(HapticEffect.CreditsLanded);
            onAdBonusLanded = () => haptics.Play(HapticEffect.AdBonus);
            onPauseChanged = state => haptics.SetHeld(HapticsHold.Paused, state != PauseState.Playing);

            grenades.Exploded += onGrenadeExploded;
            primaryWeapon.ShotLanded += onShotLanded;
            switchBeat = primaryWeapon.Beat;
            if (switchBeat != null)
            {
                switchBeat.Swapped += onSwapped;
            }

            loadout = primaryWeapon.Loadout;
            loadout.RanDry += onRanDry;
            for (var i = 0; i < loadout.Count; i++)
            {
                loadout[i].ReloadFinished += onReloadFinished;
            }

            if (level != null)
            {
                level.EnemyKilled += onEnemyKilled;
                if (level.Boss != null)
                {
                    level.Boss.FightBegan += onBossFightBegan;
                }
            }
            else if (routeWaves != null)
            {
                routeWaves.EnemyKilled += onEnemyKilled;
            }

            lastHitPoints = health.HitPoints;
            health.Hurt += onHurt;
            health.Died += onDied;
            health.Restored += onRestored;
            if (hurtVignette != null)
            {
                hurtVignette.Beat += onHeartbeat;
            }

            lastComboMultiplier = combo.Multiplier;
            combo.MultiplierChanged += onComboChanged;
            skills.Flare.Skill.Activated += onFlare;
            skills.Adrenaline.Skill.Activated += onAdrenaline;
            skills.Lure.Skill.Activated += onLureThrown;
            skills.Lure.Landed += onLureLanded;
            lightning.Loomed += onLoomed;
            scoreBoost.Started += onScoreBoostStarted;
            endScreen.NewBestStamped += onNewBest;
            endScreen.CreditsLanded += onCreditsLanded;
            endScreen.AdBonusLanded += onAdBonusLanded;
            pause.StateChanged += onPauseChanged;
            haptics.SetHeld(HapticsHold.Paused, pause.IsFrozen);
        }

        /// <summary>A Station's fight is cleared with <paramref name="grade"/>.</summary>
        public void StationCleared(Grade grade) =>
            haptics.Play(grade == Grade.S ? HapticEffect.StationClearS : HapticEffect.StationClear);

        /// <summary>Holds every effect while an ad plays over the Level.</summary>
        public void ShowingAd(bool showing) => haptics.SetHeld(HapticsHold.Ad, showing);

        public void Dispose()
        {
            // The views may already be destroyed on scene unload: the events are plain C#, so unsubscribing is safe.
            grenades.Exploded -= onGrenadeExploded;
            primaryWeapon.ShotLanded -= onShotLanded;
            if (switchBeat != null)
            {
                switchBeat.Swapped -= onSwapped;
            }

            loadout.RanDry -= onRanDry;
            for (var i = 0; i < loadout.Count; i++)
            {
                loadout[i].ReloadFinished -= onReloadFinished;
            }

            if (level != null)
            {
                level.EnemyKilled -= onEnemyKilled;
                if (level.Boss != null)
                {
                    level.Boss.FightBegan -= onBossFightBegan;
                }
            }
            else if (routeWaves != null)
            {
                routeWaves.EnemyKilled -= onEnemyKilled;
            }

            ForgetBoss();
            health.Hurt -= onHurt;
            health.Died -= onDied;
            health.Restored -= onRestored;
            if (hurtVignette != null)
            {
                hurtVignette.Beat -= onHeartbeat;
            }

            combo.MultiplierChanged -= onComboChanged;
            skills.Flare.Skill.Activated -= onFlare;
            skills.Adrenaline.Skill.Activated -= onAdrenaline;
            skills.Lure.Skill.Activated -= onLureThrown;
            skills.Lure.Landed -= onLureLanded;
            lightning.Loomed -= onLoomed;
            scoreBoost.Started -= onScoreBoostStarted;
            endScreen.NewBestStamped -= onNewBest;
            endScreen.CreditsLanded -= onCreditsLanded;
            endScreen.AdBonusLanded -= onAdBonusLanded;
            pause.StateChanged -= onPauseChanged;

            // The game's Haptics outlive the Level: nothing it held stays held.
            haptics.SetHeld(HapticsHold.Paused, false);
            haptics.SetHeld(HapticsHold.Ad, false);
        }

        // A hit only: a miss and a Critter feel like nothing.
        private void OnShotLanded(ShotOutcome outcome, Vector3 _)
        {
            if (outcome == ShotOutcome.HurtEnemy)
            {
                haptics.Play(HapticEffect.EnemyHit);
            }
        }

        private void OnHurt()
        {
            var damage = lastHitPoints - health.HitPoints;
            lastHitPoints = health.HitPoints;
            haptics.Play(HapticEffect.Hurt, definition.HurtStrength(damage));
        }

        private void OnDied()
        {
            lastHitPoints = health.HitPoints;
            haptics.Play(HapticEffect.Death);
        }

        private void OnRestored() => lastHitPoints = health.HitPoints;

        // Only a step up: the Combo breaking is felt as the miss or the hit that broke it.
        private void OnComboChanged(int multiplier)
        {
            if (multiplier > lastComboMultiplier)
            {
                haptics.Play(HapticEffect.ComboUp);
            }

            lastComboMultiplier = multiplier;
        }

        private void OnBossFightBegan(ColossusPattern pattern)
        {
            ForgetBoss();
            boss = pattern;
            boss.FlashRequested += onBossFlashRequested;
            boss.Landed += onBossLanded;
            boss.Defeated += onBossDefeated;
        }

        // The growl before a stomp is the wind-up; a feint's late second flash isn't.
        private void OnBossFlashRequested(float _)
        {
            if (boss.Step == ColossusStep.Growling)
            {
                haptics.Play(HapticEffect.BossWindUp);
            }
        }

        private void ForgetBoss()
        {
            if (boss == null)
            {
                return;
            }

            boss.FlashRequested -= onBossFlashRequested;
            boss.Landed -= onBossLanded;
            boss.Defeated -= onBossDefeated;
            boss = null;
        }
    }
}
