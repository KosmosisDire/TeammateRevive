using System.ComponentModel;

namespace TeammateRevival
{
    public enum ReviveStrategy
    {
        [Description("Damage everyone in range")]
        DamageInRange,
        
        [Description("Temporary cut max HP Max to half")]
        ReduceMaxHp
    }
}