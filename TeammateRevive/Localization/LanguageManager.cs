using System.Collections.Generic;
using RoR2;

namespace TeammateRevive.Localization;

public class LanguageManager
{
    public static void RegisterLanguages()
    {
        Language.collectLanguageRootFolders += LanguageOnCollectLanguageRootFolders;
    }

    private static void LanguageOnCollectLanguageRootFolders(List<string> folders)
    {
        folders.Add(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(MainTeammateRevival.instance.Info.Location)!, "Localization", "Languages"));
    }
}