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
        public event Action<ReviveRuleValues, ReviveRuleValues> ValuesChanged;

        public ReviveRuleValues Values { get; private set; }
        

        public float ReduceReviveProgressSpeed { get; private set; }
        public float ReviveLinkBuffTime { get; private set; }

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
            ApplyValues(pluginConfig.RuleValues.Clone());
        }

        public void ApplyValues(ReviveRuleValues newValues)
        {
            var oldValues = this.Values;
            
            this.Values = newValues;
            this.ReduceReviveProgressSpeed = -(1f / newValues.ReviveTimeSeconds * newValues.ReduceReviveProgressFactor);
            this.ReviveLinkBuffTime = newValues.ReviveTimeSeconds / newValues.ReduceReviveProgressFactor * newValues.ReviveLinkBuffTimeFactor;
            
            this.ValuesChanged?.Invoke(oldValues, newValues);
        }

        public void SendValues()
        {
            new SetRulesMessage(this.Values).Send(NetworkDestination.Clients);
        }

        public float CalculateSkullRadius(Player dead)
        {
            var itemCount = dead.master.master.inventory.GetItemCount(AssetsIndexes.CharonsObolItemIndex);
            var playersCount = dead.skull.insidePlayerIDs.Count;

            return CalculateSkullRadius(itemCount, playersCount);
        }
        public float CalculateSkullRadius(int itemCount, int playersCount)
        {
            var range = this.Values.BaseTotemRange;

            if (this.run.IsDeathCurseEnabled)
            {
                var obolRangeBonus = this.Values.BaseTotemRange * itemCount * this.Values.ItemIncreaseRangeFactor;
                range += obolRangeBonus;
            }
            
            var playersCountBonus = this.Values.BaseTotemRange * this.Values.IncreaseRangeWithPlayersFactor * playersCount;
            range += playersCountBonus;
            
            return range;
        }
        

        public float GetReviveSpeed(Player player, int playersInRange)
        {
            var obolsCount = player.master.master.inventory.GetItemCount(AssetsIndexes.CharonsObolItemIndex);
            return GetReviveSpeed(obolsCount, playersInRange);
        }
        public float GetReviveSpeed(int obolsCount, int playersInRange)
        {
            var speed = (1f / this.Values.ReviveTimeSeconds / playersInRange);
            if (this.run.IsDeathCurseEnabled)
            {
                var obolFactor = this.Values.ReviveTimeSeconds / (this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount));
                speed *= obolFactor;
            }
            
            return speed;
        }
        public float GetReviveIncrease(int obolsCount) => this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount);

        public float GetReviveTime(int obolsCount) => this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount);

        public float GetDamageSpeed(Player player, Player dead)
        {
            var playersInRange = dead.skull.insidePlayerIDs.Count;
            
            var deadPlayerObolsCount = dead.master.master.inventory.GetItemCount(AssetsIndexes.CharonsObolItemIndex);
            return GetDamageSpeed(playersInRange, player.GetBody().maxHealth, deadPlayerObolsCount);
        }
        
        public float GetDamageSpeed(int playersInRange, float playerMaxHealth, int deadPlayerObolsCount)
        {
            float damageSpeed = ((playerMaxHealth * 0.85f) / this.Values.ReviveTimeSeconds / playersInRange);
            if (this.run.IsDeathCurseEnabled)
            {
                damageSpeed *= GetReviveReduceDamageFactor(deadPlayerObolsCount);
            }
            return damageSpeed;
        }

        public float GetReviveReduceDamageFactor(int deadPlayerObolsCount) => 1 / Mathf.Pow(Values.ObolDamageReduceFactor, deadPlayerObolsCount);

        public float GetCurseReduceHpFactor(int reducesCount) =>
            Mathf.Pow(this.Values.ReduceHpFactor, reducesCount) + this.Values.BaseReduceHpFactor;
    }
}