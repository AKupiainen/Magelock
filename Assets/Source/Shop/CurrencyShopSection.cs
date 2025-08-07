using System.Linq;
using UnityEngine;

namespace BrawlLine.Shop
{
    public class CurrencyShopSection : ShopSection
    {
        [Header("Shop Section Type")]
        [SerializeField] private ShopSectionType shopSectionType; 
        
        protected override void LoadSectionProducts()
        {
            SectionProducts.Clear();
            
            if (ShopManager != null)
            {
                var allProducts = ShopManager.GetAllProducts();
                
                SectionProducts = allProducts.Where(product => 
                    product.Section == shopSectionType
                ).ToList();
            }
            else
            {
                Debug.LogWarning("ShopManager instance not found when loading currency section products.");
            }
        }
    }
}