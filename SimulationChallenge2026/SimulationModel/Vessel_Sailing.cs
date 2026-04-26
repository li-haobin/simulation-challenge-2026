using System;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents the sailing activity of a vessel along its current partial service route.
    ///
    /// This is a pure duration-driven activity:
    /// - Vessels start immediately upon request (no start gating).
    /// - Vessels finish immediately once their sailing duration elapses (no finish gating).
    ///
    /// The sailing duration is computed dynamically based on leg distance and vessel speed.
    /// </summary>
    public class Vessel_Sailing : ActivityHandler<Vessel>
    {
        public Vessel_Sailing(int seed = 0)
            : base(
                  id: nameof(Vessel_Sailing),
                  seed: seed)
        {
            // Pass rs into duration function
            T_Duration = (vessel, rs) => GetDuration(vessel, rs);
        }

        /// <summary>
        /// Computes the sailing duration of a vessel based on the current partial service route.
        ///
        /// If the vessel has no current partial service route, the duration is zero,
        /// indicating that no sailing is required (e.g., end of route or immediate transition).
        /// </summary>
        private TimeSpan GetDuration(Vessel vessel, Random rs)
        {
            var partialServiceRoute = vessel.CurrentPartialServiceRoute;

            // If no route is assigned, no sailing is required
            if (partialServiceRoute == null)
            {
                return TimeSpan.Zero;
            }

            var leg = partialServiceRoute.AssociatedLeg
                ?? throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | GetDuration | " +
                    $"Vessel {vessel.Index} has no associated leg.");

            var speed = vessel.VesselClass?.SailingSpeed
                ?? throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | GetDuration | " +
                    $"Vessel {vessel.Index} has no vessel class or sailing speed.");

            if (speed <= 0)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | GetDuration | " +
                    $"Vessel {vessel.Index} has non-positive sailing speed.");
            }

            double hours = leg.SailingDistance / speed;

            // Optional stochastic variation (if you kept it earlier)
            double variationFactor = 0.95 + 0.10 * rs.NextDouble();
            hours *= variationFactor;

            return TimeSpan.FromHours(hours);
        }
    }
}