using System;
using System.Runtime.CompilerServices;
using RoR2;
using TeammateRevive.Content;
using TeammateRevive.Localization;
using TeammateRevive.Logging;

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
                BetterUI.Buffs.RegisterBuffInfo(BuffCatalog.GetBuffDef(DeathCurse.BuffIndex), LanguageConsts.ITEM_DEATH_CURSE_NAME, LanguageConsts.BUFF_DEATH_CURSE_DESCRIPTION);
                BetterUI.Buffs.RegisterBuffInfo(BuffCatalog.GetBuffDef(ReviveLink.Index), LanguageConsts.BUFF_REVIVE_LINK_NAME, LanguageConsts.BUFF_REVIVE_LINK_DESCRIPTION);
                BetterUI.Buffs.RegisterBuffInfo(BuffCatalog.GetBuffDef(ReviveRegen.Index), LanguageConsts.BUFF_REVIVE_REGEN_NAME, LanguageConsts.BUFF_REVIVE_REGEN_DESCRIPTION);
                Log.Info($"Better UI integration: OK!");
            }
            catch (Exception e)
            {
                Log.Error($"Better UI integration error: {e}");
            }
        }
    }
}