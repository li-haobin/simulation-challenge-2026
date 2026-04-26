using O2DESNet;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimulationChallenge2026
{
    internal class Queueing : Sandbox
    {
        public ActivityHandler<int> Server { get; set; }
        public int Count { get; set; } = 0;

        public Queueing(int seed = 0) : base(seed: seed)
        {
            Server = AddChild(new Process<int>("Server", capacity: 1, duration: (l, rs) => TimeSpan.FromMinutes(rs.NextDouble() * 7)));
            Server.OnFinish.Add(Server.Depart);


            Schedule(Arrive, TimeSpan.FromMinutes(1));
        }

        void Arrive()
        {
            Console.WriteLine($"{ClockTime}\tArrive");

            Server.RequestStart(++Count);
            Schedule(Arrive, TimeSpan.FromMinutes(5));
        }
    }


    
    

    

    

    

    

    
}
