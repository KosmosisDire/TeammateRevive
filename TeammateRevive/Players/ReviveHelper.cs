using System;
using System.Linq;
using System.Text;
using MonoMod.Cil;
using MonoMod.Utils;
using RoR2;
using TeammateRevive.Logging;

namespace TeammateRevive.Players;

public static class ReviveHelper
{
    public static void Init()
    {
        _ = lazyRespawnDelegate.Value;
    }

    private static readonly Lazy<Action<CharacterMaster>> lazyRespawnDelegate = new(CreateRespawnDelegate);

    public static Action<CharacterMaster> RespawnExtraLife => lazyRespawnDelegate.Value;

    /// <summary>
    /// This method creates modified version of CharacterMaster.RespawnExtraLife function that will not add
    /// used Dio's best friend and 
    /// </summary>
    private static Action<CharacterMaster> CreateRespawnDelegate()
    {
        var origMethod = typeof(CharacterMaster).GetMethod("RespawnExtraLife");
            
        try
        {
            var dynamicMethodDefinition = new DynamicMethodDefinition(origMethod);
            var cursor = new ILCursor(new ILContext(dynamicMethodDefinition.Definition));
            if (cursor.TryGotoNext(x => x.MatchCall(typeof(CharacterMaster), "get_deathFootPosition")))
            {
                // removing a first instructions range that give ExtraLifeConsumed item and push item transformation notification
                var idx = cursor.Index;
                cursor.Index = 0;
#if DEBUG
                var sb = new StringBuilder("CharacterMaster.RespawnExtraLife removed instructions:").AppendLine();
                foreach (var bodyInstruction in dynamicMethodDefinition.Definition.Body.Instructions.Take(idx - 1))
                {
                    sb.AppendLine(bodyInstruction.ToString());
                }
                Log.Debug(sb.ToString());
#endif
                cursor.RemoveRange(idx - 1);
                var respawnExtraLifeDelegate =
                    dynamicMethodDefinition.Generate().CreateDelegate<Action<CharacterMaster>>();
                
                Log.Info("Created patched CharacterMaster.RespawnExtraLife version successfully!");
                
                
                return respawnExtraLifeDelegate;
            }
            else
            {
                Log.Warn("Cannot create patched version of RespawnExtraLife!");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Error when trying to create patched RespawnExtraLife: {ex}");
        }

        return FallbackRespawnFunction;
    }

    private static void FallbackRespawnFunction(CharacterMaster master)
    {
        master.RespawnExtraLife();
        master.inventory.RemoveItem(RoR2Content.Items.ExtraLifeConsumed);
    }
}