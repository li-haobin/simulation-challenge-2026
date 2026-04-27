using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents shipments being transported by vessel.
    ///
    /// Design:
    /// - Start is controlled by vessel signals.
    /// - Finish is controlled by vessel signals.
    ///
    /// Start:
    /// - For a signalled vessel, start all shipments in the vessel's current loading set.
    /// - Before starting, assign shipment.CarryingVessel to that vessel.
    ///
    /// Finish:
    /// - For a signalled vessel, finish all ready shipments whose carrying vessel is that vessel
    ///   and which are no longer in vessel.CarriedShipments.
    /// - Before finishing, validate that the shipment's current booking arrival port matches
    ///   the departure port of the vessel's current leg.
    /// - Then clear shipment.CarryingVessel.
    /// </summary>
    public class Shipment_BeingTransported : ActivityHandler<Shipment>
    {
        /// <summary>
        /// Vessel signals for starting transported shipments.
        /// </summary>
        private readonly HashSet<Vessel> P_StartSignals = new();

        /// <summary>
        /// Vessel signals for finishing transported shipments.
        /// </summary>
        private readonly HashSet<Vessel> Q_FinishSignals = new();

        public Shipment_BeingTransported(int seed = 0)
            : base(
                  id: nameof(Shipment_BeingTransported),
                  seed: seed)
        {
        }

        /// <summary>
        /// External start signal indicating that shipments loaded onto a vessel
        /// should begin the transported activity.
        /// </summary>
        public void SignalStart(Vessel vessel)
        {
            if (vessel == null)
                throw new ArgumentNullException(nameof(vessel));

            Log("SignalStart", vessel);

            if (P_StartSignals.Add(vessel))
            {
                Schedule(AttemptStart);
            }
        }

        /// <summary>
        /// External finish signal indicating that shipments associated with a vessel
        /// may finish the transported activity.
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
        /// For each signalled vessel, find its current loading shipments,
        /// assign shipment.CarryingVessel, and start them.
        /// </summary>
        protected override void AttemptStart()
        {
            Log("AttemptStart");

            foreach (var vessel in P_StartSignals.ToList())
            {
                var loadingShipments = vessel.GetLoadingShipmentsAtCurrentPort();

                foreach (var shipment in loadingShipments)
                {
                    if (shipment.CarryingVessel != null)
                    {
                        throw new InvalidOperationException(
                            $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                            $"Shipment {shipment.Index} already has carrying vessel {shipment.CarryingVessel.Index}.");
                    }

                    if (!R_LoadsRequestedStart.Contains(shipment))
                    {
                        throw new InvalidOperationException(
                            $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptStart | " +
                            $"Shipment {shipment.Index} is loaded onto vessel {vessel.Index} " +
                            $"without requesting start.");
                    }
                    if (shipment.Index == 3107) ;
                    shipment.CarryingVessel = vessel;
                    Start(shipment);
                }

                P_StartSignals.Remove(vessel);
            }
        }

        /// <summary>
        /// For each signalled vessel, find ready shipments whose carrying vessel is that vessel
        /// and which are no longer in vessel.CarriedShipments.
        ///
        /// Before finishing each shipment:
        /// - validate booking arrival port against the vessel's current leg departure port
        /// - clear shipment.CarryingVessel
        /// </summary>
        protected override void AttemptFinish()
        {
            Log("AttemptFinish");

            foreach (var vessel in Q_FinishSignals.ToList())
            {
                var currentPartialServiceRoute = vessel.CurrentPartialServiceRoute
                    ?? throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                        $"Vessel {vessel.Index} has null CurrentPartialServiceRoute.");

                var currentLeg = currentPartialServiceRoute.AssociatedLeg
                    ?? throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                        $"Vessel {vessel.Index} current partial service route has null AssociatedLeg.");

                var currentLegDeparturePort = currentLeg.DeparturePort
                    ?? throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                        $"Vessel {vessel.Index} current leg has null DeparturePort.");

                var shipmentsToFinish = D_LoadsReadyFinish
                    .Where(shipment =>
                        shipment.CarryingVessel == vessel &&
                        !vessel.CarriedShipments.Contains(shipment))
                    .ToList();

                foreach (var shipment in shipmentsToFinish)
                {
                    var currentBooking = shipment.GetCurrentBooking();

                    if (currentBooking.ArrivalPort != currentLegDeparturePort)
                    {
                        throw new InvalidOperationException(
                            $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                            $"Shipment {shipment.Index} current booking arrival port " +
                            $"{currentBooking.ArrivalPort.Name} does not match vessel {vessel.Index} " +
                            $"current leg departure port {currentLegDeparturePort.Name}.");
                    }
                    if (shipment.Index == 3107) ;
                    shipment.CarryingVessel = null;
                    Finish(shipment);
                }

                Q_FinishSignals.Remove(vessel);
            }
        }
    }
}