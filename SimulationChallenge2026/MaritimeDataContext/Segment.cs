using SimulationChallenge2026;

/// <summary>
/// Represents an ordered segment within a liner service route.
///
/// A segment is the service-route-level use of a physical leg. While a
/// <see cref="Leg"/> describes the physical movement from one port to another,
/// a segment describes where that movement appears in a specific service route
/// sequence.
///
/// This distinction is useful because the same physical leg may be reused by
/// different service routes, while each segment belongs to exactly one service
/// route and has its own sequence index within that route.
/// </summary>
public class Segment
{
    /// <summary>
    /// Sequence index of this segment within its associated service route.
    ///
    /// The sequence index defines the visiting order of the route. It is also
    /// used by bookings to indicate the departure segment and arrival segment
    /// for a shipment.
    /// </summary>
    public int SequenceIndex { get; set; }

    /// <summary>
    /// The physical sailing leg used by this service-route segment.
    ///
    /// The associated leg defines the departure port, arrival port, and sailing
    /// distance for the movement represented by this segment.
    /// </summary>
    public Leg AssociatedLeg { get; set; } = null!;

    /// <summary>
    /// The service route to which this segment belongs.
    /// </summary>
    public ServiceRoute AssociatedServiceRoute { get; set; } = null!;

    /// <summary>
    /// Vessels currently assigned to or travelling on this segment.
    ///
    /// This collection is updated as vessels advance through their cyclic
    /// service routes.
    /// </summary>
    public List<Vessel> CurrentVessels { get; } = new();

    public override string ToString()
    {
        return $"{AssociatedServiceRoute.Id}-{SequenceIndex}";
    }
}