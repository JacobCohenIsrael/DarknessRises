using DarknessRises.Game.Routes;

namespace DarknessRises.Game.Player
{
    /// <summary>How one leg bends for the plant stance, in degrees, in the plane the legs walk in.</summary>
    public readonly struct LegBend
    {
        public LegBend(float thighForwardDegrees, float kneeDegrees)
        {
            ThighForwardDegrees = thighForwardDegrees;
            KneeDegrees = kneeDegrees;
        }

        /// <summary>How far the thigh swings forward from the hip. Negative swings it back.</summary>
        public float ThighForwardDegrees { get; }

        /// <summary>How far the knee bends, folding the shin back under the thigh.</summary>
        public float KneeDegrees { get; }

        /// <summary>How far the shin leans back from upright once the thigh has swung and the knee has bent.</summary>
        public float ShinBackDegrees => KneeDegrees - ThighForwardDegrees;
    }

    /// <summary>
    /// The stance the character plants in at a Station: a stride with the knees bent, lead foot forward and rear foot
    /// back, as if braced to shoot. It holds it from the Ready beat until the clear is called, then stands up again.
    /// The two legs bend so both feet stay at the same height, however far the stride is.
    /// </summary>
    public static class PlantStance
    {
        /// <summary>Whether the character stands planted during <paramref name="beat"/>.</summary>
        public static bool IsPlantedIn(StationBeat beat)
        {
            return beat == StationBeat.Ready || beat == StationBeat.Fight || beat == StationBeat.Clear;
        }

        /// <summary>
        /// The lead leg: its thigh swings <paramref name="leadDegrees"/> forward and its shin leans
        /// <paramref name="rearDegrees"/> back, scaled by <paramref name="weight"/> (0 standing, 1 planted).
        /// </summary>
        public static LegBend Lead(float leadDegrees, float rearDegrees, float weight)
        {
            return new LegBend(leadDegrees * weight, (leadDegrees + rearDegrees) * weight);
        }

        /// <summary>
        /// The rear leg: its thigh swings <paramref name="rearDegrees"/> back and its shin leans
        /// <paramref name="leadDegrees"/> back, the mirror of the lead leg, so both feet end up as far below the hips.
        /// </summary>
        public static LegBend Rear(float leadDegrees, float rearDegrees, float weight)
        {
            return new LegBend(-rearDegrees * weight, (leadDegrees - rearDegrees) * weight);
        }
    }
}
