using System;
using RoR2;
using TeammateRevive.Logging;

namespace TeammateRevive
{
    public class RunTracker
    {
        public static RunTracker instance;

        public event Action<RunTracker> RunStarted;

        private bool isStarted;
        public bool IsStarted
        {
            get => this.isStarted;
            set
            {
                if (this.IsStarted == value) return;
                this.isStarted = value;
                if (value)
                {
                    Log.Info("Run started");
                    this.RunStarted?.Invoke(this);
                }
            }
        }

        public RunTracker()
        {
            Run.onRunStartGlobal += GlobalOnRunStarted;
            instance = this;
        }

        void GlobalOnRunStarted(Run obj)
        {
            this.IsStarted = true;
        }
    }
}