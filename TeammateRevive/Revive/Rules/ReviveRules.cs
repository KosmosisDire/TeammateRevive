using System;
using R2API.Networking;
using R2API.Networking.Interfaces;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Players;
using TeammateRevive.Resources;
using UnityEngine;

namespace TeammateRevive.Revive.Rules
{
    public class ReviveRules
    {
        public static ReviveRules instance;
        
        private readonly RunTracker run;
        public event Action<ReviveRules> ValuesChanged;

        public ReviveRuleValues Values { get; private set; }
        

        public float ReduceReviveProgressSpeed { get; private set; }
        public float PostReviveBuffTime { get; private set; }

        public ReviveRules(RunTracker run)
        {
            instance = this;
            this.run = run;
            this.run.RunStarted += OnRunStarted;
        }

        private void OnRunStarted(RunTracker sender)
        {
            if (NetworkHelper.IsServer)
            {
                SendValues();
            }
        }

        public void ApplyConfigValues(PluginConfig pluginConfig)
        {
            Values = new ReviveRuleValues();
            if (!pluginConfig.IncreaseRangeWithPlayers) Values.IncreaseRangeWithPlayersFactor = 0;
            Values.BaseTotemRange = pluginConfig.TotemRange;
            Values.ReviveTimeSeconds = pluginConfig.ReviveTimeSeconds;
            ApplyValues(Values);
        }

        public void ApplyValues(ReviveRuleValues newValues)
        {
            this.Values = newValues;
            this.ReduceReviveProgressSpeed = -(1f / newValues.ReviveTimeSeconds * newValues.ReduceReviveProgressFactor);
            this.PostReviveBuffTime = newValues.ReviveTimeSeconds / newValues.ReduceReviveProgressFactor * newValues.PostReviveBuffTimeFactor;
            this.ValuesChanged?.Invoke(this);
        }

        public void SendValues()
        {
            new SetRulesMessage(this.Values).Send(NetworkDestination.Clients);
        }

        public float CalculateSkullRadius(Player dead)
        {
            var itemCount = dead.master.master.inventory.GetItemCount(ItemsAndBuffs.ReviveItemIndex);
            var playersCount = dead.skull.insidePlayerIDs.Count;

            return CalculateSkullRadius(itemCount, playersCount);
        }
        public float CalculateSkullRadius(int itemCount, int playersCount)
        {
            var reviveItemBonus = this.Values.BaseTotemRange * itemCount * this.Values.ItemIncreaseRangeFactor;
            var playersCountBonus = this.Values.BaseTotemRange * this.Values.IncreaseRangeWithPlayersFactor * playersCount;
            
            return this.Values.BaseTotemRange + reviveItemBonus + playersCountBonus;
        }
        

        public float GetReviveSpeed(Player player, int playersInRange)
        {
            var obolsCount = player.master.master.inventory.GetItemCount(ItemsAndBuffs.ReviveItemIndex);
            return GetReviveSpeed(obolsCount, playersInRange);
        }
        public float GetReviveSpeed(int obolsCount, int playersInRange)
        {
            var obolFactor = this.Values.ReviveTimeSeconds / (this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount));
            var speed = (1f / this.Values.ReviveTimeSeconds / playersInRange) * obolFactor;
            
            return speed;
        }
        public float GetReviveIncrease(int obolsCount) => this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount);

        public float GetReviveTime(int obolsCount) => this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount);

        public float GetDamageSpeed(Player player, Player dead)
        {
            var playersInRange = dead.skull.insidePlayerIDs.Count;
            
            var deadPlayerObolsCount = dead.master.master.inventory.GetItemCount(ItemsAndBuffs.ReviveItemIndex);
            return GetDamageSpeed(playersInRange, player.GetBody().maxHealth, deadPlayerObolsCount);
        }
        
        public float GetDamageSpeed(int playersInRange, float playerMaxHealth, int deadPlayerObolsCount)
        {
            float damageSpeed = ((playerMaxHealth * 0.85f) / this.Values.ReviveTimeSeconds / playersInRange) * GetReviveReduceDamageFactor(deadPlayerObolsCount);
            return damageSpeed;
        }

        public float GetReviveReduceDamageFactor(int deadPlayerObolsCount) => 1 / Mathf.Pow(Values.ObolDamageReduceFactor, deadPlayerObolsCount);

        public float GetCurseReduceHpFactor(int reducesCount) =>
            Mathf.Pow(this.Values.ReduceHpFactor, reducesCount) + this.Values.BaseReduceHpFactor;
    }
}