namespace DarknessRises.Game.Audio
{
    /// <summary>Each category's percent before the player has moved its slider.</summary>
    public readonly struct VolumeDefaults
    {
        public VolumeDefaults(int sound, int ambient, int music)
        {
            Sound = VolumeCurve.Clamp(sound);
            Ambient = VolumeCurve.Clamp(ambient);
            Music = VolumeCurve.Clamp(music);
        }

        public int Sound { get; }
        public int Ambient { get; }
        public int Music { get; }

        public int For(AudioCategory category) => category switch
        {
            AudioCategory.Ambient => Ambient,
            AudioCategory.Music => Music,
            _ => Sound,
        };
    }
}
