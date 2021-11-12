using System;
using System.Runtime.CompilerServices;
using RoR2;
using TeammateRevive.Logging;
using TeammateRevive.Resources;

namespace TeammateRevive.Integrations
{
    /// <summary>
    /// Adds description for added buffs
    /// </summary>
    public class BetterUiModIntegration
    {
        public BetterUiModIntegration()
        {
            RoR2Application.onLoad += () =>
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI"))
                {
                    RegisterBuffs();
                }
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        void RegisterBuffs()
        {
            try
            {
                BetterUI.Buffs.RegisterBuffInfo(BuffCatalog.GetBuffDef(AssetsIndexes.DeathCurseBuffIndex), "Death Curse", "Reduces your max HP/Shield.");
                BetterUI.Buffs.RegisterBuffInfo(BuffCatalog.GetBuffDef(AssetsIndexes.ReviveLinkBuffIndex), "Revive Link", "Marks that character will receive Death curse when dead character is revived. Removed after some time.");
                Log.Info($"Better UI integration: OK!");
            }
            catch (Exception e)
            {
                Log.Error($"Better UI integration error: {e}");
            }
        }
    }
}