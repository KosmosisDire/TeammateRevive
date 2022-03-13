
using RoR2;
using RoR2.UI;
using TeammateRevive.Common;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Revive.Rules;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.ProgressBar
{
    /// <summary>
    /// Updates progress bar based on revival status.
    /// </summary>
    public class ReviveProgressBarTracker
    {
        private static readonly Color NegativeProgressColor = new(1, .35f, .35f);
        private static readonly Color PositiveProgressColor = Color.white;
        
        private readonly ProgressBarController progressBar;
        private readonly PlayersTracker players;
        private readonly SkullTracker skullTracker;
        private readonly ReviveRules rules;

        public DeadPlayerSkull trackingSkull;

        private float queuedToHideAt;
        private bool IsQueuedToHide => this.queuedToHideAt > 0;

        private SpectatorLabel spectatorLabel;

        public ReviveProgressBarTracker(ProgressBarController progressBar, PlayersTracker players, SkullTracker skullTracker, ReviveRules rules)
        {
            this.progressBar = progressBar;
            this.players = players;
            this.skullTracker = skullTracker;
            this.rules = rules;
            
            DeadPlayerSkull.GlobalOnDestroy += OnSkullDestroy;
            On.RoR2.UI.SpectatorLabel.Awake += SpectatorLabelAwake;
        }

        private void SpectatorLabelAwake(On.RoR2.UI.SpectatorLabel.orig_Awake orig, RoR2.UI.SpectatorLabel self)
        {
            orig(self);
            this.spectatorLabel = self;
        }

        private void OnSkullDestroy(DeadPlayerSkull skull)
        {
            if (this.trackingSkull == skull)
            {
                Log.DebugMethod("removing tracking - skull destroyed");
                RemoveTracking();
            }
        }

        public void Update()
        {
            var skull = GetSkullInRange();
            
            // no skull, no tracking
            if (skull == this.trackingSkull && skull == null)
            {
                if (this.progressBar.IsShown)
                {
                    Log.DebugMethod("hide - no skull, no tracking");
                    this.progressBar.Hide();
                }
                return;
            }

            // new skull
            if (skull != null && this.trackingSkull != skull)
            {
                Log.DebugMethod("new skull");
                this.trackingSkull = skull;
                DequeFromHiding();
                this.progressBar.UpdateText(skull.PlayerName);
            }
            
            // update progress
            if (this.trackingSkull != null)
            {
                this.progressBar.SetFraction(this.trackingSkull.progress);
                this.progressBar.SetColor(this.trackingSkull.fractionPerSecond >= 0 ? PositiveProgressColor : NegativeProgressColor);
                this.progressBar.UpdateText(this.trackingSkull.PlayerName, this.trackingSkull.progress);
            }

            // player moved out of skull circle, queuing to hide
            if (skull == null && this.trackingSkull != null && !this.IsQueuedToHide)
            {
                Log.DebugMethod("queue to hide");
                QueueToHide();
            }

            // hiding either if progress become 0 or specified delay elapsed
            if (this.trackingSkull != null && this.trackingSkull.progress == 0)
            {
                Log.DebugMethod("removing due to progress is 0");
                RemoveTracking();
            }
            
            // hiding based on time
            if (this.IsQueuedToHide && Time.time > this.queuedToHideAt)
            {
                Log.DebugMethod($"removing tracking after delay ({Time.time} > {this.queuedToHideAt})");
                RemoveTracking();
            }
        }

        private void QueueToHide()
        {
            // TODO: make hide time in sync with revive link buff
            this.queuedToHideAt = Time.time + this.rules.ReviveLinkBuffTime;
            Log.DebugMethod($"Queued to hide at {this.queuedToHideAt} (current time: {Time.time})");
        }

        private void DequeFromHiding() => this.queuedToHideAt = 0;

        private void RemoveTracking()
        {
            DequeFromHiding();
            this.progressBar.Hide();
            this.trackingSkull = null;
        }

        public DeadPlayerSkull GetSkullInRange()
        {
            if (!this.skullTracker.HasAnySkulls)
                return null;

            var trackingBodyId = this.players.CurrentUserBodyId ?? GetSpectatingBody();
            if (trackingBodyId == null)
                return null;

            return this.skullTracker.GetSkullInRange(trackingBodyId.Value);
        }

        private NetworkInstanceId? GetSpectatingBody()
        {
            if (IsInSpectatorMode())
            {
                var target = this.spectatorLabel.cachedTarget;
                if (target.IsDestroyed())
                {
                    return null;
                }

                var characterBody = target.GetComponent<CharacterBody>();
                if (characterBody != null && !characterBody.isPlayerControlled)
                {
                    return null;
                }

                return characterBody.netId;
            }

            return null;
        }

        private bool IsInSpectatorMode() => this.spectatorLabel != null 
                                            && !this.spectatorLabel.gameObject.IsDestroyed() 
                                            && !this.spectatorLabel.labelRoot.IsDestroyed() 
                                            && this.spectatorLabel.labelRoot.activeSelf;
    }
}