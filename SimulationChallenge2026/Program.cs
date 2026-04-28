using SimulationChallenge2026;

var context = MaritimeDataInitializer.Create();

var sim = new Model(context, seed: 1);
sim.Run(TimeSpan.FromDays(365));

Console.WriteLine();