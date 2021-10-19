using R2API.Networking.Interfaces;
using TeammateRevive.Common;
using TeammateRevive.Logging;
using UnityEngine.Networking;

namespace TeammateRevive.Revive.Rules
{
    public class SetRulesMessage : INetMessage
    {
        private readonly ReviveRuleValues ruleValues;

        public SetRulesMessage()
        {
        }

        public SetRulesMessage(ReviveRuleValues ruleValues)
        {
            this.ruleValues = ruleValues;
        }
        
        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.ruleValues.BaseTotemRange);
            writer.Write(this.ruleValues.IncreaseRangeWithPlayersFactor);
            writer.Write(this.ruleValues.ItemIncreaseRangeFactor);
            writer.Write(this.ruleValues.ReviveTimeSeconds);
            writer.Write(this.ruleValues.ObolReviveFactor);
            writer.Write(this.ruleValues.ReduceHpFactor);
            writer.Write(this.ruleValues.BaseReduceHpFactor);
            writer.Write(this.ruleValues.ReduceReviveProgressFactor);
            writer.Write(this.ruleValues.PostReviveBuffTimeFactor);
            writer.Write(this.ruleValues.ObolDamageReduceFactor);
            Log.Info("Sending new rule values");
        }

        public void Deserialize(NetworkReader reader)
        {
            this.ruleValues.BaseTotemRange = reader.ReadSingle();
            this.ruleValues.IncreaseRangeWithPlayersFactor = reader.ReadSingle();
            this.ruleValues.ItemIncreaseRangeFactor = reader.ReadSingle();
            this.ruleValues.ReviveTimeSeconds = reader.ReadSingle();
            this.ruleValues.ObolReviveFactor = reader.ReadSingle();
            this.ruleValues.ReduceHpFactor = reader.ReadSingle();
            this.ruleValues.BaseReduceHpFactor = reader.ReadSingle();
            this.ruleValues.ReduceReviveProgressFactor = reader.ReadSingle();
            this.ruleValues.PostReviveBuffTimeFactor = reader.ReadSingle();
            this.ruleValues.ObolDamageReduceFactor = reader.ReadSingle();
        }

        public void OnReceived()
        {
            Log.Info("Received new rule values");
            if (NetworkHelper.IsClient())
            {
                ReviveRulesCalculator.instance.ApplyValues(this.ruleValues);
                Log.Info("Applied new rule values");
            }
        }
    }
}