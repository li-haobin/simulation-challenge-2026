using System;
using System.Collections.Generic;
using System.Text;

namespace SimulationChallenge2026
{
    public class Vessel
    {
        public int Index { get; set; }
        public VesselClass VesselClass { get; set; } = null!;

        public Berth? CurrentBerth { get; set; }
        public List<Shipment> CarriedShipments { get; } = new();
        public ServiceRoute? AssignedServiceRoute { get; set; }
        public PartialServiceRoute? CurrentPartialServiceRoute { get; set; }

        public override string ToString() =>
            $"Vessel-{Index} [{VesselClass.Name}]";

        /// <summary>
        /// Retrieves carried shipments whose current booking belongs to the vessel's
        /// assigned service route and matches the specified partial service route.
        /// </summary>
        private HashSet<Shipment> GetShipmentsAtSegment(
            PartialServiceRoute partialServiceRoute,
            Func<Booking, int> segmentIndexSelector)
        {
            var assignedServiceRoute = AssignedServiceRoute
                ?? throw new InvalidOperationException(
                    $"Vessel {Index} has no assigned service route.");

            if (partialServiceRoute == null)
                throw new ArgumentNullException(nameof(partialServiceRoute));

            if (CarriedShipments == null)
                throw new InvalidOperationException(
                    $"Vessel {Index} has no carried shipments.");

            return CarriedShipments
                .Where(shipment =>
                {
                    var booking = shipment.GetCurrentBooking();

                    return booking.ServiceRoute == assignedServiceRoute
                        && segmentIndexSelector(booking) == partialServiceRoute.SequenceIndex;
                })
                .ToHashSet();
        }

        /// <summary>
        /// Retrieves shipments that should be loaded for the vessel's next partial service route.
        /// 
        /// Loading uses the next segment because the vessel is being served before
        /// sailing to the next partial service route.
        /// </summary>
        public HashSet<Shipment> GetLoadingShipmentsAtNextSegment()
        {
            var nextPartialServiceRoute = GetNextPartialServiceRoute();

            return GetShipmentsAtSegment(
                nextPartialServiceRoute,
                booking => booking.DepartureSegmentIndex);
        }

        /// <summary>
        /// Retrieves shipments that should be discharged at the vessel's current partial service route.
        /// 
        /// Discharging uses the current segment because the vessel has arrived at
        /// the current partial service route.
        /// 
        /// If CurrentPartialServiceRoute is null, the vessel has not yet entered
        /// any partial service route, so there are no shipments to discharge.
        /// </summary>
        public HashSet<Shipment> GetDischargingShipmentsAtCurrentSegment()
        {
            var currentPartialServiceRoute = CurrentPartialServiceRoute;

            if (currentPartialServiceRoute == null)
            {
                return new HashSet<Shipment>();
            }

            return GetShipmentsAtSegment(
                currentPartialServiceRoute,
                booking => booking.ArrivalSegmentIndex);
        }

        /// <summary>
        /// Retrieves the next partial service route in the vessel's assigned service route.
        ///
        /// Rules:
        /// - If CurrentPartialServiceRoute is null, return the first partial service route.
        /// - Otherwise, return the next partial service route by sequence index.
        /// - If the current one is the last, wrap around to the first, forming a cyclic route.
        /// </summary>
        /// <returns>
        /// The next partial service route in the cyclic service route.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if:
        /// - the vessel has no assigned service route,
        /// - or the assigned service route has no partial service routes.
        /// </exception>
        public PartialServiceRoute GetNextPartialServiceRoute()
        {
            var assignedServiceRoute = AssignedServiceRoute
                ?? throw new InvalidOperationException(
                    $"Vessel {Index} has no assigned service route.");

            var partialServiceRoutes = assignedServiceRoute.PartialServiceRoutes
                ?? throw new InvalidOperationException(
                    $"Service route {assignedServiceRoute.Id} has no partial service routes.");

            if (partialServiceRoutes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Service route {assignedServiceRoute.Id} has no partial service routes.");
            }

            var firstPartialServiceRoute = partialServiceRoutes
                .OrderBy(p => p.SequenceIndex)
                .First();

            // If current route is not yet assigned, start from the first one
            if (CurrentPartialServiceRoute == null)
            {
                return firstPartialServiceRoute;
            }

            var nextPartialServiceRoute = partialServiceRoutes
                .Where(p => p.SequenceIndex > CurrentPartialServiceRoute.SequenceIndex)
                .OrderBy(p => p.SequenceIndex)
                .FirstOrDefault();

            // Wrap around if current route is already the last one
            return nextPartialServiceRoute ?? firstPartialServiceRoute;
        }        
    }
}
