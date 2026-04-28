using SimulationChallenge2026;

/// <summary>
/// Represents a physical sailing leg between two ports.
///
/// A leg defines the geographical connection from a departure port
/// to an arrival port, together with the sailing distance between them.
///
/// In the routing structure, one leg may be associated with one or
/// more service-route segments. This is useful when the same physical
/// port-to-port connection is reused by different service routes.
/// </summary>
public class Leg
{
    /// <summary>
    /// The port from which vessels depart on this leg.
    /// </summary>
    public Port DeparturePort { get; set; } = null!;

    /// <summary>
    /// The port at which vessels arrive after completing this leg.
    /// </summary>
    public Port ArrivalPort { get; set; } = null!;

    /// <summary>
    /// Sailing distance of this leg, measured in nautical miles.
    /// </summary>
    public double SailingDistance { get; set; }

    /// <summary>
    /// Service-route segments that use this physical leg.
    ///
    /// A leg may be reused by multiple segments, especially when the same
    /// port-to-port connection appears in different service routes.
    /// </summary>
    public List<Segment> Segments { get; } = new();

    public override string ToString() =>
        $"{DeparturePort.Name} -> {ArrivalPort.Name} ({SailingDistance} nm)";
}