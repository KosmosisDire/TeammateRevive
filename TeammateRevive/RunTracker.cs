using RoR2;

namespace TeammateRevive
{
    public class RunTracker
    {
        public static RunTracker instance;
        
        public bool IsStarted { get; set; }

        public RunTracker()
        {
            Run.onRunStartGlobal += OnRunStarted;
            instance = this;
        }

        private void OnRunStarted(Run obj)
        {
            this.IsStarted = true;
        }
    }
}