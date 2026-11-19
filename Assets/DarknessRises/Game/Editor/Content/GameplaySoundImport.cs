using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Content
{
    /// <summary>
    /// The import settings of a short gameplay sound (a Gunshot, a Firing loop, the reload, the Grenade's bang), set from
    /// code so nothing is set by hand: mono, since every gameplay sound either plays in 2D or is placed in 3D, where a mono
    /// clip places cleanly, and stereo only doubles its memory; Vorbis in the build, decompressed when it loads, so its
    /// first play is on time and seeking into it is exact. On WebGL a clip kept compressed in memory plays late and seeks
    /// loosely. Short clips cost little as PCM; a long loop such as the storm stays compressed in memory instead.
    /// </summary>
    public static class GameplaySoundImport
    {
        /// <summary>Loads the clip at <paramref name="path"/>, reimporting it first if its settings differ.</summary>
        public static AudioClip Load(string path)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                var settings = importer.defaultSampleSettings;
                if (!importer.forceToMono
                    || settings.loadType != AudioClipLoadType.DecompressOnLoad
                    || settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    importer.forceToMono = true;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>
        /// Gives the clip <paramref name="reference"/> points at these import settings, whichever clip was chosen for it,
        /// so a clip referenced before the settings changed gets them too. Does nothing to an empty reference.
        /// </summary>
        public static void ApplyToReferenced(SerializedProperty reference)
        {
            var clip = AddressableContent.Referenced<AudioClip>(reference);
            if (clip != null)
            {
                Load(AssetDatabase.GetAssetPath(clip));
            }
        }
    }
}
