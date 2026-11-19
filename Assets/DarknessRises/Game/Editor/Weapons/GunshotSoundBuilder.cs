using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Weapons
{
    /// <summary>
    /// Builds the sound of the Primary weapons firing: the view on the character that plays them, and the placeholder
    /// Gunshots and Firing loop on each Weapon definition (Free Pack, approved as placeholders) where none is set, so
    /// sounds chosen in the Inspector survive a rebuild. Gives every clip it uses the gameplay import settings.
    /// </summary>
    /// <remarks>
    /// The clips and trims were chosen from their waveforms:
    /// <list type="bullet">
    /// <item><b>Pistol</b>: Hand Gun 1 and 2, whole. Both hit at once and die away within half a second.
    /// Hand Gun 3 starts 30 ms late and rings for over a second; Hand Gun_6 decays slowly, still -20 dB at half a second.</item>
    /// <item><b>Machine gun</b>: the last shot of the Machine Gun 1 burst. The burst is eight shots about 62 ms apart, each
    /// cut off by the next; only the last has its tail. It is the Hand Gun 1 recording, so the Machine gun plays it a
    /// little higher and quieter, and shorter, where it's 40 dB down.</item>
    /// <item><b>Flamethrower</b>: Dragon Spit Fire 1 builds up for a second, roars steadily to about 3.45 s, then
    /// dies away. The loop starts late in the build-up and repeats the steady part.</item>
    /// </list>
    /// </remarks>
    public static class GunshotSoundBuilder
    {
        private const string FreePack = "Assets/Free Pack/";
        private const string PistolClipPath1 = FreePack + "Hand Gun 1.wav";
        private const string PistolClipPath2 = FreePack + "Hand Gun 2.wav";
        private const string MachineGunClipPath = FreePack + "Machine Gun 1.wav";
        private const string FlamethrowerClipPath = FreePack + "Dragon Spit Fire 1.wav";

        // From the waveforms (see the remarks), in clip seconds.
        private const float MachineGunLastShotSeconds = 0.4395f;
        private const float MachineGunShotLengthSeconds = 0.4f;
        private const float MachineGunMinPitch = 1.05f;
        private const float MachineGunMaxPitch = 1.15f;
        private const float MachineGunVolume = 0.55f;
        private const float FlameStartSeconds = 0.6f;
        private const float FlameLoopStartSeconds = 1.05f;
        private const float FlameLoopEndSeconds = 3.45f;

        /// <summary>
        /// Authors the Weapons' sounds where none is set, and adds the view that plays them to <paramref name="character"/>.
        /// Call it once the Game root and the character are built.
        /// </summary>
        public static void Build(GameRoot gameRoot, Transform character)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var configuration = (GameConfiguration)serializedRoot.FindProperty("configuration").objectReferenceValue;
            AuthorSounds(configuration);

            var soundObject = new GameObject("Gunshot Sound", typeof(GunshotSoundView));
            soundObject.transform.SetParent(character, false);
            serializedRoot.FindProperty("gunshotSound").objectReferenceValue = soundObject.GetComponent<GunshotSoundView>();
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Points each Primary weapon's sound at its placeholder, unless one was chosen, with its import settings.</summary>
        public static void AuthorSounds(GameConfiguration configuration)
        {
            AuthorGunshots(configuration.Pistol, minPitch: null, maxPitch: null, volume: null,
                (PistolClipPath1, 0f, 0f), (PistolClipPath2, 0f, 0f));
            AuthorGunshots(configuration.MachineGun, MachineGunMinPitch, MachineGunMaxPitch, MachineGunVolume,
                (MachineGunClipPath, MachineGunLastShotSeconds, MachineGunShotLengthSeconds));
            AuthorFiringLoop(configuration.Flamethrower);

            // Written to disk with the settings' defaults (the Gunshot voices), even when nothing else changed.
            EditorUtility.SetDirty(configuration);
        }

        private static void AuthorGunshots(
            WeaponDefinition weapon, float? minPitch, float? maxPitch, float? volume, params (string path, float start, float length)[] clips)
        {
            var serialized = new SerializedObject(weapon);
            var gunshots = serialized.FindProperty("sound.gunshots");
            if (gunshots.arraySize == 0)
            {
                gunshots.arraySize = clips.Length;
                for (var i = 0; i < clips.Length; i++)
                {
                    var gunshot = gunshots.GetArrayElementAtIndex(i);
                    AddressableContent.Reference(gunshot.FindPropertyRelative("clip"), GameplaySoundImport.Load(clips[i].path), AddressableContent.SharedGroup);
                    gunshot.FindPropertyRelative("startSeconds").floatValue = clips[i].start;
                    gunshot.FindPropertyRelative("lengthSeconds").floatValue = clips[i].length;
                }

                if (minPitch.HasValue)
                {
                    serialized.FindProperty("sound.minPitch").floatValue = minPitch.Value;
                }

                if (maxPitch.HasValue)
                {
                    serialized.FindProperty("sound.maxPitch").floatValue = maxPitch.Value;
                }

                if (volume.HasValue)
                {
                    serialized.FindProperty("sound.volume").floatValue = volume.Value;
                }
            }

            for (var i = 0; i < gunshots.arraySize; i++)
            {
                var clip = gunshots.GetArrayElementAtIndex(i).FindPropertyRelative("clip");
                GameplaySoundImport.ApplyToReferenced(clip);
                MarkReferenced(clip);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
        }

        private static void AuthorFiringLoop(WeaponDefinition weapon)
        {
            var serialized = new SerializedObject(weapon);
            var clip = serialized.FindProperty("sound.firingLoop.clip");
            if (AddressableContent.Referenced<AudioClip>(clip) == null)
            {
                AddressableContent.Reference(clip, GameplaySoundImport.Load(FlamethrowerClipPath), AddressableContent.SharedGroup);
                serialized.FindProperty("sound.firingLoop.startSeconds").floatValue = FlameStartSeconds;
                serialized.FindProperty("sound.firingLoop.loopStartSeconds").floatValue = FlameLoopStartSeconds;
                serialized.FindProperty("sound.firingLoop.loopEndSeconds").floatValue = FlameLoopEndSeconds;
            }

            GameplaySoundImport.ApplyToReferenced(clip);
            MarkReferenced(clip);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
        }

        // Loaded through Addressables with the character's other content.
        private static void MarkReferenced(SerializedProperty clip)
        {
            var asset = AddressableContent.Referenced<AudioClip>(clip);
            if (asset != null)
            {
                AddressableContent.Add(asset, AddressableContent.SharedGroup);
            }
        }
    }
}
