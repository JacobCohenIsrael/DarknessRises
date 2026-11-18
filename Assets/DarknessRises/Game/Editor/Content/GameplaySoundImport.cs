using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Content
{
    /// <summary>
    /// The import settings of a gameplay sound, set from code so nothing is set by hand: mono, since every gameplay
    /// sound either plays in 2D or is placed in 3D, where a mono clip places cleanly, and stereo only doubles its memory;
    /// Vorbis, kept compressed in memory and decoded as it plays, so a long clip never takes its full PCM size.
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
                    || settings.loadType != AudioClipLoadType.CompressedInMemory
                    || settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    importer.forceToMono = true;
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
