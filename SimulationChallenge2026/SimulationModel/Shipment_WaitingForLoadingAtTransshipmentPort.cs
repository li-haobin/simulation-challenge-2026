using System;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents shipments waiting for loading at a transshipment port.
    ///
    /// Design:
    /// - Start has no additional gate control.
    /// - Finish has no external gate control.
    ///
    /// Before finishing:
    /// 1. Adjust the remaining booking suffix, if needed.
    /// 2. Advance CurrentBookingIndex to the next booking.
    /// 3. Finish the shipment.
    /// </summary>
    public class Shipment_WaitingForLoadingAtTransshipmentPort : ActivityHandler<Shipment>
    {
        public Shipment_WaitingForLoadingAtTransshipmentPort(int seed = 0)
            : base(
                  id: nameof(Shipment_WaitingForLoadingAtTransshipmentPort),
                  seed: seed)
        {
        }

        protected override void AttemptFinish()
        {
            Log(nameof(AttemptFinish));

            foreach (var shipment in D_LoadsReadyFinish.ToList())
            {
                if (shipment == null)
                    throw new ArgumentNullException(nameof(shipment));

                Require(
                    shipment.AssociatedBookings.Count > 0,
                    nameof(AttemptFinish),
                    $"Shipment {shipment.Index} has no associated bookings.");

                Require(
                    shipment.CurrentBookingIndex.HasValue,
                    nameof(AttemptFinish),
                    $"Shipment {shipment.Index} has null CurrentBookingIndex.");

                int currentBookingIndex = shipment.CurrentBookingIndex.Value;

                var currentBooking = shipment.AssociatedBookings
                    .FirstOrDefault(booking => booking.SequenceIndex == currentBookingIndex);

                currentBooking = RequireNotNull(
                    currentBooking,
                    nameof(AttemptFinish),
                    $"Shipment {shipment.Index} booking with sequence index {currentBookingIndex}");

                // Step 1: Adjust remaining bookings, if dynamic replanning is enabled later.
                AdjustRemainingBookings(shipment, currentBooking);

                // Step 2: Advance to the next booking in the booking chain.
                var nextBooking = shipment.AssociatedBookings
                    .Where(booking => booking.SequenceIndex > currentBookingIndex)
                    .OrderBy(booking => booking.SequenceIndex)
                    .FirstOrDefault();

                nextBooking = RequireNotNull(
                    nextBooking,
                    nameof(AttemptFinish),
                    $"Shipment {shipment.Index} next booking after sequence index {currentBookingIndex}");

                shipment.CurrentBookingIndex = nextBooking.SequenceIndex;

                // Step 3: Finish this waiting activity.
                Finish(shipment);
            }
        }

        /// <summary>
        /// Placeholder for future dynamic replanning logic.
        ///
        /// Principles:
        /// - Do not modify completed bookings.
        /// - Only adjust the remaining booking suffix.
        /// - Ensure continuity from the current transshipment port to the final destination.
        /// </summary>
        private void AdjustRemainingBookings(Shipment shipment, Booking currentBooking)
        {
            // TODO:
            // Future dynamic routing / replanning logic:
            //
            // Possible extensions:
            // - recompute shortest path from current port to destination;
            // - replace only the suffix after currentBooking;
            // - preserve completed bookings unchanged;
            // - incorporate congestion, delay, or stochastic effects.
        }
    }
}