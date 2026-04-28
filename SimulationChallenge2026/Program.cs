using SimulationChallenge2026;
using System;
using System.Linq;

var context = MaritimeDataInitializer.Create();

var sim = new Model(context, seed: 1);

var totalDays = 360;
var updateIntervalDays = 10;

for (var day = updateIntervalDays; day <= totalDays; day += updateIntervalDays)
{
    sim.Run(TimeSpan.FromDays(updateIntervalDays));

    Console.Clear();

    Console.WriteLine($"Simulation Progress: Day {day:N0} / {totalDays:N0}");
    Console.WriteLine($"Simulation Clock Time: {sim.ClockTime:yyyy-MM-dd HH:mm:ss}");

    PrintShipmentWaitingForLoadingAtOriginPortStatistics(sim);

    Console.WriteLine();
}

Console.WriteLine("Simulation completed.");
Console.WriteLine();


#region Statistics Output

static void PrintShipmentWaitingForLoadingAtOriginPortStatistics(Model sim)
{
    if (sim == null)
        throw new ArgumentNullException(nameof(sim));

    var activity = sim.Shipment_WaitingForLoadingAtOriginPort;

    Console.WriteLine();
    Console.WriteLine("============================================================");
    Console.WriteLine("Shipment Waiting for Loading at Origin Port Statistics");
    Console.WriteLine("============================================================");

    PrintTeusByDemandMatrix(activity);

    PrintTeusByOriginPortTable(activity);

    Console.WriteLine();
}

static void PrintTeusByDemandMatrix(Shipment_WaitingForLoadingAtOriginPort activity)
{
    Console.WriteLine();
    Console.WriteLine("TEUs by Demand Matrix");
    Console.WriteLine("------------------------------------------------------------");

    var origins = activity.HC_TeusByDemand.Keys
        .Select(demand => demand.OriginPort)
        .Distinct()
        .OrderBy(port => port.Name)
        .ToList();

    var destinations = activity.HC_TeusByDemand.Keys
        .Select(demand => demand.DestinationPort)
        .Distinct()
        .OrderBy(port => port.Name)
        .ToList();

    const int firstColumnWidth = 18;
    const int cellWidth = 14;

    Console.Write($"{"Origin \\ Destination",-firstColumnWidth}");

    foreach (var destination in destinations)
    {
        Console.Write($"{destination.Name,cellWidth}");
    }

    Console.WriteLine();

    Console.Write($"{new string('-', firstColumnWidth),-firstColumnWidth}");

    foreach (var _ in destinations)
    {
        Console.Write($"{new string('-', cellWidth),cellWidth}");
    }

    Console.WriteLine();

    foreach (var origin in origins)
    {
        Console.Write($"{origin.Name,-firstColumnWidth}");

        foreach (var destination in destinations)
        {
            var item = activity.HC_TeusByDemand
                .FirstOrDefault(pair =>
                    pair.Key.OriginPort == origin &&
                    pair.Key.DestinationPort == destination);

            if (item.Key == null)
            {
                Console.Write($"{"-",cellWidth}");
            }
            else
            {
                Console.Write($"{item.Value.AverageCount,cellWidth:N0}");
            }
        }

        Console.WriteLine();
    }
}

static void PrintTeusByOriginPortTable(Shipment_WaitingForLoadingAtOriginPort activity)
{
    Console.WriteLine();
    Console.WriteLine("TEUs by Origin Port");
    Console.WriteLine("------------------------------------------------------------");
    Console.WriteLine(
        $"{"Origin Port",-20}" +
        $"{"Average TEUs Waiting",20}");

    Console.WriteLine(
        $"{new string('-', 20),-20}" +
        $"{new string('-', 20),20}");

    foreach (var item in activity.HC_TeusByOriginPort
        .OrderBy(item => item.Key.Name))
    {
        var originPort = item.Key;
        var counter = item.Value;

        Console.WriteLine(
            $"{originPort.Name,-20}" +
            $"{counter.AverageCount,20:N0}");
    }
}

#endregion