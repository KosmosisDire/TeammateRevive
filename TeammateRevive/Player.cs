using RoR2;
using TeammateRevive.RevivalStrategies.ReduceMaxHp;
using UnityEngine;
using UnityEngine.Networking;

namespace TeammateRevival
{
    public class Player
    {
        public NetworkUser networkUser;
        public PlayerCharacterMasterController master;

        public DeadPlayerSkull deathMark = null;

        public Vector3 groundPosition = Vector3.zero;
        public float rechargedHealth = 0;
        
        public bool isDead = false;
        public NetworkInstanceId bodyID;

        public Player(PlayerCharacterMasterController _player)
        {
            if (_player.networkUser) this.networkUser = _player.networkUser;
            this.master = _player;
            if(this.master.master.GetBody())
                this.bodyID = this.master.master.GetBody().netId;

            this.rechargedHealth = 0;
        }


        public ReduceMaxHpBehavior GetReduceHpBehavior()
        {
            var body = GetBody();
            var component = body.GetComponent<ReduceMaxHpBehavior>();
            if (component)
            {
                return component;
            }
            
            return  body.gameObject.AddComponent<ReduceMaxHpBehavior>();
        }

        public CharacterBody GetBody()
        {
            if (this.master.master.GetBody()) this.bodyID = this.master.master.GetBody().netId;

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
    }
}