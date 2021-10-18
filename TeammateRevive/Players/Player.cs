using RoR2;
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
        public float rechargedHealth = 0;
        
        public bool isDead = false;
        public NetworkInstanceId? BodyId => this.master.master.GetBody()?.netId;

        public Player(PlayerCharacterMasterController _player)
        {
            if (_player.networkUser) this.networkUser = _player.networkUser;
            this.master = _player;
            this.rechargedHealth = 0;
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
    }
}