
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
        public static readonly Color NegativeProgressColor = new(0.84f, 0.2f, 0.28f, 1f);
        public static readonly Color ZeroProgressColor = new(0.39f, 0.12f, 0.73f, 1f);
        public static readonly Color FullProgressColor = new(0.09f, 0.49f, 0.3f, 1f);
        public Color PositiveProgressColor => Color.Lerp(ZeroProgressColor, FullProgressColor, progressBar.progress);
        
        private readonly ProgressBarController progressBar;
        private readonly PlayersTracker players;
        private readonly DeathTotemTracker deathTotemTracker;
        private readonly ReviveRules rules;

        public DeathTotemBehavior trackingTotem;

        private float queuedToHideAt;
        private bool IsQueuedToHide => queuedToHideAt > 0;

        private SpectatorLabel spectatorLabel;

        public ReviveProgressBarTracker(ProgressBarController progressBar, PlayersTracker players, DeathTotemTracker totemTracker, ReviveRules rules)
        {
            this.progressBar = progressBar;
            this.players = players;
            deathTotemTracker = totemTracker;
            this.rules = rules;
            
            DeathTotemBehavior.GlobalOnDestroy += OnDeathTotemDestroy;
            On.RoR2.UI.SpectatorLabel.Awake += SpectatorLabelAwake;
        }

        private void SpectatorLabelAwake(On.RoR2.UI.SpectatorLabel.orig_Awake orig, RoR2.UI.SpectatorLabel self)
        {
            orig(self);
            spectatorLabel = self;
        }

        private void OnDeathTotemDestroy(DeathTotemBehavior totem)
        {
            if (trackingTotem == totem)
            {
                Log.DebugMethod("removing tracking - totem destroyed");
                RemoveTracking();
            }
        }

        public void Update()
        {
            var deathTotem = GetDeathTotemInRange();
            
            // no totem, no tracking
            if (deathTotem == trackingTotem && deathTotem == null)
            {
                if (progressBar.showing)
                {
                    Log.DebugMethod("hide - no totem, no tracking");
                    progressBar.Hide();
                }
                return;
            }

            // new totem
            if (deathTotem != null && trackingTotem != deathTotem)
            {
                Log.DebugMethod("new totem");
                trackingTotem = deathTotem;
                DequeFromHiding();
                progressBar.UpdateText(deathTotem.PlayerName);
            }
            
            // update progress
            if (trackingTotem != null)
            {
                progressBar.SetFraction(trackingTotem.progress);
                progressBar.SetColor(trackingTotem.fractionPerSecond >= 0 ? PositiveProgressColor : NegativeProgressColor);
                progressBar.UpdateText(trackingTotem.PlayerName, trackingTotem.progress);
            }

            // player moved out of totem circle, queuing to hide
            if (deathTotem == null && trackingTotem != null && !IsQueuedToHide)
            {
                Log.DebugMethod("queue to hide");
                QueueToHide();
            }

            // hiding either if progress become 0 or specified delay elapsed
            if (trackingTotem != null && trackingTotem.progress == 0)
            {
                Log.DebugMethod("removing due to progress is 0");
                RemoveTracking();
            }
            
            // hiding based on time
            if (IsQueuedToHide && Time.time > queuedToHideAt)
            {
                Log.DebugMethod($"removing tracking after delay ({Time.time} > {queuedToHideAt})");
                RemoveTracking();
            }
        }

        private void QueueToHide()
        {
            // TODO: make hide time in sync with revive link buff
            queuedToHideAt = Time.time + rules.ReviveLinkBuffTime;
            Log.DebugMethod($"Queued to hide at {queuedToHideAt} (current time: {Time.time})");
        }

        private void DequeFromHiding() => queuedToHideAt = 0;

        private void RemoveTracking()
        {
            DequeFromHiding();
            progressBar.Hide();
            trackingTotem = null;
        }

        public DeathTotemBehavior GetDeathTotemInRange()
        {
            if (!deathTotemTracker.HasAnyTotems)
                return null;

            var trackingBodyId = players.CurrentUserBodyId ?? GetSpectatingBody();
            if (trackingBodyId == null)
                return null;

            return deathTotemTracker.GetDeathTotemInRange(trackingBodyId.Value);
        }

        private NetworkInstanceId? GetSpectatingBody()
        {
            if (IsInSpectatorMode())
            {
                var target = spectatorLabel.cachedTarget;
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

        private bool IsInSpectatorMode() => spectatorLabel != null 
                                            && !spectatorLabel.gameObject.IsDestroyed() 
                                            && !spectatorLabel.labelRoot.IsDestroyed() 
                                            && spectatorLabel.labelRoot.activeSelf;
    }
}