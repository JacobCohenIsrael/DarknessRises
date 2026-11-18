using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Headless
{
    /// <summary>
    /// Keeps a batch-mode Editor silent on the developer's machine (AGENTS.md rule 13). A headless Editor still plays
    /// sound through the speakers in Play Mode, so in batch mode this sets the master volume to zero when the Editor
    /// loads and again on entering Play Mode. Sounds still play as far as the game can tell (<c>AudioSource.isPlaying</c>,
    /// clip and pitch readings are unchanged), so audio can still be checked by reading state. It uses
    /// <see cref="AudioListener.volume"/> rather than the Editor's mute toggle, which would persist into an interactive
    /// Editor opened on the same project later. It does nothing in an interactive Editor.
    /// </summary>
    [InitializeOnLoad]
    internal static class HeadlessAudioMute
    {
        private const float Silent = 0f;

        static HeadlessAudioMute()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            AudioListener.volume = Silent;
            EditorApplication.playModeStateChanged += KeepSilent;
        }

        private static void KeepSilent(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                AudioListener.volume = Silent;
            }
        }
    }
}
