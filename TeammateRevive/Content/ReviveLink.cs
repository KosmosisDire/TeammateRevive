using R2API;
using RoR2;
using TeammateRevive.Resources;
using UnityEngine;

namespace TeammateRevive.Content
{
    public class ReviveLink : ContentBase
    {
        public static string Name = "BUFF_ReviveLink";
        public static BuffIndex Index;
        
        public override void Init()
        {
            BuffAPI.Add(new CustomBuff(Name, AddedAssets.ReviveLinkBuffIcon, Color.white, true, true));
        }

        protected override void OnBuffsAvailable() => Index = BuffCatalog.FindBuffIndex(Name);
    }
}