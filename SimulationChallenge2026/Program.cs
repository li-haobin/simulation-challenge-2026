using MathNet.Numerics.Random;
using SimulationChallenge2026;

var context = MaritimeDataInitializer.Create();

//Console.WriteLine($"Ports: {context.Ports.Count}");
//Console.WriteLine($"Demands: {context.Demands.Count}");
//Console.WriteLine($"Service Routes: {context.ServiceRoutes.Count}");
//Console.WriteLine($"Legs: {context.Legs.Count}");
//Console.WriteLine($"Partial Service Routes: {context.PartialServiceRoutes.Count}");
//Console.WriteLine($"Vessel Classes: {context.VesselClasses.Count}");
//Console.WriteLine($"Vessels: {context.Vessels.Count}");
//Console.WriteLine();

//foreach (var vesselClass in context.VesselClasses)
//{
//    Console.WriteLine($"{vesselClass.Name}: {vesselClass.Vessels.Count} vessels");
//}

//Console.WriteLine();

//foreach (var route in context.ServiceRoutes)
//{
//    Console.WriteLine(
//        $"{route.Id} - {route.Name}: " +
//        $"{route.DeployedVessels.Count} vessels, " +
//        $"{route.PartialServiceRoutes.Count} partial service routes");
//}



//var sim = new Queueing(seed: 1);

var sim = new Model(context, seed: 1);
sim.Run(TimeSpan.FromDays(14));