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

        public bool IsDeathCurseEnabled => (ReviveRules.instance?.Values.ForceEnableDeathCurseForSinglePlayer ?? false)
                                           || (this.deathCurseArtifact.ArtifactEnabled || (ReviveRules.instance?.Values.ForceDeathCurseRule ?? false))
                                           && Run.instance?.participatingPlayerCount != 1;

        public RunTracker(DeathCurseArtifact deathCurseArtifact)
        {
            this.deathCurseArtifact = deathCurseArtifact;
            Run.onRunStartGlobal += GlobalOnRunStarted;
            Run.onRunDestroyGlobal += GlobalOnRunDestroy;
            On.RoR2.Run.BeginStage += hook_BeginStage;
            instance = this;
        }

        private void hook_BeginStage(On.RoR2.Run.orig_BeginStage orig, Run self)
        {
            orig(self);
            this.IsStarted = true;
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