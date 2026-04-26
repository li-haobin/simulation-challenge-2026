using O2DESNet;
using System;

namespace SimulationChallenge2026
{
    internal class Model : Sandbox
    {
        public MaritimeDataContext DataContext { get; }

        // --- Generators ---
        public Shipment_Generator Shipment_Generator { get; }
        public Vessel_Generator Vessel_Generator { get; }
        public Berth_Generator Berth_Generator { get; }

        // --- Activities ---
        public Shipment_WaitingForLoadingAtOriginPort Shipment_WaitingForLoadingAtOriginPort { get; }
        public Shipment_WaitingForLoadingAtTransshipmentPort Shipment_WaitingForLoadingAtTransshipmentPort { get; }
        public Shipment_BeingTransported Shipment_BeingTransported { get; }
        public Vessel_AwaitingInstructions Vessel_AwaitingInstructions { get; }
        public Vessel_Sailing Vessel_Sailing { get; }
        public Vessel_QueuingForBerth Vessel_QueuingForBerth { get; }
        public Vessel_BeingServed Vessel_BeingServed { get; }
        public Berth_Idle Berth_Idle { get; }
        public Berth_Berthing Berth_Berthing { get; }
        public Berth_HandlingCargo Berth_HandlingCargo { get; }

        public Model(MaritimeDataContext dataContext, int seed = 0)
            : base(seed: seed)
        {
            DataContext = dataContext
                ?? throw new ArgumentNullException(nameof(dataContext));

            // --- Generators ---
            Shipment_Generator = AddChild(
                new Shipment_Generator(DataContext, seed: DefaultRS.Next()) { EnableLog = false });

            Vessel_Generator = AddChild(
                new Vessel_Generator(DataContext, seed: DefaultRS.Next()) { EnableLog = false });

            Berth_Generator = AddChild(
                new Berth_Generator(DataContext, seed: DefaultRS.Next()) { EnableLog = false });

            // --- Activities ---
            Shipment_WaitingForLoadingAtOriginPort = AddChild(
                new Shipment_WaitingForLoadingAtOriginPort(DataContext, seed: DefaultRS.Next()) { EnableLog = true });

            Shipment_WaitingForLoadingAtTransshipmentPort = AddChild(
                new Shipment_WaitingForLoadingAtTransshipmentPort(seed: DefaultRS.Next()) { EnableLog = true });

            Shipment_BeingTransported = AddChild(
                new Shipment_BeingTransported(seed: DefaultRS.Next()) { EnableLog = true });

            Vessel_AwaitingInstructions = AddChild(
                new Vessel_AwaitingInstructions(seed: DefaultRS.Next()) { EnableLog = true });

            Vessel_Sailing = AddChild(
                new Vessel_Sailing(seed: DefaultRS.Next()) { EnableLog = true });

            Vessel_QueuingForBerth = AddChild(
                new Vessel_QueuingForBerth(seed: DefaultRS.Next()) { EnableLog = true });

            Vessel_BeingServed = AddChild(
                new Vessel_BeingServed(seed: DefaultRS.Next()) { EnableLog = true });

            Berth_Idle = AddChild(
                new Berth_Idle(seed: DefaultRS.Next()) { EnableLog = true });

            Berth_Berthing = AddChild(
                new Berth_Berthing(seed: DefaultRS.Next()) { EnableLog = true });

            Berth_HandlingCargo = AddChild(
                new Berth_HandlingCargo(seed: DefaultRS.Next()) { EnableLog = true });

            // =========================================================
            // 🔗 Event Wiring (Core System Logic)
            // =========================================================

            // --- Shipment flow ---
            Shipment_Generator.ConnectTo(Shipment_WaitingForLoadingAtOriginPort);
            Shipment_WaitingForLoadingAtOriginPort.ConnectTo(Shipment_BeingTransported);
            Shipment_BeingTransported.ConnectTo(Shipment_WaitingForLoadingAtTransshipmentPort, shipment => true);

            // --- Vessel flow ---
            Vessel_Generator.ConnectTo(Vessel_AwaitingInstructions);

            // --- Berth flow ---
            Berth_Generator.ConnectTo(Berth_Idle);

            //// --- Vessel → Sailing (next step placeholder) ---
            //// Vessel_AwaitingInstructions.OnFinish.Add(...)

            //// --- Berth lifecycle ---
            //Berth_Idle.OnFinish.Add(Berth_Berthing.RequestStart);
            //Berth_Berthing.OnFinish.Add(Berth_HandlingCargo.RequestStart);
            //Berth_HandlingCargo.OnFinish.Add(Berth_Idle.RequestStart);

        }
    }
}