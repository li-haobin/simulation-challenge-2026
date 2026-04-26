using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Cargo handling activity at berth.
    /// 
    /// This activity models the loading and unloading operations performed
    /// by quay cranes at a berth.
    /// 
    /// Key characteristics:
    /// - Start requires an external vessel signal
    /// - Start matches vessel → berth
    /// - Duration depends on vessel workload at the current port
    /// </summary>
    public class Berth_HandlingCargo : ActivityHandler<Berth>
    {
        /// <summary>
        /// Start signals carrying vessel information
        /// </summary>
        public HashSet<Vessel> P_StartSignals { get; } = new();

        public Berth_HandlingCargo(int seed = 0)
            : base(id: nameof(Berth_HandlingCargo), seed: seed)
        {
            // Duration is defined based on berth state (resource-centric design)
            T_Duration = (berth, rs) => GetDuration(berth, rs);
        }

        /// <summary>
        /// Computes cargo handling duration based on:
        /// - total TEU to be handled at this port
        /// - quay crane productivity
        /// - number of cranes deployable based on vessel LOA
        /// </summary>
        private TimeSpan GetDuration(Berth berth, Random rs)
        {
            var vessel = berth.OccupyingVessel
                ?? throw new InvalidOperationException(
                $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | GetDuration | " +
                $"Berth {berth} has no occupying vessel.");

            var vesselClass = vessel.VesselClass
                ?? throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | GetDuration | " +
                    $"Vessel {vessel.Index} has no vessel class.");

            // Assumed productivity per quay crane
            const double singleQcProductivityTeuPerHour = 30.0;

            // Shoreline length required per quay crane
            const double shorelinePerQcMeter = 100.0;

            // Determine the number of deployable quay cranes based on vessel LOA
            int qcCount = (int)Math.Floor(vesselClass.LOA / shorelinePerQcMeter);
            qcCount = Math.Max(1, qcCount);

            // Compute workload at the current port:
            // total TEU to discharge + total TEU to load
            int dischargingTeu = vessel.GetDischargingShipmentsAtCurrentPort()
                .Sum(shipment => shipment.TeuSize);

            int loadingTeu = vessel.GetLoadingShipmentsAtCurrentPort()
                .Sum(shipment => shipment.TeuSize);

            int totalTeuToHandle = dischargingTeu + loadingTeu;

            double totalProductivity = qcCount * singleQcProductivityTeuPerHour;
            double hours = totalTeuToHandle / totalProductivity;

            return TimeSpan.FromHours(hours);
        }

        /// <summary>
        /// External signal indicating that a vessel is ready for cargo handling
        /// </summary>
        public void SignalStart(Vessel vessel)
        {
            if (vessel == null)
                throw new ArgumentNullException(nameof(vessel));

            Log("SignalStart", vessel);

            if (vessel.CurrentBerth == null)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | SignalStart | " +
                    $"Vessel {vessel.Index} has null CurrentBerth.");
            }

            if (P_StartSignals.Add(vessel))
            {
                Schedule(AttemptStart);
            }
        }

        /// <summary>
        /// Matches vessel signals with available berths and starts processing
        /// </summary>
        protected override void AttemptStart()
        {
            Log("AttemptStart");

            if (R_LoadsRequestedStart.Count == 0 || P_StartSignals.Count == 0)
                return;

            foreach (var vessel in P_StartSignals.ToList())
            {
                var berth = vessel.CurrentBerth;

                if (berth == null)
                {
                    throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                        $"Vessel {vessel.Index} has null CurrentBerth.");
                }

                // Find matching berth in requested set
                if (!R_LoadsRequestedStart.Contains(berth))
                    continue;

                // Ensure berth-vessel association is explicitly updated
                // This supports strong modularity and future deep copy,
                // as vessel state is not maintained within berth activity handlers
                berth.OccupyingVessel = vessel;

                P_StartSignals.Remove(vessel);

                Start(berth);
            }
        }
    }
}