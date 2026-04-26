using System;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents shipments waiting for loading at a transshipment port.
    ///
    /// Design:
    /// - No start gate control
    /// - No finish gate control
    ///
    /// Before finishing:
    /// 1. Adjust remaining bookings (suffix)
    /// 2. Advance CurrentBookingIndex
    /// 3. Finish shipment
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
            Log("AttemptFinish");

            foreach (var shipment in D_LoadsReadyFinish.ToList())
            {
                if (shipment == null)
                    throw new ArgumentNullException(nameof(shipment));

                if (shipment.AssociatedBookings == null || shipment.AssociatedBookings.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                        $"Shipment {shipment.Index} has no associated bookings.");
                }

                if (!shipment.CurrentBookingIndex.HasValue)
                {
                    throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                        $"Shipment {shipment.Index} has null CurrentBookingIndex.");
                }

                var currentBooking = shipment.AssociatedBookings
                    .FirstOrDefault(b => b.SequenceIndex == shipment.CurrentBookingIndex.Value);

                if (currentBooking == null)
                {
                    throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                        $"Shipment {shipment.Index} has no booking matching index {shipment.CurrentBookingIndex.Value}.");
                }

                // Step 1: Adjust remaining bookings (suffix only)
                AdjustRemainingBookings(shipment, currentBooking);

                // Step 2: Advance to next booking
                var nextBooking = shipment.AssociatedBookings
                    .Where(b => b.SequenceIndex > shipment.CurrentBookingIndex.Value)
                    .OrderBy(b => b.SequenceIndex)
                    .FirstOrDefault();

                if (nextBooking == null)
                {
                    throw new InvalidOperationException(
                        $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AttemptFinish | " +
                        $"Shipment {shipment.Index} has no next booking after index {shipment.CurrentBookingIndex.Value}.");
                }

                shipment.CurrentBookingIndex = nextBooking.SequenceIndex;

                // Step 3: Finish
                Finish(shipment);
            }
        }

        /// <summary>
        /// Placeholder for future replanning logic.
        ///
        /// Principles:
        /// - Do NOT modify completed bookings (prefix)
        /// - Only adjust the remaining bookings (suffix)
        /// - Ensure continuity from current port to final destination
        /// </summary>
        private void AdjustRemainingBookings(Shipment shipment, Booking currentBooking)
        {
            // TODO:
            // Future dynamic routing / replanning logic:
            //
            // Possible extensions:
            // - recompute shortest path from current port to destination
            // - replace only the suffix after currentBooking
            // - preserve prefix unchanged
            // - incorporate congestion, delay, or stochastic effects
        }
    }
}