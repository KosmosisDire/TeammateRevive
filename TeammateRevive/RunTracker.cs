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
        public event Action<RunTracker> RunEnded;

        private bool isStarted;
        public bool IsStarted
        {
            get => isStarted;
            set
            {
                if (IsStarted == value) return;
                isStarted = value;
                if (value)
                {
                    Log.Info("Run started");
                    RunStarted?.Invoke(this);
                }
                else
                {
                    Log.Info("Run ended");
                    RunEnded?.Invoke(this);
                }
            }
        }

        public bool IsDeathCurseEnabled => (ReviveRules.instance?.Values.ForceEnableDeathCurseForSinglePlayer ?? false)
                                           || (deathCurseArtifact.ArtifactEnabled || (ReviveRules.instance?.Values.ForceDeathCurseRule ?? false))
                                           && Run.instance?.participatingPlayerCount != 1;

        public RunTracker(DeathCurseArtifact deathCurseArtifact)
        {
            this.deathCurseArtifact = deathCurseArtifact;
            Run.onRunStartGlobal += GlobalOnRunStarted;
            Run.onRunDestroyGlobal += GlobalOnRunDestroy;
            On.RoR2.Run.BeginStage += Hook_BeginStage;
            instance = this;
        }

        private void Hook_BeginStage(On.RoR2.Run.orig_BeginStage orig, Run self)
        {
            orig(self);
            IsStarted = true;
        }

        void GlobalOnRunStarted(Run obj)
        {
            IsStarted = true;
        }

        private void GlobalOnRunDestroy(Run obj)
        {
            IsStarted = false;
        }
    }
}