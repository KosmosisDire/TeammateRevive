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
        private readonly PluginConfig pluginConfig;
        public event Action<ReviveRuleValues, ReviveRuleValues> ValuesChanged;

        public ReviveRuleValues Values { get; private set; }
        

        public float ReduceReviveProgressSpeed { get; private set; }
        public float ReviveLinkBuffTime { get; private set; }

        public ReviveRules(RunTracker run, PluginConfig pluginConfig)
        {
            instance = this;
            this.run = run;
            this.pluginConfig = pluginConfig;
            run.RunStarted += OnRunStarted;
            pluginConfig.RuleValuesBindCollection.OnChanged +=  ApplyConfigValues;
        }

        private void OnRunStarted(RunTracker sender)
        {
            if (NetworkHelper.IsServer)
            {
                SendValues();
            }
        }

        public void ApplyConfigValues()
        {
            ApplyValues(pluginConfig.RuleValues.Clone());
        }

        public void ApplyValues(ReviveRuleValues newValues)
        {
            var oldValues = Values;
            
            Values = newValues;
            ReduceReviveProgressSpeed = -(1f / newValues.ReviveTimeSeconds * newValues.ReduceReviveProgressFactor);
            ReviveLinkBuffTime = newValues.ReviveTimeSeconds / newValues.ReduceReviveProgressFactor * newValues.ReviveLinkBuffTimeFactor;
            
            ValuesChanged?.Invoke(oldValues, newValues);
        }

        public void SendValues()
        {
            new SetRulesMessage(Values).Send(NetworkDestination.Clients);
        }

        public float CalculateDeathTotemRadius(Player dead)
        {
            var itemCount = dead.master.master.inventory.GetItemCount(CharonsObol.Index);
            var playersCount = dead.deathTotem.insidePlayerIDs.Count;

            return CalculateDeathTotemRadius(itemCount, playersCount);
        }
        public float CalculateDeathTotemRadius(int itemCount, int playersCount)
        {
            var range = Values.BaseTotemRange;

            if (run.IsDeathCurseEnabled)
            {
                var obolRangeBonus = Values.BaseTotemRange * itemCount * Values.ItemIncreaseRangeFactor;
                range += obolRangeBonus;
            }
            
            var playersCountBonus = Values.BaseTotemRange * Values.IncreaseRangeWithPlayersFactor * playersCount;
            range += playersCountBonus;
            
            return range;
        }
        

        public float GetReviveSpeed(Player reviver, int playersInRange)
        {
            var obolsCount = reviver.master.master.inventory.GetItemCount(CharonsObol.Index);
            var reviveEverywhereCount = reviver.master.master.inventory.GetItemCount(DeadMansHandItem.Index);
            return GetReviveSpeed(obolsCount, reviveEverywhereCount, playersInRange);
        }
        
        public float GetReviveSpeed(int obolsCount, int reviveEverywhereCount, int playersInRange)
        {
            var speed = (1f / Values.ReviveTimeSeconds / playersInRange);
            if (reviveEverywhereCount > 0) speed /= 2;

            if (run.IsDeathCurseEnabled)
            {
                var obolFactor = Values.ReviveTimeSeconds / (Values.ReviveTimeSeconds / Mathf.Pow(Values.ObolReviveFactor, obolsCount + reviveEverywhereCount));
                speed *= obolFactor;
            }
            
            return speed;
        }
        public float GetReviveIncrease(int obolsCount) => Values.ReviveTimeSeconds / Mathf.Pow(Values.ObolReviveFactor, obolsCount);

        public float GetReviveTime(int obolsCount, int reviveEverywhereCount) => Values.ReviveTimeSeconds /
            GetReviveSpeed(obolsCount, reviveEverywhereCount, 1);

        public float GetReviveTimeIncrease(int obolsCount, int reviveEverywhereCount) =>
            GetReviveSpeed(obolsCount, 0, 1) / GetReviveSpeed(obolsCount, reviveEverywhereCount, 1);

        public float GetDamageSpeed(float playerMaxHealth, Player dead, int reviverEverywhereObolCount)
        {
            var playersInRange = dead.deathTotem.insidePlayerIDs.Count;
            
            var deadPlayerObolsCount = dead.master.master.inventory.GetItemCount(CharonsObol.Index);
            return GetDamageSpeed(playersInRange, playerMaxHealth, deadPlayerObolsCount, reviverEverywhereObolCount);
        }
        
        public float GetDamageSpeed(int playersInRange, float playerMaxHealth, int deadPlayerObolsCount, int reviverEverywhereObolCount)
        {
            // aim to leave 15% max HP
            float damageSpeed = ((playerMaxHealth * 0.85f) / Values.ReviveTimeSeconds / playersInRange);
            if (run.IsDeathCurseEnabled)
            {
                damageSpeed *= GetReviveReduceDamageFactor(deadPlayerObolsCount, reviverEverywhereObolCount);
            }
            return damageSpeed;
        }

        public float GetReviveReduceDamageFactor(int deadPlayerObolsCount, int reviverEverywhereObolCount)
        {
            return 1 / Mathf.Pow(Values.ObolDamageReduceFactor, deadPlayerObolsCount);
        }

        public float GetCurseReduceHpFactor(int reducesCount) =>
            Mathf.Pow(Values.ReduceHpFactor, reducesCount) + Values.BaseReduceHpFactor;
    }
}