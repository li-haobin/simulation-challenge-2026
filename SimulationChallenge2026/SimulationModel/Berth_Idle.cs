using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Idle activity of berths.
    /// 
    /// A berth starts idling unconditionally once requested.
    /// A berth finishes idling only when a vessel arrives and matches the berth's port.
    /// </summary>
    public class Berth_Idle : ActivityHandler<Berth>
    {
        /// <summary>
        /// Finish signals carrying arriving vessel information
        /// </summary>
        public HashSet<Vessel> Q_FinishSignals { get; } = new();

        public Berth_Idle(int seed = 0)
            : base(id: nameof(Berth_Idle), seed: seed)
        {
        }

        /// <summary>
        /// External signal indicating that a vessel is ready to occupy a berth
        /// </summary>
        public void SignalFinish(Vessel vessel)
        {
            if (vessel == null)
                throw new ArgumentNullException(nameof(vessel));

            Log("SignalFinish", vessel);

            if (Q_FinishSignals.Add(vessel))
            {
                Schedule(AttemptFinish);
            }
        }

        /// <summary>
        /// Finish idling only when an arriving vessel can be matched
        /// with an idle berth at the vessel's arrival port.
        /// </summary>
        protected override void AttemptFinish()
        {
            Log("AttemptFinish");

            if (D_LoadsReadyFinish.Count == 0 || Q_FinishSignals.Count == 0)
                return;

            foreach (var vessel in Q_FinishSignals.ToList())
            {
                var arrivalPort = GetArrivalPortOrThrow(vessel);

                var berth = D_LoadsReadyFinish
                    .FirstOrDefault(b => b.Port == arrivalPort);

                if (berth == null)
                    continue;

                Q_FinishSignals.Remove(vessel);

                berth.OccupyingVessel = vessel;
                vessel.CurrentBerth = berth;

                Finish(berth);
            }
        }

        /// <summary>
        /// Gets the arrival port of the vessel's current partial service route.
        /// Throws if routing state is incomplete.
        /// </summary>
        private Port GetArrivalPortOrThrow(Vessel vessel)
        {
            if (vessel.CurrentPartialServiceRoute == null)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                    $"Vessel {vessel.Index} has null CurrentPartialServiceRoute.");
            }

            if (vessel.CurrentPartialServiceRoute.AssociatedLeg == null)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                    $"Vessel {vessel.Index} has null AssociatedLeg.");
            }

            var arrivalPort = vessel.CurrentPartialServiceRoute.AssociatedLeg.ArrivalPort;

            if (arrivalPort == null)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                    $"Vessel {vessel.Index} has null ArrivalPort.");
            }

            return arrivalPort;
        }
    }
}