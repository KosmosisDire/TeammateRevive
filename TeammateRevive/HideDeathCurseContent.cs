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
            On.RoR2.UI.RuleChoiceController.UpdateChoiceDisplay += OnRuleChoiceUpdate;
        }
    }

    private static void OnRuleChoiceUpdate(On.RoR2.UI.RuleChoiceController.orig_UpdateChoiceDisplay orig, RoR2.UI.RuleChoiceController self, RuleChoiceDef def)
    {
        orig(self, def);
        // hide Death Curse artifact if it is not enabled
        if (def.globalName.StartsWith("Artifacts.ARTIFACT_DEATH_CURSE.Off"))
        {
            self.gameObject.SetActive(false);
        }
    }

    private static Entry[] OnBuildPickupEntries(On.RoR2.UI.LogBook.LogBookController.orig_BuildPickupEntries orig, Dictionary<ExpansionDef, bool> expansionAvailability)
    {
        var result = orig(expansionAvailability);
        var filtered = result.Where(r => !ItemsToHide.Contains(r.nameToken)).ToArray();
        Log.Info($"Hiding Death Curse items ({filtered.Length}/{result.Length})");
        return filtered;
    }
}