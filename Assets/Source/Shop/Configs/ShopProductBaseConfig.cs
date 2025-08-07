using UnityEngine;
using BrawlLine.Localization;

namespace BrawlLine.Shop
{
    public enum ShopSectionType
    {
        GemSection,
        GoldSection,
    }

    public abstract class ShopProductBaseConfig : ScriptableObject
    {
        [Header("Shop Section")]
        [SerializeField] private ShopSectionType section;

        [Header("Product Details")]
        [SerializeField] private LocString productName;
        [SerializeField] private LocString description;
        [SerializeField] private Sprite productIcon;

        public ShopSectionType Section => section;
        public LocString ProductName => productName;
        public LocString Description => description;
        public Sprite ProductIcon => productIcon;
    }
}