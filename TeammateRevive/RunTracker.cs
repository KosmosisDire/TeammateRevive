using System;
using RoR2;
using TeammateRevive.Artifact;
using TeammateRevive.Logging;
using TeammateRevive.Revive.Rules;

namespace TeammateRevive
{
    public class RunTracker
    {
        private readonly DeathCurseArtifact deathCurseArtifact;
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

        public bool IsDeathCurseEnabled => (this.deathCurseArtifact.ArtifactEnabled ||
                                           (ReviveRules.instance?.Values.ForceDeathCurseRule ?? false))
                                           && Run.instance?.participatingPlayerCount != 1;

        public RunTracker(DeathCurseArtifact deathCurseArtifact)
        {
            this.deathCurseArtifact = deathCurseArtifact;
            Run.onRunStartGlobal += GlobalOnRunStarted;
            Run.onRunDestroyGlobal += GlobalOnRunDestroy;
            instance = this;
        }

        void GlobalOnRunStarted(Run obj)
        {
            this.IsStarted = true;
        }

        private void GlobalOnRunDestroy(Run obj)
        {
            this.IsStarted = false;
        }
    }
}