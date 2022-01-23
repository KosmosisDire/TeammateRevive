using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using InLobbyConfig.Fields;
using RoR2;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;

namespace TeammateRevive.Integrations
{
    public class InLobbyConfigIntegration
    {
        private readonly PluginConfig pluginConfig;

        public InLobbyConfigIntegration(PluginConfig pluginConfig)
        {
            this.pluginConfig = pluginConfig;
            RoR2Application.onLoad += () =>
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig"))
                {
                    Register();
                }
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        void Register()
        {            
            try
            {
                var configEntry = new InLobbyConfig.ModConfigEntry
                {
                    DisplayName = MainTeammateRevival.PluginName
                };
                AddSection(configEntry, this.pluginConfig.RuleValuesBindCollection);
            
#if DEBUG
                // Adding debug section only for debug builds - why tempt players? :)
                AddSection(configEntry, this.pluginConfig.DebugBindCollection);
#endif
            
                InLobbyConfig.ModConfigCatalog.Add(configEntry);
                
                Log.Info("InLobbyConfig integration: OK!");
            }
            catch (Exception ex)
            {
                Log.Error("InLobbyConfig integration failed: " + ex);
            }
        }

        static void AddSection(InLobbyConfig.ModConfigEntry configEntry, BindCollection bindCollection)
        {
            configEntry.SectionFields.Add(bindCollection.Section, GetFieldsFromBindings(bindCollection));            
        }

        static IConfigField[] GetFieldsFromBindings(BindCollection bindCollection)
        {
            // for some reason InLobbyConfig mod developer decided that this method should be private, so using reflections to access it
            var createField = (Func<ConfigEntryBase, IConfigField>)typeof(ConfigFieldUtilities)
                .GetMethod("ProcessConfigRow", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .CreateDelegate(typeof(Func<ConfigEntryBase, IConfigField>));

            return bindCollection.Bindings.Select(f =>
            {
                if (f.Description.AcceptableValues is AcceptableValueList<string> list)
                {
                    return new SelectListField<string>(f.Definition.Key, f.Description.Description,
                        () => new[] { (string)f.BoxedValue },
                        (s, i) => f.BoxedValue = s,
                        i => { },
                        () => list.AcceptableValues.ToDictionary(k => k)
                    );
                } 
                return createField(f);
            }).ToArray();
        }
    }
}