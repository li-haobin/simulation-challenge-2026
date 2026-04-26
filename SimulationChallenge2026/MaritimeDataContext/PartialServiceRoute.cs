using SimulationChallenge2026;

public class PartialServiceRoute
{
    /// <summary>
    /// Sequence index within the service route (defines order of legs)
    /// </summary>
    public int SequenceIndex { get; set; }

    /// <summary>
    /// The physical leg associated with this route segment
    /// </summary>
    public Leg AssociatedLeg { get; set; } = null!;

    /// <summary>
    /// The parent service route
    /// </summary>
    public ServiceRoute AssociatedServiceRoute { get; set; } = null!;

    /// <summary>
    /// Vessels currently on this segment
    /// </summary>
    public List<Vessel> CurrentVessels { get; } = new();

    public override string ToString()
    {
        return $"{AssociatedServiceRoute.Id}-{SequenceIndex}";
    }
}