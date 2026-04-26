using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents vessels being served at berth.
    ///
    /// Start side:
    /// - Controlled by shipment signals accumulated in P_StartSignals.
    /// - For each vessel requesting start, the handler searches the global shipment pool
    ///   for shipments whose current booking:
    ///   1. belongs to the same service route as the vessel, and
    ///   2. departs from the vessel's current port.
    /// - A subset of such shipments may be selected and added to the vessel's carried
    ///   shipments, subject to the vessel TEU capacity constraint.
    /// - The vessel is allowed to start even if no shipment is selected for loading.
    ///
    /// Finish side:
    /// - Controlled by berth signals.
    /// - A ready vessel is matched by vessel.CurrentBerth == berth.
    /// - Before finishing, the vessel is advanced to the next partial service route
    ///   in the cyclic service route and released from the berth.
    /// </summary>
    public class Vessel_BeingServed : ActivityHandler<Vessel>
    {
        /// <summary>
        /// Pending shipment signals representing shipments currently available
        /// for loading consideration.
        ///
        /// This is a global pool shared across all vessels handled by this activity.
        /// Matching to a specific vessel is performed later in AttemptStart().
        /// </summary>
        private readonly HashSet<Shipment> P_StartSignals = new();

        /// <summary>
        /// Finish signal tokens keyed by berth.
        /// Each token may allow one ready vessel currently associated with that berth
        /// to leave this activity.
        /// </summary>
        private readonly HashSet<Berth> Q_FinishSignals = new();

        public Vessel_BeingServed(int seed = 0)
            : base(
                  id: nameof(Vessel_BeingServed),
                  seed: seed)
        {
        }

        /// <summary>
        /// Registers a shipment as available for loading consideration.
        ///
        /// This method does not directly assign the shipment to any vessel.
        /// Instead, it adds the shipment to the shared pending shipment pool
        /// P_StartSignals and schedules AttemptStart().
        ///
        /// Requirements:
        /// - The shipment must not already be carried by any vessel.
        ///
        /// Actual vessel-shipment matching, capacity checks, and loading selection
        /// are all handled later in AttemptStart().
        /// </summary>
        /// <param name="shipment">The shipment that becomes available for loading consideration.</param>
        public void SignalStart(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            Log("SignalStart", shipment);

            if (shipment.CarryingVessel != null)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | SignalStart | " +
                    $"Shipment {shipment.Index} already has carrying vessel {shipment.CarryingVessel.Index}.");
            }

            if (P_StartSignals.Add(shipment))
            {
                Schedule(AttemptStart);
            }
        }

        /// <summary>
        /// External finish signal associated with a berth.
        /// </summary>
        /// <param name="berth">The berth that triggers a potential finish.</param>
        public void SignalFinish(Berth berth)
        {
            if (berth == null)
                throw new ArgumentNullException(nameof(berth));

            Log("SignalFinish", berth);

            if (Q_FinishSignals.Add(berth))
            {
                Schedule(AttemptFinish);
            }
        }

        /// <summary>
        /// Attempts to start vessels by selecting loadable shipments from the global
        /// pending shipment pool.
        ///
        /// For each vessel in the requested-start list:
        /// 1. Identify candidate shipments in P_StartSignals whose current booking:
        ///    - belongs to the same service route as the vessel, and
        ///    - departs from the vessel's current port.
        /// 2. Ensure each candidate shipment is currently unassigned to any vessel.
        /// 3. Compute the vessel's currently occupied TEU, excluding shipments whose
        ///    current booking arrives at the current port, since those shipments are
        ///    considered to be discharged here.
        /// 4. Select a subset of candidate shipments that fits within the vessel's
        ///    remaining TEU capacity.
        /// 5. Add the selected shipments to vessel.CarriedShipments.
        /// 6. Remove the selected shipments from P_StartSignals.
        /// 7. Start the vessel, even if no shipment is selected.
        /// </summary>
        protected override void AttemptStart()
        {
            Log("AttemptStart");

            foreach (var vessel in R_LoadsRequestedStart.ToList())
            {
                var currentBerth = vessel.CurrentBerth
                    ?? throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                        $"Vessel {vessel.Index} has no current berth.");

                var currentPort = currentBerth.Port
                    ?? throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                        $"Vessel {vessel.Index} current berth has no port.");

                var assignedServiceRoute = vessel.AssignedServiceRoute
                    ?? throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                        $"Vessel {vessel.Index} has no assigned service route.");

                var vesselClass = vessel.VesselClass
                    ?? throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                        $"Vessel {vessel.Index} has no vessel class.");

                // Candidate shipments:
                // - currently signalled
                // - currently unassigned to any vessel
                // - whose current booking is on the same service route
                // - and whose departure port matches the vessel's current port
                var candidateShipments = P_StartSignals
                    .Where(shipment =>
                    {
                        if (shipment.CarryingVessel != null)
                        {
                            throw new InvalidOperationException(
                                $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                                $"Shipment {shipment.Index} already assigned to vessel {shipment.CarryingVessel.Index}.");
                        }

                        var booking = shipment.GetCurrentBooking();

                        return booking.ServiceRoute == assignedServiceRoute
                            && booking.DeparturePort == currentPort;
                    })
                    .ToList();

                // Current onboard TEU excluding shipments to be discharged at this port
                int occupiedTeu = vessel.CarriedShipments
                    .Where(shipment => shipment.GetCurrentBooking().ArrivalPort != currentPort)
                    .Sum(shipment => shipment.TeuSize);

                int remainingCapacity = vesselClass.TeuCapacity - occupiedTeu;

                if (remainingCapacity < 0)
                {
                    throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                        $"Vessel {vessel.Index} exceeds TEU capacity before loading.");
                }

                // Select a subset of candidate shipments that fits into remaining capacity
                var selectedShipments = new List<Shipment>();
                int selectedTeu = 0;

                foreach (var shipment in candidateShipments)
                {
                    if (shipment.TeuSize <= 0)
                    {
                        throw new InvalidOperationException(
                            $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                            $"Shipment {shipment.Index} has non-positive TEU size.");
                    }

                    if (selectedTeu + shipment.TeuSize > remainingCapacity)
                        continue;

                    selectedShipments.Add(shipment);
                    selectedTeu += shipment.TeuSize;
                }

                // Add selected shipments to the vessel.
                // Shipment-side carrying state is handled in shipment activities, not here.
                foreach (var shipment in selectedShipments)
                {
                    if (!vessel.CarriedShipments.Contains(shipment))
                    {
                        vessel.CarriedShipments.Add(shipment);
                    }

                    P_StartSignals.Remove(shipment);
                }

                // Start even if no loading occurs
                Start(vessel);
            }
        }

        /// <summary>
        /// Attempts to finish vessels based on berth-specific finish signals.
        ///
        /// For each berth signal:
        /// 1. Find the ready-to-finish vessel whose current berth matches the signalled berth
        /// 2. Consume the finish signal
        /// 3. Remove all carried shipments whose current booking arrives at the current port
        /// 4. Remove the vessel from its current partial service route
        /// 5. Advance the vessel to the next partial service route in the cyclic route
        /// 6. Clear the vessel's current berth
        /// 7. Finish the vessel
        /// </summary>
        protected override void AttemptFinish()
        {
            Log("AttemptFinish");

            foreach (var berth in Q_FinishSignals.ToList())
            {
                var vessel = D_LoadsReadyFinish
                    .FirstOrDefault(v => v.CurrentBerth == berth);

                if (vessel == null)
                    continue;

                // Consume the finish token
                Q_FinishSignals.Remove(berth);

                // Remove all shipments to be discharged at the current port
                var dischargingShipments = vessel.GetDischargingShipmentsAtCurrentPort();
                foreach (var shipment in dischargingShipments)
                {
                    vessel.CarriedShipments.Remove(shipment);
                }

                // Remove the vessel from its current partial service route only if one is assigned
                if (vessel.CurrentPartialServiceRoute != null)
                {
                    vessel.CurrentPartialServiceRoute.CurrentVessels.Remove(vessel);
                }

                // Advance the vessel to the next partial service route.
                // If CurrentPartialServiceRoute is null, this initializes it to the first route.
                var nextPartialServiceRoute = vessel.GetNextPartialServiceRoute();
                vessel.CurrentPartialServiceRoute = nextPartialServiceRoute;
                nextPartialServiceRoute.CurrentVessels.Add(vessel);

                // Release berth association
                vessel.CurrentBerth = null;

                // Fire the finish event
                Finish(vessel);
            }
        }
    }
}