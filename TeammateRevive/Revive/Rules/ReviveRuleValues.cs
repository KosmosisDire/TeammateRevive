namespace TeammateRevive.Revive.Rules
{
    public class ReviveRuleValues
    {
        // totem range
        public float BaseTotemRange { get; set; } = 30;
        public float IncreaseRangeWithPlayersFactor { get; set; } = .4f;
        public float ItemIncreaseRangeFactor { get; set; } = .5f;
        
        // revive
        public float ReviveTimeSeconds { get; set; } = 6;
        public float ReduceReviveProgressFactor { get; set; } = .1f;
        public float ReviveLinkBuffTimeFactor { get; set; } = 1f;
        public float ObolReviveFactor { get; set; } = 1.125f;
        public float ObolDamageReduceFactor { get; set; } = 1.1f;
        
        // curse
        public float BaseReduceHpFactor { get; set; } = .25f;
        public float ReduceHpFactor { get; set; } = 1.2f;

        public bool ForceDeathCurseRule { get; set; } = false;

        public bool DebugKeepSkulls { get; set; } = false;

        public bool ShawnCharonShrine { get; set; } = true;

        public ReviveRuleValues Clone()
        {
            return (ReviveRuleValues)MemberwiseClone();
        }
    }
}