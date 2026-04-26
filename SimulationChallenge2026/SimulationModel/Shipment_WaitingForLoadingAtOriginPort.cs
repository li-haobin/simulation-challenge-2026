using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents shipments waiting for loading at their origin port.
    ///
    /// Design:
    /// - No start gate control
    /// - No finish gate control
    /// - Before finishing, assign the shipment's booking chain
    /// </summary>
    public class Shipment_WaitingForLoadingAtOriginPort : ActivityHandler<Shipment>
    {
        public MaritimeDataContext MaritimeDataContext { get; }

        public Shipment_WaitingForLoadingAtOriginPort(
            MaritimeDataContext maritimeDataContext,
            int seed = 0)
            : base(
                  id: nameof(Shipment_WaitingForLoadingAtOriginPort),
                  seed: seed)
        {
            MaritimeDataContext = maritimeDataContext
                ?? throw new ArgumentNullException(nameof(maritimeDataContext));
        }

        /// <summary>
        /// Finish all ready shipments without external finish gating.
        /// Before finishing, assign the shipment's associated bookings.
        /// </summary>
        protected override void AttemptFinish()
        {
            Log("AttemptFinish");

            foreach (var shipment in D_LoadsReadyFinish.ToList())
            {
                AssignAssociatedBookings(shipment);
                Finish(shipment);
            }
        }

        /// <summary>
        /// Assigns the booking chain for the shipment by solving a shortest-path problem
        /// on the port network induced by all feasible route-based bookings.
        /// </summary>
        private void AssignAssociatedBookings(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            var demand = shipment.Demand
                ?? throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AssignAssociatedBookings | " +
                    $"Shipment {shipment.Index} has null Demand.");

            var originPort = demand.OriginPort
                ?? throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AssignAssociatedBookings | " +
                    $"Shipment {shipment.Index} demand has null OriginPort.");

            var destinationPort = demand.DestinationPort
                ?? throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AssignAssociatedBookings | " +
                    $"Shipment {shipment.Index} demand has null DestinationPort.");

            // Reset first, in case this method is called more than once.
            shipment.AssociatedBookings.Clear();
            shipment.CurrentBookingIndex = null;

            if (originPort == destinationPort)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AssignAssociatedBookings | " +
                    $"Shipment {shipment.Index} has same origin and destination port.");
            }

            var candidateBookings = BuildAllCandidateBookings();

            var path = FindShortestBookingPath(
                originPort,
                destinationPort,
                candidateBookings);

            if (path == null || path.Count == 0)
            {
                throw new InvalidOperationException(
                    $"[{ClockTime:d\\.hh\\:mm\\:ss}] {Id} | AssignAssociatedBookings | " +
                    $"No feasible booking chain found for shipment {shipment.Index} " +
                    $"from {originPort.Name} to {destinationPort.Name}.");
            }

            for (int i = 0; i < path.Count; i++)
            {
                var edge = path[i];

                var booking = new Booking
                {
                    SequenceIndex = i + 1,
                    Shipment = shipment,
                    ServiceRoute = edge.ServiceRoute,
                    DeparturePort = edge.DeparturePort,
                    ArrivalPort = edge.ArrivalPort
                };

                shipment.AssociatedBookings.Add(booking);
                edge.ServiceRoute.AssociatedBookings.Add(booking);

                booking.DeparturePort.OutgoingBookings.Add(booking);
                booking.ArrivalPort.IncomingBookings.Add(booking);
            }

            shipment.CurrentBookingIndex = shipment.AssociatedBookings
                .Min(b => b.SequenceIndex);
        }

        /// <summary>
        /// Builds all feasible "single-booking" edges from all service routes.
        ///
        /// A single booking:
        /// - uses one service route only
        /// - may span multiple consecutive legs on that route
        /// - follows the cyclic order of the service route
        ///
        /// To avoid degenerate full-cycle bookings, we only allow up to n-1 legs
        /// on a route with n partial service routes.
        /// </summary>
        private List<CandidateBookingEdge> BuildAllCandidateBookings()
        {
            var edges = new List<CandidateBookingEdge>();

            foreach (var serviceRoute in MaritimeDataContext.ServiceRoutes)
            {
                var partialRoutes = serviceRoute.PartialServiceRoutes
                    .OrderBy(p => p.SequenceIndex)
                    .ToList();

                if (partialRoutes.Count == 0)
                    continue;

                int n = partialRoutes.Count;

                for (int startIndex = 0; startIndex < n; startIndex++)
                {
                    double cumulativeDistance = 0.0;
                    var departurePort = partialRoutes[startIndex].AssociatedLeg.DeparturePort;

                    for (int step = 1; step <= n - 1; step++)
                    {
                        int legIndex = (startIndex + step - 1) % n;
                        var leg = partialRoutes[legIndex].AssociatedLeg;

                        cumulativeDistance += leg.SailingDistance;

                        var arrivalPort = leg.ArrivalPort;

                        if (departurePort == arrivalPort)
                            continue;

                        edges.Add(new CandidateBookingEdge
                        {
                            ServiceRoute = serviceRoute,
                            DeparturePort = departurePort,
                            ArrivalPort = arrivalPort,
                            TotalDistance = cumulativeDistance
                        });
                    }
                }
            }

            return edges;
        }

        /// <summary>
        /// Finds the shortest booking path from origin to destination using Dijkstra.
        /// Each candidate booking edge is a route-based movement between two ports.
        /// </summary>
        private List<CandidateBookingEdge>? FindShortestBookingPath(
            Port originPort,
            Port destinationPort,
            List<CandidateBookingEdge> allEdges)
        {
            var outgoing = allEdges
                .GroupBy(e => e.DeparturePort)
                .ToDictionary(g => g.Key, g => g.ToList());

            var ports = MaritimeDataContext.Ports.ToList();

            var distance = ports.ToDictionary(p => p, _ => double.PositiveInfinity);
            var previousEdge = new Dictionary<Port, CandidateBookingEdge?>();
            var unvisited = new HashSet<Port>(ports);

            distance[originPort] = 0.0;
            previousEdge[originPort] = null;

            while (unvisited.Count > 0)
            {
                var current = unvisited
                    .OrderBy(p => distance[p])
                    .First();

                if (double.IsPositiveInfinity(distance[current]))
                    break;

                if (current == destinationPort)
                    break;

                unvisited.Remove(current);

                if (!outgoing.TryGetValue(current, out var candidateEdges))
                    continue;

                foreach (var edge in candidateEdges)
                {
                    var next = edge.ArrivalPort;

                    if (!unvisited.Contains(next))
                        continue;

                    double alt = distance[current] + edge.TotalDistance;

                    if (alt < distance[next])
                    {
                        distance[next] = alt;
                        previousEdge[next] = edge;
                    }
                }
            }

            if (!previousEdge.ContainsKey(destinationPort))
                return null;

            var path = new List<CandidateBookingEdge>();
            var cursor = destinationPort;

            while (cursor != originPort)
            {
                if (!previousEdge.TryGetValue(cursor, out var edge) || edge == null)
                    return null;

                path.Add(edge);
                cursor = edge.DeparturePort;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Internal representation of one feasible booking option.
        /// </summary>
        private sealed class CandidateBookingEdge
        {
            public ServiceRoute ServiceRoute { get; set; } = null!;
            public Port DeparturePort { get; set; } = null!;
            public Port ArrivalPort { get; set; } = null!;
            public double TotalDistance { get; set; }
        }
    }
}