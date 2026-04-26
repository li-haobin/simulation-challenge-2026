using O2DESNet;
using O2DESNet.Standard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimulationChallenge2026
{
    /// <summary>
    /// Abstract base class for activity processing in DES.
    /// 
    /// Defines a standard lifecycle:
    /// RequestStart → Start → ReadyFinish → Finish → Depart
    /// 
    /// By default:
    /// - Start is unconditional
    /// - Finish is unconditional
    /// - Duration is zero unless specified
    /// 
    /// Subclasses may override:
    /// - AttemptStart / AttemptFinish (control logic)
    /// - T_Duration (processing time)
    /// </summary>
    public abstract class ActivityHandler<TLoad> : Sandbox
    {
        /// <summary>
        /// Loads requested to start (R)
        /// </summary>
        public HashSet<TLoad> R_LoadsRequestedStart { get; } = new();

        /// <summary>
        /// Loads currently being processed (S)
        /// </summary>
        public HashSet<TLoad> S_LoadsStarted { get; } = new();

        /// <summary>
        /// Loads ready to finish (D)
        /// </summary>
        public HashSet<TLoad> D_LoadsReadyFinish { get; } = new();

        /// <summary>
        /// Loads finished but not yet departed (F)
        /// </summary>
        public HashSet<TLoad> F_LoadsFinished { get; } = new();

        public bool EnableLog { get; set; } = true;

        /// <summary>
        /// Duration function of the activity.
        /// If null, duration defaults to zero.
        /// </summary>
        public Func<TLoad, Random, TimeSpan>? T_Duration { get; protected set; } = null;

        public HourCounter R_HourCounter { get; }
        public HourCounter S_HourCounter { get; }
        public HourCounter D_HourCounter { get; }
        public HourCounter F_HourCounter { get; }

        /// <summary>
        /// Event triggered when a load starts processing
        /// </summary>
        public List<Action<TLoad>> OnStart { get; } = new();

        /// <summary>
        /// Event triggered when a load finishes processing
        /// </summary>
        public List<Action<TLoad>> OnFinish { get; } = new();

        protected ActivityHandler(string id, int seed = 0)
            : base(id: id, seed: seed)
        {
            R_HourCounter = AddHourCounter();
            S_HourCounter = AddHourCounter();
            D_HourCounter = AddHourCounter();
            F_HourCounter = AddHourCounter();
        }

        /// <summary>
        /// External request to start processing a load
        /// </summary>
        public virtual void RequestStart(TLoad load)
        {
            Log("RequestStart", load);

            if (R_LoadsRequestedStart.Add(load))
            {
                R_HourCounter.ObserveChange(1);
                Schedule(AttemptStart);
            }
        }

        /// <summary>
        /// Default start logic: start all requested loads
        /// </summary>
        protected virtual void AttemptStart()
        {
            Log("AttemptStart");

            foreach (var load in R_LoadsRequestedStart.ToList())
            {
                Start(load);
            }
        }

        /// <summary>
        /// Moves load from R → S and schedules completion
        /// </summary>
        protected virtual void Start(TLoad load)
        {
            Log("Start", load);

            if (!R_LoadsRequestedStart.Remove(load))
                return;

            R_HourCounter.ObserveChange(-1);

            if (S_LoadsStarted.Add(load))
                S_HourCounter.ObserveChange(1);

            var duration = T_Duration?.Invoke(load, DefaultRS) ?? TimeSpan.Zero;

            Schedule(() => ReadyFinish(load), duration);

            foreach (var action in OnStart) action?.Invoke(load);
        }

        /// <summary>
        /// Moves load from S → D and triggers finish attempt
        /// </summary>
        protected virtual void ReadyFinish(TLoad load)
        {
            Log("ReadyFinish", load);

            if (!S_LoadsStarted.Remove(load))
                return;

            S_HourCounter.ObserveChange(-1);

            if (D_LoadsReadyFinish.Add(load))
                D_HourCounter.ObserveChange(1);

            Schedule(AttemptFinish);
        }

        /// <summary>
        /// Default finish logic: finish all ready loads
        /// </summary>
        protected virtual void AttemptFinish()
        {
            Log("AttemptFinish");

            foreach (var load in D_LoadsReadyFinish.ToList())
            {
                Finish(load);
            }
        }

        /// <summary>
        /// Moves load from D → F and triggers OnFinish events
        /// </summary>
        protected virtual void Finish(TLoad load)
        {
            Log("Finish", load);

            if (!D_LoadsReadyFinish.Remove(load))
                return;

            D_HourCounter.ObserveChange(-1);

            if (F_LoadsFinished.Add(load))
                F_HourCounter.ObserveChange(1);

            foreach (var action in OnFinish) action?.Invoke(load);
        }

        /// <summary>
        /// Removes load from system (F → exit)
        /// </summary>
        public virtual void Depart(TLoad load)
        {
            Log("Depart", load);

            if (!F_LoadsFinished.Remove(load))
                return;

            F_HourCounter.ObserveChange(-1);

            Schedule(AttemptStart);
        }

        protected void Log(string eventName, object? load = null)
        {
            if (!EnableLog) return;

            var time = $"[{ClockTime:d\\.hh\\:mm\\:ss}]";

            if (load == null)
                Console.WriteLine($"{time} {Id} | {eventName}");
            else
                Console.WriteLine($"{time} {Id} | {eventName} | {load}");
        }

        /// <summary>
        /// Connect this activity to the next activity in the process flow.
        ///
        /// Flow semantics:
        /// 1. When an entity FINISHES this activity, it REQUESTS START in the next activity.
        /// 2. When an entity STARTS in the next activity, it DEPARTS from this activity.
        ///
        /// This represents a standard activity-to-activity transfer:
        /// the current activity pushes the entity forward when finishing,
        /// and the entity leaves the current activity only after the next
        /// activity has actually started.
        ///
        /// Optional:
        /// - filter:
        ///   Controls whether an entity finishing this activity should be
        ///   forwarded to the next activity. This is useful for routing logic,
        ///   such as port-based, route-based, or type-based transitions.
        /// </summary>
        public void ConnectTo(
            ActivityHandler<TLoad> next,
            Func<TLoad, bool>? filter = null)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            // ============================================================
            // 1. Forward flow: this.Finish -> next.RequestStart
            // ============================================================
            this.OnFinish.Add(load =>
            {
                // Forward the entity to the next activity only if it satisfies
                // the optional filter condition.
                if (filter == null || filter(load))
                {
                    next.RequestStart(load);
                }
            });

            // ============================================================
            // 2. Departure trigger: next.Start -> this.Depart
            // ============================================================
            next.OnStart.Add(load =>
            {
                // Once the next activity has started the entity,
                // the entity is considered to have departed from this activity.
                this.Depart(load);
            });
        }
    }
}