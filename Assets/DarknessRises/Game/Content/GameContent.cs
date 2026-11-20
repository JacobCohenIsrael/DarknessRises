using System.Collections.Generic;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Waves;
using DarknessRises.Game.Weapons;

namespace DarknessRises.Game.Content
{
    /// <summary>
    /// The content a playable scene loads before it starts, read from the definitions' Addressables references:
    /// the Primary weapons' Shots, hit sparks, Gunshots, Firing loops and reload sounds, the Station banners' sounds, the Grenade,
    /// the character's sounds, the storm loop, the end screen's Credits sound, the prefab and sounds of every Enemy type its
    /// fights use, and its Boss's sounds.
    /// </summary>
    public static class GameContent
    {
        /// <param name="configuration">The root configuration.</param>
        /// <param name="primaryWeapons">The Primary weapons the character carries.</param>
        /// <param name="runsLevel">
        /// Whether the scene runs the configuration's Level; otherwise it runs the Route's Waves in one Room (the Test Room).
        /// </param>
        /// <param name="rampStep">The Ramp of the Level the scene runs. Its Runner is loaded only once the Ramp brings Runners.</param>
        public static ContentList For(GameConfiguration configuration, IReadOnlyList<WeaponDefinition> primaryWeapons, bool runsLevel, RampStep rampStep)
        {
            var content = new ContentList();
            for (var i = 0; i < primaryWeapons.Count; i++)
            {
                content.Add(primaryWeapons[i].Shot.Prefab);
                content.Add(primaryWeapons[i].HitSpark.Prefab);
                var sound = primaryWeapons[i].Sound;
                for (var j = 0; j < sound.Gunshots.Count; j++)
                {
                    content.Add(sound.Gunshots[j].Clip);
                }

                content.Add(sound.FiringLoop.Clip);
            }

            content.Add(configuration.ReloadFeedback.Clip);

            var banners = configuration.StationBanners;
            content.Add(banners.Ready.Clip);
            content.Add(banners.Clear.Clip);
            content.Add(banners.Go.Clip);

            var grenade = configuration.Grenade;
            content.Add(grenade.Body.Prefab);
            content.Add(grenade.Explosion.Prefab);
            content.Add(grenade.Explosion.Clip);

            var hitReaction = configuration.Player.HitReaction;
            content.Add(hitReaction.HurtClip);
            content.Add(hitReaction.DeathClip);
            content.Add(configuration.Credits.EndScreen.BonusClip);
            content.Add(configuration.AudioVolumes.SampleClip);

            var boss = runsLevel ? configuration.Level.Boss : null;
            if (boss != null)
            {
                content.Add(boss.GrowlClip);
                content.Add(boss.StompClip);
                content.Add(boss.HurtClip);
                content.Add(boss.DefeatClip);
                content.Add(boss.RelocateClip);
            }

            foreach (var enemyType in EnemyTypes(configuration, runsLevel, rampStep))
            {
                content.Add(enemyType.Prefab);
                var audio = enemyType.Audio;
                content.Add(audio.SpawnClip);
                content.Add(audio.HurtClip);
                content.Add(audio.DeathClip);
                content.Add(audio.WindUpClip);
                content.Add(audio.AttackClip);
                content.Add(enemyType.Tells.Sounds.Clips);
            }

            return content;
        }

        private static HashSet<EnemyDefinition> EnemyTypes(GameConfiguration configuration, bool runsLevel, RampStep rampStep)
        {
            var enemyTypes = new HashSet<EnemyDefinition>();
            if (runsLevel)
            {
                foreach (var room in configuration.Level.Rooms)
                {
                    AddWaves(enemyTypes, room.Stations);
                    if (room.Stragglers.EnemyType != null)
                    {
                        enemyTypes.Add(room.Stragglers.EnemyType);
                    }
                }

                var boss = configuration.Level.Boss;
                if (boss != null && boss.AddType != null)
                {
                    enemyTypes.Add(boss.AddType);
                }

                var runner = configuration.Ramp.Runner;
                if (rampStep.HasRunners && runner != null)
                {
                    enemyTypes.Add(runner);
                }
            }
            else
            {
                AddWaves(enemyTypes, configuration.Route.Stations);
            }

            return enemyTypes;
        }

        private static void AddWaves(HashSet<EnemyDefinition> enemyTypes, IReadOnlyList<StationWavesDefinition> stations)
        {
            foreach (var station in stations)
            {
                foreach (var wave in station.Waves)
                {
                    foreach (var group in wave.Groups)
                    {
                        if (group.EnemyType != null)
                        {
                            enemyTypes.Add(group.EnemyType);
                        }
                    }
                }
            }
        }
    }
}
