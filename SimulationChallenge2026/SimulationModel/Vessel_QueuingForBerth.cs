using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents vessels waiting in queue for berth assignment.
    ///
    /// Start side:
    /// - No gating. Vessels enter the queue immediately.
    ///
    /// Finish side:
    /// - Controlled by berth signals.
    /// - Each berth signal releases exactly one vessel, identified by berth.OccupyingVessel.
    /// </summary>
    public class Vessel_QueuingForBerth : ActivityHandler<Vessel>
    {
        /// <summary>
        /// Finish signal tokens keyed by berth.
        /// Each token allows one vessel assigned to that berth to leave the queue.
        /// </summary>
        private readonly HashSet<Berth> Q_FinishSignals = new();

        public Vessel_QueuingForBerth(int seed = 0)
            : base(
                  id: nameof(Vessel_QueuingForBerth),
                  seed: seed)
        {
        }

        /// <summary>
        /// External signal indicating that a berth is ready to admit its occupying vessel.
        /// </summary>
        public void SignalFinish(Berth berth)
        {
            if (berth == null)
                throw new ArgumentNullException(nameof(berth));

            Log(nameof(SignalFinish), berth);

            if (Q_FinishSignals.Add(berth))
            {
                Schedule(AttemptFinish);
            }
        }

        /// <summary>
        /// Attempts to release queued vessels based on berth-specific finish signals.
        ///
        /// For each signalled berth:
        /// 1. Identify the occupying vessel from the berth.
        /// 2. Check whether that vessel is ready to finish this activity.
        /// 3. Assign the berth to vessel.CurrentBerth.
        /// 4. Finish the vessel.
        /// </summary>
        protected override void AttemptFinish()
        {
            Log(nameof(AttemptFinish));

            foreach (var berth in Q_FinishSignals.ToList())
            {
                var vessel = RequireNotNull(
                    berth.OccupyingVessel,
                    nameof(AttemptFinish),
                    $"Berth {berth.Index} occupying vessel");

                if (!D_LoadsReadyFinish.Contains(vessel))
                    continue;

                Q_FinishSignals.Remove(berth);

                // Assign the berth to the vessel before it leaves the queue.
                vessel.CurrentBerth = berth;

                Finish(vessel);
            }
        }
    }
}