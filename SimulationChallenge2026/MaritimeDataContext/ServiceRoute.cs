using System.Collections.Generic;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Represents a liner shipping service route.
    /// 
    /// A service route defines a cyclic sequence of legs (PartialServiceRoutes)
    /// and the vessels deployed on this route.
    /// 
    /// Bookings associated with this route serve as the linkage that
    /// represents how shipments dynamically utilize the route over time.
    /// 
    /// The start day of week controls the initial release phase of vessels
    /// in the simulation.
    /// </summary>
    public class ServiceRoute
    {
        /// <summary>
        /// Unique identifier of the service route.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name of the service route.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Start day of week in range [0, 7), representing the initial phase
        /// of the service schedule.
        /// </summary>
        public double StartDayOfWeek { get; set; }

        /// <summary>
        /// Ordered sequence of partial service routes (legs) forming a cyclic route.
        /// </summary>
        public List<PartialServiceRoute> PartialServiceRoutes { get; } = new();

        /// <summary>
        /// Vessels deployed on this service route.
        /// </summary>
        public List<Vessel> DeployedVessels { get; } = new();

        /// <summary>
        /// Bookings associated with this route.
        /// Bookings represent how shipments dynamically utilize the route.
        /// </summary>
        public List<Booking> AssociatedBookings { get; } = new();

        public override string ToString()
        {
            return $"{Id} | StartDay={StartDayOfWeek:F2}";
        }
    }
}