using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.UI.LogBook;
using TeammateRevive.Configuration;
using TeammateRevive.Content;
using TeammateRevive.Logging;

namespace TeammateRevive;

public static class HideDeathCurseContent
{
    private static readonly string[] ItemsToHide = {
        CharonsObol.NameToken,
        ReviveEverywhereItem.NameToken
    };
        
    public static void Init(PluginConfig config)
    {
        Log.Info($"Init: {config.HideDeathCurseItemsInLogBook}");
        if (config.HideDeathCurseItemsInLogBook)
        {
            On.RoR2.UI.LogBook.LogBookController.BuildPickupEntries += OnBuildPickupEntries;
        }

        if (config.HideDeathCurseArtifact)
        {
            On.RoR2.RuleCatalog.AddRule += OnAddRule;
        }
    }

    private static void OnAddRule(On.RoR2.RuleCatalog.orig_AddRule orig, RuleDef ruledef)
    {
        if (ruledef.globalName == "Artifacts.ARTIFACT_DEATH_CURSE")
        {
            Log.Info($"Hid Death Curse artifact.");
            return;
        }

        orig(ruledef);
    }

    private static Entry[] OnBuildPickupEntries(On.RoR2.UI.LogBook.LogBookController.orig_BuildPickupEntries orig, Dictionary<ExpansionDef, bool> expansionAvailability)
    {
        var result = orig(expansionAvailability);
        var filtered = result.Where(r => !ItemsToHide.Contains(r.nameToken)).ToArray();
        Log.Info($"Hiding Death Curse items ({filtered.Length}/{result.Length})");
        return filtered;
    }
}