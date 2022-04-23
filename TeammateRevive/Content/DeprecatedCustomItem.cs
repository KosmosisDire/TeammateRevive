using R2API;
using RoR2;
using UnityEngine;

namespace TeammateRevive.Content
{
    class DeprecatedCustomItem
    {
        public static CustomItem Create(string name, string nameToken,
            string descriptionToken, string loreToken,
            string pickupToken,
            Sprite pickupIconSprite, GameObject pickupModelPrefab,
            ItemTier tier, ItemTag[] tags,
            bool canRemove,
            bool hidden,
            UnlockableDef unlockableDef = null,
            ItemDisplayRuleDict? itemDisplayRules = null)
        {

            var itemDef = ScriptableObject.CreateInstance<ItemDef>();
            itemDef.canRemove = canRemove;
            itemDef.descriptionToken = descriptionToken;
            itemDef.hidden = hidden;
            itemDef.loreToken = loreToken;
            itemDef.name = name;
            itemDef.nameToken = nameToken;
            itemDef.pickupIconSprite = pickupIconSprite;
            itemDef.pickupModelPrefab = pickupModelPrefab;
            itemDef.pickupToken = pickupToken;
            itemDef.tags = tags;
            itemDef.deprecatedTier = tier;
            itemDef.unlockableDef = unlockableDef;

            return new CustomItem(itemDef, itemDisplayRules);
        }
    }
}
