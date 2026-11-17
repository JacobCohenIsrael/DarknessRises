using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// An Enemy type's Tells: the cues that give it away in Darkness without lighting it.
    /// Tells only show where an Enemy is; nothing an Enemy does depends on them or on being lit.
    /// </summary>
    [Serializable]
    public sealed class EnemyTellSettings
    {
        [SerializeField] private EyeGlintSettings eyeGlint = new();

        [SerializeField] private TellSoundSettings sounds = new();

        public EyeGlintSettings EyeGlint => eyeGlint;
        public TellSoundSettings Sounds => sounds;
    }
}
