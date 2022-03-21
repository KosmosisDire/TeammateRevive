using System.Collections.Generic;
using System.Linq;
using RoR2.ExpansionManagement;
using RoR2.UI.LogBook;
using TeammateRevive.Configuration;
using TeammateRevive.Content;

namespace TeammateRevive;

public static class RemoveDeathCurseItemsFromLogbook
{
    private static readonly string[] ItemsToHide = {
        CharonsObol.Name,
        ReviveEverywhereItem.Name
    };
        
    public static void Init(PluginConfig config)
    {
        if (config.HideDeathCurseItemsInLogBook)
        {
            On.RoR2.UI.LogBook.LogBookController.BuildPickupEntries += OnBuildPickupEntries;
        }
    }

    private static Entry[] OnBuildPickupEntries(On.RoR2.UI.LogBook.LogBookController.orig_BuildPickupEntries orig, Dictionary<ExpansionDef, bool> expansionAvailability)
    {
        var result = orig(expansionAvailability);
        return result.Where(r => !ItemsToHide.Contains(r.nameToken)).ToArray();
    }
}