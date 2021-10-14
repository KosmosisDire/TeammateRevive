using System.Collections;
using TeammateRevival.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevival.RevivalStrategies
{
    public class DamageInRageRevivalStrategy : IRevivalStrategy
    {
        public MainTeammateRevival Plugin { get; }
        public PluginConfig Config => MainTeammateRevival.PluginConfig;

        public DamageInRageRevivalStrategy(MainTeammateRevival plugin)
        {
            this.Plugin = plugin;
        }

        public void Init()
        {
            
        }

        public DeadPlayerSkull ServerSpawnSkull(Player player)
        {
            player.deathMark = Object.Instantiate(this.Plugin.DeathMarker).GetComponent<DeadPlayerSkull>();

            player.deathMark.transform.position = player.groundPosition;
            player.deathMark.transform.rotation = Quaternion.identity;

            if (this.Config.IncreaseRangeWithPlayers)
            {
                player.deathMark.radiusSphere.transform.localScale = Vector3.one * (this.Config.TotemRange * 2 + 0.5f * this.Plugin.TotalPlayers);
                Log.Info(this.Config.TotemRange * 2 + 0.5f * this.Plugin.TotalPlayers);
            }
            else
            {
                player.deathMark.radiusSphere.transform.localScale = Vector3.one * (this.Config.TotemRange);
            }
            
            NetworkServer.Spawn(player.deathMark.gameObject);
            this.Plugin.StartCoroutine(SendValuesDelay(0.2f, player.deathMark));

            Log.Info("Skull spawned on Server and Client");
            return player.deathMark;
        }

        public void OnClientSkullSpawned(DeadPlayerSkull skull)
        {
            // TODO: fill out if needed
        }

        public void Update(Player player, Player dead)
        {
            var skull = dead.deathMark.GetComponent<DeadPlayerSkull>();
            
            //if alive player is within the range of the circle
            if (Vector3.Distance(player.groundPosition, dead.groundPosition) < Config.TotemRange)
            {
                //add health to dead player
                float healAmount = (Time.deltaTime)/Config.ReviveTimeSeconds/Plugin.TotalPlayers * 2;
                dead.rechargedHealth += healAmount;

                //damage alive player - down to 1 HP
                float damageAmount = (player.GetBody().maxHealth * 0.85f * Time.deltaTime)/Config.ReviveTimeSeconds/dead.deathMark.insidePlayerIDs.Count;
                player.GetBody().healthComponent.Networkhealth -= Mathf.Clamp(damageAmount, 0f, player.GetBody().healthComponent.health - 1f);

                //set light color and intensity based on ratio
                float ratio = dead.rechargedHealth;
                if (!skull.insidePlayerIDs.Contains(player.GetBody().netId))
                    skull.insidePlayerIDs.Add(player.GetBody().netId);

                skull.SetValuesSend(healAmount, new Color(1 - ratio, ratio, 0.6f * ratio), 4 + 15 * ratio);
            }
            else
            {
                //set light to red if no one is inside the circle
                if (skull.insidePlayerIDs.Contains(player.GetBody().netId))
                    skull.insidePlayerIDs.Remove(player.GetBody().netId);

                skull.SetValuesSend(skull.amount, new Color(1, 0, 0), skull.intensity);
            }

            //if dead player has recharged enough health, respawn
            if (dead.rechargedHealth >= 1)
            {
                Plugin.RespawnPlayer(dead);
            }
        }
        
        private IEnumerator SendValuesDelay(float delay, DeadPlayerSkull skull) 
        {
            yield return new WaitForSecondsRealtime(delay);
            skull.SetValuesSend(skull.amount, new Color(1, 0, 0), skull.intensity);
        }
    }
}