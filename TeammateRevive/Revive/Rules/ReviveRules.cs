using System;
using R2API.Networking;
using R2API.Networking.Interfaces;
using TeammateRevive.Common;
using TeammateRevive.Configuration;
using TeammateRevive.Content;
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
            var itemCount = dead.master.master.inventory.GetItemCount(CharonsObol.Index);
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
        

        public float GetReviveSpeed(Player reviver, int playersInRange)
        {
            var obolsCount = reviver.master.master.inventory.GetItemCount(CharonsObol.Index);
            var reviveEverywhereCount = reviver.master.master.inventory.GetItemCount(ReviveEverywhereItem.Index);
            return GetReviveSpeed(obolsCount, reviveEverywhereCount, playersInRange);
        }
        
        public float GetReviveSpeed(int obolsCount, int reviveEverywhereCount, int playersInRange)
        {
            var speed = (1f / this.Values.ReviveTimeSeconds / playersInRange);
            if (reviveEverywhereCount > 0) speed /= 2;

            if (this.run.IsDeathCurseEnabled)
            {
                var obolFactor = this.Values.ReviveTimeSeconds / (this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount + reviveEverywhereCount));
                speed *= obolFactor;
            }
            
            return speed;
        }
        public float GetReviveIncrease(int obolsCount) => this.Values.ReviveTimeSeconds / Mathf.Pow(this.Values.ObolReviveFactor, obolsCount);

        public float GetReviveTime(int obolsCount, int reviveEverywhereCount) => this.Values.ReviveTimeSeconds /
            this.GetReviveSpeed(obolsCount, reviveEverywhereCount, 1);

        public float GetReviveTimeIncrease(int obolsCount, int reviveEverywhereCount) =>
            GetReviveSpeed(obolsCount, 0, 1) / GetReviveSpeed(obolsCount, reviveEverywhereCount, 1);

        public float GetDamageSpeed(float playerMaxHealth, Player dead, int reviverEverywhereObolCount)
        {
            var playersInRange = dead.skull.insidePlayerIDs.Count;
            
            var deadPlayerObolsCount = dead.master.master.inventory.GetItemCount(CharonsObol.Index);
            return GetDamageSpeed(playersInRange, playerMaxHealth, deadPlayerObolsCount, reviverEverywhereObolCount);
        }
        
        public float GetDamageSpeed(int playersInRange, float playerMaxHealth, int deadPlayerObolsCount, int reviverEverywhereObolCount)
        {
            // aim to leave 15% max HP
            float damageSpeed = ((playerMaxHealth * 0.85f) / this.Values.ReviveTimeSeconds / playersInRange);
            if (this.run.IsDeathCurseEnabled)
            {
                damageSpeed *= GetReviveReduceDamageFactor(deadPlayerObolsCount, reviverEverywhereObolCount);
            }
            return damageSpeed;
        }

        public float GetReviveReduceDamageFactor(int deadPlayerObolsCount, int reviverEverywhereObolCount)
        {
            return 1 / Mathf.Pow(this.Values.ObolDamageReduceFactor, deadPlayerObolsCount);
        }

        public float GetCurseReduceHpFactor(int reducesCount) =>
            Mathf.Pow(this.Values.ReduceHpFactor, reducesCount) + this.Values.BaseReduceHpFactor;
    }
}