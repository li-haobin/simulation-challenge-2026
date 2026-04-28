using SimulationChallenge2026;
using System;
using System.Collections.Generic;
using System.Linq;

var context = MaritimeDataInitializer.Create();

var sim = new Model(context, seed: 1);

var totalDays = 360;
var updateIntervalDays = 10;

for (var day = updateIntervalDays; day <= totalDays; day += updateIntervalDays)
{
    sim.Run(TimeSpan.FromDays(updateIntervalDays));

    ClearConsoleScreen();

    Console.WriteLine($"Simulation Progress: Day {day:N0} / {totalDays:N0}");
    Console.WriteLine($"Simulation Clock Time: {sim.ClockTime:yyyy-MM-dd HH:mm:ss}");

    PrintShipmentWaitingForLoadingAtOriginPortStatistics(sim);

    PrintShipmentBeingTransportedStatistics(sim);

    Console.WriteLine();
}

Console.WriteLine("Simulation completed.");
Console.WriteLine();


#region Statistics Output

static void ClearConsoleScreen()
{
    if (Console.IsOutputRedirected)
        return;

    // Clear screen + clear scrollback + move cursor to top-left.
    Console.Write("\u001b[2J\u001b[3J\u001b[H");
    Console.Out.Flush();
}

static void PrintShipmentWaitingForLoadingAtOriginPortStatistics(Model sim)
{
    if (sim == null)
        throw new ArgumentNullException(nameof(sim));

    var originActivity = sim.Shipment_WaitingForLoadingAtOriginPort;
    var transshipmentActivity = sim.Shipment_WaitingForLoadingAtTransshipmentPort;

    Console.WriteLine();
    Console.WriteLine("============================================================");
    Console.WriteLine("Shipment Waiting for Loading Statistics");
    Console.WriteLine("============================================================");

    // Keep the original demand matrix.
    PrintTeusByDemandMatrix(originActivity);

    // Extend the port-level table with transshipment waiting TEU.
    PrintTeusByPortTable(originActivity, transshipmentActivity);

    Console.WriteLine();
}

static void PrintShipmentBeingTransportedStatistics(Model sim)
{
    if (sim == null)
        throw new ArgumentNullException(nameof(sim));

    var activity = sim.Shipment_BeingTransported;

    Console.WriteLine();
    Console.WriteLine("============================================================");
    Console.WriteLine("Shipment Being Transported Statistics");
    Console.WriteLine("============================================================");

    PrintAverageTeusInTransitionByDemandMatrix(activity);

    PrintCompletedTeusByDemandMatrix(activity);

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

static void PrintTeusByPortTable(
    Shipment_WaitingForLoadingAtOriginPort originActivity,
    Shipment_WaitingForLoadingAtTransshipmentPort transshipmentActivity)
{
    Console.WriteLine();
    Console.WriteLine("Average Waiting TEUs by Port");
    Console.WriteLine();

    Console.WriteLine(
        $"{"Port",-25}" +
        $"{"Origin Waiting TEU",20}" +
        $"{"Transshipment Waiting TEU",28}" +
        $"{"Total Waiting TEU",20}");

    Console.WriteLine(new string('-', 93));

    var ports = originActivity.HC_TeusByOriginPort.Keys
        .Union(transshipmentActivity.HC_TeusByTransshipmentPort.Keys)
        .OrderBy(port => port.Name)
        .ToList();

    foreach (var port in ports)
    {
        double originAverageTeu = GetAverageCount(
            originActivity.HC_TeusByOriginPort,
            port);

        double transshipmentAverageTeu = GetAverageCount(
            transshipmentActivity.HC_TeusByTransshipmentPort,
            port);

        double totalAverageTeu = originAverageTeu + transshipmentAverageTeu;

        Console.WriteLine(
            $"{port.Name,-25}" +
            $"{originAverageTeu,20:N0}" +
            $"{transshipmentAverageTeu,28:N0}" +
            $"{totalAverageTeu,20:N0}");
    }

    Console.WriteLine(new string('-', 93));

    double totalOriginAverageTeu = originActivity.HC_TeusByOriginPort
        .Values
        .Sum(counter => counter.AverageCount);

    double totalTransshipmentAverageTeu = transshipmentActivity.HC_TeusByTransshipmentPort
        .Values
        .Sum(counter => counter.AverageCount);

    Console.WriteLine(
        $"{"TOTAL",-25}" +
        $"{totalOriginAverageTeu,20:N0}" +
        $"{totalTransshipmentAverageTeu,28:N0}" +
        $"{totalOriginAverageTeu + totalTransshipmentAverageTeu,20:N0}");

    Console.WriteLine();
}

static void PrintAverageTeusInTransitionByDemandMatrix(
    Shipment_BeingTransported activity)
{
    Console.WriteLine();
    Console.WriteLine("Average TEUs in Transition by Demand Matrix");
    Console.WriteLine("------------------------------------------------------------");

    PrintDemandMatrix(
        activity.HC_TeusInTransitionByDemand,
        counter => counter.AverageCount,
        "N0");
}

static void PrintCompletedTeusByDemandMatrix(
    Shipment_BeingTransported activity)
{
    Console.WriteLine();
    Console.WriteLine("Completed TEUs by Demand Matrix");
    Console.WriteLine("------------------------------------------------------------");

    PrintDemandMatrix(
        activity.HC_TeusInTransitionByDemand,
        counter => counter.TotalDecrement,
        "N0");
}

static void PrintDemandMatrix(
    Dictionary<Demand, O2DESNet.HourCounter> counters,
    Func<O2DESNet.HourCounter, double> valueSelector,
    string numberFormat)
{
    var origins = counters.Keys
        .Select(demand => demand.OriginPort)
        .Distinct()
        .OrderBy(port => port.Name)
        .ToList();

    var destinations = counters.Keys
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
            var item = counters
                .FirstOrDefault(pair =>
                    pair.Key.OriginPort == origin &&
                    pair.Key.DestinationPort == destination);

            if (item.Key == null)
            {
                Console.Write($"{"-",cellWidth}");
            }
            else
            {
                double value = valueSelector(item.Value);
                Console.Write($"{value.ToString(numberFormat),cellWidth}");
            }
        }

        Console.WriteLine();
    }
}

static double GetAverageCount(
    Dictionary<Port, O2DESNet.HourCounter> counters,
    Port port)
{
    return counters.TryGetValue(port, out var counter)
        ? counter.AverageCount
        : 0.0;
}

#endregion