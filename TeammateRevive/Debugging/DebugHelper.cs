using System;
using System.Linq;
using R2API.Networking;
using RoR2;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Content;
using TeammateRevive.Players;
using TeammateRevive.DeathTotem;
using UnityEngine;

namespace TeammateRevive.Debugging
{
    public static class DebugHelper
    {
        public static PluginConfig Config { get; set; }

        public static int DamageTargetIndex = 1;
        
        public static void Init(PluginConfig config)
        {
            NetworkingAPI.RegisterMessageType<DebugNetworkMessage>();
            Config = config;
        }

        public static void Update()
        {
            var players = PlayersTracker.instance;
            
            if (Input.GetKeyDown(KeyCode.F2))
                RunOnServer(ToggleRegenBuff, nameof(ToggleRegenBuff));
            
            // set 1st player hp to 1
            if (Input.GetKeyDown(KeyCode.F3)) players.All[0].GetBody().healthComponent.Networkhealth = 1;
            
            // set 1nd player hp to 1
            if (Input.GetKeyDown(KeyCode.F4)) players.All[1].GetBody().healthComponent.Networkhealth = 1;
            
            // damage 2nd player
            if (Input.GetKeyDown(KeyCode.F5))
                RunOnServer(DamageSelectedPlayer, nameof(DamageSelectedPlayer));

            if (Input.GetKeyDown(KeyCode.F6))
                RunOnServer(GiveCurse, nameof(GiveCurse));
            
            if (Input.GetKeyDown(KeyCode.F7))
                RunOnServer(RemoveCurse, nameof(GiveCurse));
            
            if (Input.GetKeyDown(KeyCode.F8))
                RunOnServer(GiveObol, nameof(GiveObol));
            
            if (Input.GetKeyDown(KeyCode.F9))
                RunOnServer(RemoveObol, nameof(RemoveObol));

            if (Input.GetKeyDown(KeyCode.F10))
                RunOnServer(SpawnTotemForFirstPlayer, nameof(SpawnTotemForFirstPlayer));

            if (Config.GodMode && NetworkHelper.IsServer)
            {
                foreach (var player in players.All)
                {
                    var body = player.GetBody();
                    if (body != null)
                    {
                        body.healthComponent.Networkhealth = 1000;
                        body.maxHealth = 1000;
                        body.moveSpeed = body.baseMoveSpeed * 2;
                        body.skillLocator.DeductCooldownFromAllSkillsServer(10);
                        body.damage = body.baseDamage * 5;
                        body.attackSpeed = body.baseAttackSpeed * 10;
                    }
                }
            }
        }

        static void RunOnServer(Action act, string name)
        {
            if (NetworkHelper.IsServer)
            {
                act();
            }
            else
            {
                DebugNetworkMessage.SendToServer(name);
            }
        }

        public static void GiveObol()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.GiveItem(CharonsObol.Index));
        }
        
        public static void RemoveObol()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.RemoveItem(CharonsObol.Index));
        }

        public static void GiveCurse()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.GiveItem(DeathCurse.ItemIndex));
        }
        
        public static void RemoveCurse()
        {
            NetworkUser.readOnlyInstancesList.ToList()
                .ForEach(u => u.master.inventory.RemoveItem(DeathCurse.ItemIndex));
        }

        public static void ToggleRegenBuff()
        {
            var p = PlayersTracker.instance.All.First();
            if (!p.master.master.GetBody().HasBuff(ReviveRegen.Index))
            {
                p.master.master.GetBody().AddBuff(ReviveRegen.Index);
            }
            else
            {
                p.master.master.GetBody().RemoveBuff(ReviveRegen.Index);
            }
        }

        public static void DamageSelectedPlayer()
        {
            PlayersTracker.instance.All[DamageTargetIndex].GetBody().healthComponent.TakeDamage(new DamageInfo
            {
                attacker = PlayersTracker.instance.All[0].GetBody().gameObject,
                damage = 10000,
                damageType = DamageType.Generic
            });
        }

        public static void SpawnTotemForFirstPlayer()
        {
            var players = PlayersTracker.instance;
            
            if (!players.Dead.Contains(players.All[0]))
            {
                players.Dead.Add(players.All[0]);
            }

            players.All[0].master.master.deathFootPosition = players.All[0].GetBody().footPosition;

            DeathTotemTracker.instance.ServerSpawnTotem(players.All[0]);
        }
    }
}