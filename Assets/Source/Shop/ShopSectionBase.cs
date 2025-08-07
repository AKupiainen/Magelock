using System.Collections.Generic;
using BrawlLine.DependencyInjection;
using UnityEngine;

namespace BrawlLine.Shop
{
    public abstract class ShopSection : MonoBehaviour
    {
        [Header("Section Base References")]
        [SerializeField] protected GameObject sectionContent;
        [SerializeField] protected Transform productContainer;
        [SerializeField] protected GameObject productItemPrefab;

        [Inject] protected readonly ShopManager ShopManager;
        
        protected List<ShopProductBaseConfig> SectionProducts = new();
        protected readonly List<GameObject> ProductItems = new();

        protected bool isActive;

        public bool IsActive => isActive;

        public virtual void Initialize()
        {
            LoadSectionProducts();
            SetupSectionContent();
        }

        protected abstract void LoadSectionProducts();
        
        protected virtual void SetupSectionContent()
        {
            ClearProductItems();
            
            foreach (var product in SectionProducts)
            {
                CreateProductItem(product);
            }
        }

        protected virtual GameObject CreateProductItem(ShopProductBaseConfig product)
        {
            if (productItemPrefab == null || productContainer == null)
            {
                Debug.LogError($"Product item prefab or container is null in section: {gameObject.name}");
                return null;
            }

            ShopProductItem productItemComponent = DIContainer.Instance.InstantiateFromPrefab<ShopProductItem>(
                    productItemPrefab,
                    dontDestroyOnLoad: false
                );

            productItemComponent.transform.SetParent(productContainer, false);

            if (productItemComponent != null)
            {
                productItemComponent.Initialize(product);
            }
            
            ProductItems.Add(productItemComponent.gameObject);
            return productItemComponent.gameObject;
        }

        protected virtual void ClearProductItems()
        {
            foreach (var item in ProductItems)
            {
                if (item != null)
                {
                    DestroyImmediate(item);
                }
            }

            ProductItems.Clear();
        }

        public virtual void SetActive(bool active)
        {
            isActive = active;
            
            if (sectionContent != null)
            {
                sectionContent.SetActive(active);
            }
        }

        public virtual void RefreshSection()
        {
            SetupSectionContent();
        }

        protected virtual void OnDestroy() { }
    }
}