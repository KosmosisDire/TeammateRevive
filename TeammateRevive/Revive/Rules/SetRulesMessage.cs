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
            ruleValues = new ReviveRuleValues();
        }

        public SetRulesMessage(ReviveRuleValues ruleValues)
        {
            this.ruleValues = ruleValues;
        }
        
        public void Serialize(NetworkWriter writer)
        {
            writer.Write(ruleValues.BaseTotemRange);
            writer.Write(ruleValues.IncreaseRangeWithPlayersFactor);
            writer.Write(ruleValues.ItemIncreaseRangeFactor);
            writer.Write(ruleValues.ReviveTimeSeconds);
            writer.Write(ruleValues.ObolReviveFactor);
            writer.Write(ruleValues.ReduceHpFactor);
            writer.Write(ruleValues.BaseReduceHpFactor);
            writer.Write(ruleValues.ReduceReviveProgressFactor);
            writer.Write(ruleValues.ReviveLinkBuffTimeFactor);
            writer.Write(ruleValues.ObolDamageReduceFactor);
            writer.Write(ruleValues.ForceDeathCurseRule);
            writer.Write(ruleValues.DebugKeepTotem);
            writer.Write(ruleValues.EnableRevivalToken);
            writer.Write(ruleValues.CutReviveeHp);
            writer.Write(ruleValues.PostReviveRegenDurationSec);
            Log.Info("Sending new rule values");
        }

        public void Deserialize(NetworkReader reader)
        {
            ruleValues.BaseTotemRange = reader.ReadSingle();
            ruleValues.IncreaseRangeWithPlayersFactor = reader.ReadSingle();
            ruleValues.ItemIncreaseRangeFactor = reader.ReadSingle();
            ruleValues.ReviveTimeSeconds = reader.ReadSingle();
            ruleValues.ObolReviveFactor = reader.ReadSingle();
            ruleValues.ReduceHpFactor = reader.ReadSingle();
            ruleValues.BaseReduceHpFactor = reader.ReadSingle();
            ruleValues.ReduceReviveProgressFactor = reader.ReadSingle();
            ruleValues.ReviveLinkBuffTimeFactor = reader.ReadSingle();
            ruleValues.ObolDamageReduceFactor = reader.ReadSingle();
            ruleValues.ForceDeathCurseRule = reader.ReadBoolean();
            ruleValues.DebugKeepTotem = reader.ReadBoolean();
            ruleValues.EnableRevivalToken = reader.ReadBoolean();
            ruleValues.CutReviveeHp = reader.ReadBoolean();
            ruleValues.PostReviveRegenDurationSec = reader.ReadSingle();
        }

        public void OnReceived()
        {
            Log.Info("Received new rule values");
            if (NetworkHelper.IsClient())
            {
                ReviveRules.instance.ApplyValues(ruleValues);
                Log.Info("Applied new rule values");
            }
        }
    }
}