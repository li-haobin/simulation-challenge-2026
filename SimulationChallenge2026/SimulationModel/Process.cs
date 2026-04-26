using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Capacity-constrained activity handler with optional processing duration.
    /// 
    /// This class extends ActivityHandler by introducing:
    /// - Capacity limitation via token-based control
    /// - Optional externally provided duration function
    /// 
    /// Mechanism:
    /// - Each unit of capacity is represented by a token
    /// - P_StartSignals: available tokens (idle capacity)
    /// - Q_FinishSignals: occupied tokens (busy capacity)
    /// 
    /// Start:
    /// - A load can start only if a token is available
    /// - Token moves from P → Q
    /// 
    /// Finish:
    /// - A load can finish only if it holds a token
    /// - Token is removed from Q
    /// 
    /// Depart:
    /// - Token is returned to P (capacity is released)
    /// 
    /// Duration:
    /// - Can be provided via constructor
    /// - If null, defaults to zero duration (instant processing)
    /// </summary>
    public class Process<TLoad> : ActivityHandler<TLoad>
    {
        /// <summary>
        /// Maximum number of loads that can be processed concurrently
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Available capacity tokens (idle servers)
        /// </summary>
        public HashSet<object> P_StartSignals { get; } = new();

        /// <summary>
        /// Occupied capacity tokens (busy servers)
        /// </summary>
        public HashSet<object> Q_FinishSignals { get; } = new();

        public Process(
            string id,
            int capacity,
            Func<TLoad, Random, TimeSpan>? duration = null,
            int seed = 0)
            : base(id, seed)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            Capacity = capacity;

            // Assign processing duration function (optional)
            T_Duration = duration;

            // Initialize capacity tokens
            for (int i = 0; i < Capacity; i++)
            {
                P_StartSignals.Add(new object());
            }
        }

        /// <summary>
        /// Start only when both:
        /// - load is waiting
        /// - capacity token is available
        /// </summary>
        protected override void AttemptStart()
        {
            Log("AttemptStart");

            int n = Math.Min(R_LoadsRequestedStart.Count, P_StartSignals.Count);
            if (n <= 0) return;

            var loads = R_LoadsRequestedStart.Take(n).ToList();
            var signals = P_StartSignals.Take(n).ToList();

            foreach (var signal in signals)
            {
                // Allocate capacity token
                P_StartSignals.Remove(signal);
                Q_FinishSignals.Add(signal);
            }

            foreach (var load in loads)
            {
                Start(load);
            }
        }

        /// <summary>
        /// Finish only when:
        /// - load is ready
        /// - it holds a capacity token
        /// </summary>
        protected override void AttemptFinish()
        {
            Log("AttemptFinish");

            int n = Math.Min(D_LoadsReadyFinish.Count, Q_FinishSignals.Count);
            if (n <= 0) return;

            var loads = D_LoadsReadyFinish.Take(n).ToList();
            var signals = Q_FinishSignals.Take(n).ToList();

            foreach (var signal in signals)
            {
                // Release occupied token
                Q_FinishSignals.Remove(signal);
            }

            foreach (var load in loads)
            {
                Finish(load);
            }
        }

        /// <summary>
        /// On departure:
        /// - load exits the system
        /// - capacity token is returned (server becomes available)
        /// </summary>
        public override void Depart(TLoad load)
        {
            Log("Depart", load);

            if (!F_LoadsFinished.Remove(load))
                return;

            F_HourCounter.ObserveChange(-1);

            // Return capacity token to available pool
            P_StartSignals.Add(new object());

            Schedule(AttemptStart);
        }
    }
}