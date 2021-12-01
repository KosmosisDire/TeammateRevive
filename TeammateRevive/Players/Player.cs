
using System.Collections.Generic;
using System.Linq;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Skull;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.Players
{
    public class Player
    {
        public readonly NetworkUser networkUser;
        public readonly PlayerCharacterMasterController master;

        public DeadPlayerSkull skull = null;

        public Vector3 groundPosition = Vector3.zero;
        public float reviveProgress = 0;
        
        public bool isDead = false;
        public NetworkInstanceId? BodyId => this.master.master.GetBody()?.netId;

        public readonly Dictionary<Player, float> reviveLinks = new();

        public Player(PlayerCharacterMasterController _player)
        {
            if (_player.networkUser) this.networkUser = _player.networkUser;
            this.master = _player;
            this.reviveProgress = 0;
        }

        public CharacterBody GetBody()
        {
            return this.master.master.GetBody();
        }

        public bool CheckAlive()
        {
            return (GetBody() && !this.master.master.IsDeadAndOutOfLivesServer() && GetBody().healthComponent.alive);
        }

        public bool CheckDead()
        {
            return (!GetBody() || this.master.master.IsDeadAndOutOfLivesServer() || !GetBody().healthComponent.alive);
        }

        public void IncreaseReviveLinkDuration(Player dead, float increase)
        {
            if (!this.reviveLinks.TryGetValue(dead, out var elapsingAt))
            {
                elapsingAt = Time.time + 1;
            }
            this.reviveLinks[dead] = elapsingAt + increase;
        }
        
        public int RemoveReviveLink(Player dead)
        {
            this.reviveLinks.Remove(dead);
            return this.reviveLinks.Count;
        }

        public float GetReviveLinkDuration(Player dead)
        {
            if (this.reviveLinks.TryGetValue(dead, out var time))
            {
                return time;
            }

            return 0;
        }
        
        public void ClearReviveLinks() => this.reviveLinks.Clear();

        public int GetPlayersReviveLinks()
        {
            var time = Time.time;
            
            foreach (var pair in this.reviveLinks.ToArray())
            {
                var player = pair.Key;
                var elapsingAt = pair.Value;
                if (time > elapsingAt)
                {
                    RemoveReviveLink(player);
                    Log.Debug($"Removed revive link for revive of {player.networkUser.userName} from {this.networkUser.userName}. Left: {this.reviveLinks.Count}");
                }
            }

            return this.reviveLinks.Count;
        }

        public bool IsLinkedTo(Player player) => this.reviveLinks.ContainsKey(player);

    }
}