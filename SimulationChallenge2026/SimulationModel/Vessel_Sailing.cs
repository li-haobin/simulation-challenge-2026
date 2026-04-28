using System;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents the sailing activity of a vessel along its current segment.
    ///
    /// This is a duration-driven activity:
    /// - Vessels start immediately once the sailing activity is requested.
    /// - Vessels finish automatically once the sailing duration elapses.
    /// - No additional start or finish gate is required.
    ///
    /// The sailing duration is calculated dynamically from:
    /// - the sailing distance of the segment's associated leg; and
    /// - the sailing speed of the vessel's class.
    ///
    /// A small random variation is applied to represent stochastic sailing time.
    /// </summary>
    public class Vessel_Sailing : ActivityHandler<Vessel>
    {
        public Vessel_Sailing(int seed = 0)
            : base(
                  id: nameof(Vessel_Sailing),
                  seed: seed)
        {
            // Use a vessel-specific duration function.
            // The random stream rs is provided by the activity framework.
            T_Duration = (vessel, rs) => GetDuration(vessel, rs);
        }

        /// <summary>
        /// Computes the sailing duration for the given vessel.
        ///
        /// The vessel is assumed to sail along its CurrentSegment.
        /// If CurrentSegment is null, the vessel is not assigned to any sailing segment,
        /// so the duration is zero.
        /// </summary>
        private TimeSpan GetDuration(Vessel vessel, Random rs)
        {
            var currentSegment = vessel.CurrentSegment;

            // If the vessel has no current segment, no sailing is required.
            // This may occur before the vessel enters its first segment or during
            // an immediate transition state.
            if (currentSegment == null)
            {
                return TimeSpan.Zero;
            }

            var leg = RequireNotNull(
                currentSegment.AssociatedLeg,
                nameof(GetDuration),
                $"Vessel {vessel.Index} current segment {currentSegment.SequenceIndex} associated leg");

            var vesselClass = RequireNotNull(
                vessel.VesselClass,
                nameof(GetDuration),
                $"Vessel {vessel.Index} vessel class");

            double speed = vesselClass.SailingSpeed;

            Require(
                speed > 0,
                nameof(GetDuration),
                $"Vessel {vessel.Index} has non-positive sailing speed: {speed}.");

            double hours = leg.SailingDistance / speed;

            // Apply a small stochastic variation to sailing duration.
            // The factor ranges from 0.95 to 1.05.
            double variationFactor = 0.95 + 0.10 * rs.NextDouble();
            hours *= variationFactor;

            return TimeSpan.FromHours(hours);
        }
    }
}