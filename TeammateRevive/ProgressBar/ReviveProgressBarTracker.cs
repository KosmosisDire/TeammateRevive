
using RoR2;
using RoR2.UI;
using TeammateRevive.Common;
using TeammateRevive.Logging;
using TeammateRevive.Players;
using TeammateRevive.Revive.Rules;
using TeammateRevive.DeathTotem;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.ProgressBar
{
    /// <summary>
    /// Updates progress bar based on revival status.
    /// </summary>
    public class ReviveProgressBarTracker
    {
        private static readonly Color NegativeProgressColor = new(1, .3f, .3f);
        private Color PositiveProgressColor => Color.Lerp(new(0.8f, .35f, .3f), new(0, 0.772f, 0.329f), progressBar.progress);
        
        private readonly ProgressBarController progressBar;
        private readonly PlayersTracker players;
        private readonly DeathTotemTracker deathTotemTracker;
        private readonly ReviveRules rules;

        public DeathTotemBehavior trackingTotem;

        private float queuedToHideAt;
        private bool IsQueuedToHide => this.queuedToHideAt > 0;

        private SpectatorLabel spectatorLabel;

        public ReviveProgressBarTracker(ProgressBarController progressBar, PlayersTracker players, DeathTotemTracker totemTracker, ReviveRules rules)
        {
            this.progressBar = progressBar;
            this.players = players;
            this.deathTotemTracker = totemTracker;
            this.rules = rules;
            
            DeathTotemBehavior.GlobalOnDestroy += OnDeathTotemDestroy;
            On.RoR2.UI.SpectatorLabel.Awake += SpectatorLabelAwake;
        }

        private void SpectatorLabelAwake(On.RoR2.UI.SpectatorLabel.orig_Awake orig, RoR2.UI.SpectatorLabel self)
        {
            orig(self);
            this.spectatorLabel = self;
        }

        private void OnDeathTotemDestroy(DeathTotemBehavior totem)
        {
            if (this.trackingTotem == totem)
            {
                Log.DebugMethod("removing tracking - totem destroyed");
                RemoveTracking();
            }
        }

        public void Update()
        {
            var deathTotem = GetDeathTotemInRange();
            
            // no totem, no tracking
            if (deathTotem == this.trackingTotem && deathTotem == null)
            {
                if (this.progressBar.showing)
                {
                    Log.DebugMethod("hide - no totem, no tracking");
                    this.progressBar.Hide();
                }
                return;
            }

            // new totem
            if (deathTotem != null && this.trackingTotem != deathTotem)
            {
                Log.DebugMethod("new totem");
                this.trackingTotem = deathTotem;
                DequeFromHiding();
                this.progressBar.UpdateText(deathTotem.PlayerName);
            }
            
            // update progress
            if (this.trackingTotem != null)
            {
                this.progressBar.SetFraction(this.trackingTotem.progress);
                this.progressBar.SetColor(this.trackingTotem.fractionPerSecond >= 0 ? PositiveProgressColor : NegativeProgressColor);
                this.progressBar.UpdateText(this.trackingTotem.PlayerName, this.trackingTotem.progress);
            }

            // player moved out of totem circle, queuing to hide
            if (deathTotem == null && this.trackingTotem != null && !this.IsQueuedToHide)
            {
                Log.DebugMethod("queue to hide");
                QueueToHide();
            }

            // hiding either if progress become 0 or specified delay elapsed
            if (this.trackingTotem != null && this.trackingTotem.progress == 0)
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
            this.trackingTotem = null;
        }

        public DeathTotemBehavior GetDeathTotemInRange()
        {
            if (!this.deathTotemTracker.HasAnyTotems)
                return null;

            var trackingBodyId = this.players.CurrentUserBodyId ?? GetSpectatingBody();
            if (trackingBodyId == null)
                return null;

            return this.deathTotemTracker.GetDeathTotemInRange(trackingBodyId.Value);
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