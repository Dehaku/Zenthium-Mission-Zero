using System.Collections.Generic;
using UnityEngine;
using Zenthium.Data;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// Builds module data in memory through the importer's own Populate (Zenthium.Data lets the tests see it, an
    /// approved exception to writing-tests.md). Only the fields a test sets are filled in.
    /// </summary>
    internal static class TestModules
    {
        public static ModuleData Create(string id, ModuleCategory category, ModuleTag[] tags = null,
            ResourceType? resource = null, float cost = 0f, float? cooldownSeconds = null,
            ResourceType[] noRegen = null, Stat? stat = null, params (string Key, float Value)[] values)
        {
            var module = ScriptableObject.CreateInstance<ModuleData>();
            module.SetId(id);
            var dataValues = new List<DataValue>();
            foreach ((string key, float value) in values)
            {
                dataValues.Add(DataValue.FromNumber(key, value));
            }

            module.Populate(category, stat, cooldownSeconds, resource, cost, default, default, default, default,
                default, default, new DataValueList(dataValues), null,
                new List<ModuleTag>(tags ?? new ModuleTag[0]), new List<Stat>(),
                new List<ResourceType>(noRegen ?? new ResourceType[0]));
            return module;
        }

        public static ModuleData Active(string id, ResourceType? resource = null, float cost = 0f)
        {
            return Create(id, ModuleCategory.Active, resource: resource, cost: cost);
        }

        public static ModuleData Passive(string id, params ModuleTag[] tags)
        {
            return Create(id, ModuleCategory.Passive, tags);
        }

        public static ModuleData Gear(string id, params ModuleTag[] tags)
        {
            return Create(id, ModuleCategory.GearPassive, tags);
        }
    }
}
