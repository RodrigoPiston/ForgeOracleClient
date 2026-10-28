using System;
using ForgeOracle.Core.Random;

namespace ForgeOracle.Core.Items
{
    /// <summary>
    /// The identity the forge algorithm hashes: the name of the recipe's item prefab
    /// (<c>Recipe.m_item.gameObject.name</c>), never a localized display name.
    /// </summary>
    public sealed class ItemIdentity : IEquatable<ItemIdentity>
    {
        public ItemIdentity(string prefabName, string nameToken, int maxQuality)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                throw new ArgumentException("A prefab name is required.", nameof(prefabName));
            }

            PrefabName = prefabName;
            NameToken = nameToken ?? string.Empty;
            MaxQuality = maxQuality;
            PrefabHash = ValheimHash.GetStableHashCode(prefabName);
        }

        /// <summary>Internal prefab name, for example <c>SwordBlackmetal</c>.</summary>
        public string PrefabName { get; }

        /// <summary>The untranslated name token, for example <c>$item_sword_blackmetal</c>.</summary>
        public string NameToken { get; }

        /// <summary>Maximum quality declared by the item. The forge can go past it.</summary>
        public int MaxQuality { get; }

        public int PrefabHash { get; }

        public bool Equals(ItemIdentity? other)
        {
            return other != null && string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as ItemIdentity);

        public override int GetHashCode() => PrefabHash;

        public override string ToString() => PrefabName;
    }
}
