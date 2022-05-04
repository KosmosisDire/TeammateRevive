using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using TeammateRevive.Configuration;
using TeammateRevive.Logging;

namespace TeammateRevive.Integrations;

public class RiskOfOptionsIntegration
{
    public const string Guid = "com.rune580.riskofoptions";
    
    private readonly PluginConfig config;

    public RiskOfOptionsIntegration(PluginConfig config)
    {
        this.config = config;
        RoR2Application.onLoad += () =>
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Guid))
            {
                try
                {
                    RegisterOptions();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error during RiskOfOptions integration: {ex}");
                }
            }
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void RegisterOptions()
    {
        var registeredCount = RegisterBindCollection(config.RuleValuesBindCollection);
        registeredCount += RegisterBindCollection(config.MiscBindCollection);
#if DEBUG
        registeredCount += RegisterBindCollection(config.DebugBindCollection);
#endif

        Log.Info($"RiskOfOptions integration: OK! (Options: {registeredCount})");
    }

    private int RegisterBindCollection(BindCollection bindCollection)
    {
        var options = bindCollection.Bindings
            .Select(entry =>
            {
                bindCollection.TryGetMetadata(entry, out var meta);
                return CreateOption(meta, entry);
            })
            .Where(o => o != null)
            .ToArray();

        foreach (var option in options)
        {
            RiskOfOptions.ModSettingsManager.AddOption(option);
        }

        return options.Length;
    }

    private static BaseOption CreateOption(EntryMetadata meta, ConfigEntryBase entry)
    {
        var restartRequired = meta?.RestartRequired ?? false;
        var description = GetDescription(entry);
        switch (entry)
        {
            case ConfigEntry<float> fEntry:
            {
                var cfg = new StepSliderConfig
                {
                    restartRequired = restartRequired,
                    description = description
                };
                if (meta is FloatMetadata fMeta)
                {
                    cfg.min = fMeta.MinValue;
                    cfg.max = fMeta.MaxValue;
                    cfg.increment = fMeta.Step;
                }
                return new StepSliderOption(fEntry, cfg);
            }
            
            case ConfigEntry<bool> bEntry:
            {
                var cfg = new CheckBoxConfig
                {
                    description = description,
                    restartRequired = restartRequired
                };
                return new CheckBoxOption(bEntry, cfg);
            }

            case ConfigEntry<string> sEntry:
            {
                var cfg = new InputFieldConfig
                {
                    restartRequired = restartRequired,
                    description = description
                };
                return new StringInputFieldOption(sEntry, cfg);
            }
        }

        Log.Warn($"Cannot create option for config entry {entry.Definition.Section}:{entry.Definition.Key}");
        return null;
    }

    private static string GetDescription(ConfigEntryBase entry)
    {
        return $"{entry.Description.Description}\n\nDefault: {entry.DefaultValue}";
    }
}