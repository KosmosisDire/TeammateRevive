
using System.Collections.Generic;
using System.Linq;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.DeathTotem;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevive.Players
{
    public class Player
    {
        public readonly NetworkUser networkUser;
        public readonly PlayerCharacterMasterController master;

        public DeathTotemBehavior deathTotem = null;

        public Vector3 groundPosition = Vector3.zero;
        public float reviveProgress = 0;
        
        public bool isDead = false;
        public NetworkInstanceId? BodyId => master.master.GetBody()?.netId;

        public readonly Dictionary<Player, float> reviveLinks = new();

        public Player(PlayerCharacterMasterController _player)
        {
            if (_player.networkUser) networkUser = _player.networkUser;
            master = _player;
            reviveProgress = 0;
        }

        public CharacterBody GetBody()
        {
            return master.master.GetBody();
        }

        public bool CheckAlive()
        {
            return (GetBody() && !master.master.IsDeadAndOutOfLivesServer() && GetBody().healthComponent.alive);
        }

        public bool CheckDead()
        {
            return (!GetBody() || master.master.IsDeadAndOutOfLivesServer() || !GetBody().healthComponent.alive);
        }

        public void UpdateGroundPosition() => groundPosition = GetGroundPosition(this);

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

        public void IncreaseReviveLinkDuration(Player dead, float increase)
        {
            if (!reviveLinks.TryGetValue(dead, out var elapsingAt))
            {
                elapsingAt = Time.time + 1;
            }
            reviveLinks[dead] = elapsingAt + increase;
        }
        
        public int RemoveReviveLink(Player dead)
        {
            reviveLinks.Remove(dead);
            return reviveLinks.Count;
        }

        public float GetReviveLinkDuration(Player dead)
        {
            if (reviveLinks.TryGetValue(dead, out var time))
            {
                return time;
            }

            return 0;
        }
        
        public void ClearReviveLinks() => reviveLinks.Clear();

        public int GetPlayersReviveLinks()
        {
            var time = Time.time;
            
            foreach (var pair in reviveLinks.ToArray())
            {
                var player = pair.Key;
                var elapsingAt = pair.Value;
                if (time > elapsingAt)
                {
                    RemoveReviveLink(player);
                    Log.Debug($"Removed revive link for revive of {player.networkUser.userName} from {networkUser.userName}. Left: {reviveLinks.Count}");
                }
            }

            return reviveLinks.Count;
        }

        public bool IsLinkedTo(Player player) => reviveLinks.ContainsKey(player);

    }
}