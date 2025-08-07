using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BrawlLine.Shop;

namespace BrawlLine.UI
{
    public class ShopPopup : Popup
    {
        [Header("Shop UI References")]
        [SerializeField] private Transform sectionContentContainer;
        
        [Header("Shop Configuration")]
        [SerializeField] private List<ShopSection> sections = new List<ShopSection>();
        
        protected override void OnPopupOpened()
        {
            base.OnPopupOpened();
            
            foreach (var section in sections)
            {
                section.Initialize();
            }
            
            RebuildLayoutImmediate();
        }
        
        public void OnPurchaseCompleted()
        {
            foreach (var section in sections)
            {
                section.RefreshSection();
            }
            
            RebuildLayoutImmediate();
        }
        
        public void RefreshShop()
        {
            foreach (var section in sections)
            {
                section.RefreshSection();
            }
            
            RebuildLayoutImmediate();
        }
        
        private void RebuildLayoutImmediate()
        {
            if (sectionContentContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(sectionContentContainer.GetComponent<RectTransform>());
            }
            
            foreach (var section in sections)
            {
                if (section != null && section.transform is RectTransform rectTransform)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }
            
            if (transform is RectTransform popupRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(popupRectTransform);
            }
        }
    }
}