using System;
using System.Collections.Generic;
using System.Text;

namespace SimulationChallenge2026
{
    public class Leg
    {
        public Port DeparturePort { get; set; } = null!;
        public Port ArrivalPort { get; set; } = null!;
        public double SailingDistance { get; set; }

        public List<PartialServiceRoute> PartialServiceRoutes { get; } = new();

        public override string ToString() =>
            $"{DeparturePort.Name} -> {ArrivalPort.Name} ({SailingDistance} nm)";
    }
}
