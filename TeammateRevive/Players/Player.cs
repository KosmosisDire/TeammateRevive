
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

        private readonly Dictionary<Player, float> reviveInvolvements = new();

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

        public void UpdateGroundPosition() => this.groundPosition = GetGroundPosition(this);

        public static Vector3 GetGroundPosition(Player player)
        {
            var body = player.GetBody();
            
            if (body == null)
                return Vector3.zero;
            
            if (Physics.Raycast(body.transform.position, Vector3.down, out var hit, 1000, LayerMask.GetMask("World")))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) > 0.5f && Vector3.Distance(body.transform.position, player.groundPosition) > Vector3.Distance(body.transform.position, hit.point))
                {
                    return hit.point;
                }
            }
            
            return player.groundPosition;
        }

        public void SetReviveInvolvement(Player dead, float elapsingAt)
        {
            this.reviveInvolvements[dead] = elapsingAt;
        }
        
        public int RemoveReviveInvolvement(Player dead)
        {
            this.reviveInvolvements.Remove(dead);
            return this.reviveInvolvements.Count;
        }
        
        public void ClearReviveInvolvement() => this.reviveInvolvements.Clear();

        public int GetRevivingPlayersInvolvement()
        {
            var time = Time.time;
            
            foreach (var pair in this.reviveInvolvements.ToArray())
            {
                var player = pair.Key;
                var elapsingAt = pair.Value;
                if (time > elapsingAt)
                {
                    this.reviveInvolvements.Remove(player);
                    Log.Debug($"Removed involvement for revive of {player.networkUser.userName} from {this.networkUser.userName}. Left: {this.reviveInvolvements.Count}");
                }
            }

            return this.reviveInvolvements.Count;
        }

        public bool IsInvolvedInReviveOf(Player player) => this.reviveInvolvements.ContainsKey(player);

    }
}