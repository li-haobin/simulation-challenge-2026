using System;
using System.Collections.Generic;
using System.Text;

namespace SimulationChallenge2026
{
    public class Booking
    {
        public int SequenceIndex { get; set; }
        public ServiceRoute ServiceRoute { get; set; } = null!;
        public Shipment Shipment { get; set; } = null!;
        public Port DeparturePort { get; set; } = null!;
        public Port ArrivalPort { get; set; } = null!;
    }
}
