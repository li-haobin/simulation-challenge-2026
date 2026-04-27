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
        /// Retrieves the set of carried shipments whose current booking matches
        /// the vessel's current port under the specified port selector.
        /// </summary>
        /// <param name="portSelector">
        /// A selector that chooses which port of the current booking to compare
        /// against the vessel's current berth port, e.g. departure port or arrival port.
        /// </param>
        /// <returns>
        /// A set of matching shipments at the vessel's current port.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if:
        /// - the vessel has no current berth,
        /// - the berth has no associated port,
        /// - or the vessel has no carried shipments.
        /// </exception>
        private HashSet<Shipment> GetShipmentsAtCurrentPort(Func<Booking, Port> portSelector)
        {
            if (CurrentBerth == null)
                throw new InvalidOperationException(
                    $"Vessel {Index} has no current berth.");

            var port = CurrentBerth.Port
                ?? throw new InvalidOperationException(
                    $"Vessel {Index} berth has no port.");

            if (CarriedShipments == null)
                throw new InvalidOperationException(
                    $"Vessel {Index} has no carried shipments.");

            return CarriedShipments
                .Where(shipment => portSelector(shipment.GetCurrentBooking()) == port)
                .ToHashSet();
        }

        /// <summary>
        /// Retrieves the set of shipments that the vessel is expected to load
        /// at its current berth's port.
        /// </summary>
        public HashSet<Shipment> GetLoadingShipmentsAtCurrentPort()
        {
            return GetShipmentsAtCurrentPort(booking => booking.DeparturePort);
        }

        /// <summary>
        /// Retrieves the set of shipments that the vessel is expected to discharge
        /// at its current berth's port.
        /// </summary>
        public HashSet<Shipment> GetDischargingShipmentsAtCurrentPort()
        {
            return GetShipmentsAtCurrentPort(booking => booking.ArrivalPort);
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
