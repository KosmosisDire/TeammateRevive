using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace TeammateRevive.Configuration
{
    public static class ConfigFileExtensions
    {
        public static BindCollection BindCollection(this ConfigFile config, string section = null)
        {
            return new BindCollection(config, section);
        }
    }

    public class BindCollection
    {
        private readonly ConfigFile config;
        private readonly string section;

        public List<ConfigEntryBase> Bindings { get; } = new();

        public BindCollection(ConfigFile config, string section)
        {
            this.config = config;
            this.section = section;
        }
        
        public BindCollection Bind<TValue>(string key,
            string description,
            Action<TValue> set,
            TValue defaultValue = default,
            string sectionOverride = null)
        {
            var binding = this.config.Bind(sectionOverride ?? this.section, key, description: description,
                defaultValue: defaultValue);
            set(binding.Value);
            this.Bindings.Add(binding);
            return this;
        }
    }
}