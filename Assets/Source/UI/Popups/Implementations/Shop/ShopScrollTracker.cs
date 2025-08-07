using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlLine.UI
{
    public class ShopScrollTracker : MonoBehaviour
    {
        [Header("Scroll Configuration")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentTransform;
        
        [Header("Title Configuration")]
        [SerializeField] private List<TMP_Text> sectionTitles = new List<TMP_Text>();
        [SerializeField] private List<RectTransform> sectionHeaders = new List<RectTransform>();
        
        [Header("Color Settings")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = Color.gray;
        
        [Header("Tracking Settings")]
        [SerializeField] private float activationThreshold = 0.5f; 
        
        private int currentActiveIndex = -1;
        private Camera uiCamera;
        
        private void Start()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();
                
            if (contentTransform == null && scrollRect != null)
                contentTransform = scrollRect.content;
                
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;
                
            if (scrollRect != null)
                scrollRect.onValueChanged.AddListener(OnScrollChanged);

            foreach (var title in sectionTitles)
            {
                if (title != null)
                    title.color = inactiveColor;
            }
                
            UpdateActiveTitles();
        }
        
        private void OnDestroy()
        {
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }
        
        private void OnScrollChanged(Vector2 scrollValue)
        {
            UpdateActiveTitles();
        }
        
        private void UpdateActiveTitles()
        {
            if (sectionHeaders.Count == 0 || sectionTitles.Count == 0)
                return;
                
            int newActiveIndex = GetActiveSection();
            
            if (newActiveIndex != currentActiveIndex)
            {
                if (currentActiveIndex >= 0 && currentActiveIndex < sectionTitles.Count)
                {
                    sectionTitles[currentActiveIndex].color = inactiveColor;
                }
                
                if (newActiveIndex >= 0 && newActiveIndex < sectionTitles.Count)
                {
                    sectionTitles[newActiveIndex].color = activeColor;
                }
                
                currentActiveIndex = newActiveIndex;
            }
        }
        
        private int GetActiveSection()
        {
            if (scrollRect == null || contentTransform == null)
                return -1;
                
            Rect scrollViewRect = GetScrollViewRect();
            
            for (int i = 0; i < sectionHeaders.Count; i++)
            {
                if (sectionHeaders[i] == null) continue;
                
                Rect sectionRect = GetWorldRect(sectionHeaders[i]);
                
                if (IsRectVisible(sectionRect, scrollViewRect))
                {
                    return i; 
                }
            }
            
            return -1;
        }
        
        private Rect GetScrollViewRect()
        {
            RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            scrollRectTransform.GetWorldCorners(corners);
            
            return new Rect(
                corners[0].x,
                corners[0].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y
            );
        }
        
        private Rect GetWorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            return new Rect(
                corners[0].x,
                corners[0].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y
            );
        }
        
        private bool IsRectVisible(Rect sectionRect, Rect scrollViewRect)
        {
            bool overlaps = !(sectionRect.xMax < scrollViewRect.xMin || 
                             sectionRect.xMin > scrollViewRect.xMax || 
                             sectionRect.yMax < scrollViewRect.yMin || 
                             sectionRect.yMin > scrollViewRect.yMax);
            
            if (!overlaps) return false;
            
            float overlapWidth = Mathf.Min(sectionRect.xMax, scrollViewRect.xMax) - 
                                Mathf.Max(sectionRect.xMin, scrollViewRect.xMin);
            float visibleRatio = overlapWidth / sectionRect.width; 
            
            return visibleRatio >= activationThreshold;
        }
        
        public void SetActiveSection(int index)
        {
            if (index < 0 || index >= sectionTitles.Count)
                return;
                
            foreach (var title in sectionTitles)
            {
                title.color = inactiveColor;
            }
            
            sectionTitles[index].color = activeColor;
            currentActiveIndex = index;
        }
        
        public void ScrollToSection(int index)
        {
            if (index < 0 || index >= sectionHeaders.Count || scrollRect == null)
                return;
                
            RectTransform header = sectionHeaders[index];
            if (header == null) return;
            
            float headerPosition = Mathf.Abs(header.anchoredPosition.x);
            float contentWidth = contentTransform.rect.width; 
            float viewportWidth = scrollRect.viewport.rect.width; 
            
            if (contentWidth > viewportWidth)
            {
                float normalizedPosition = Mathf.Clamp01(headerPosition / (contentWidth - viewportWidth));
                scrollRect.horizontalNormalizedPosition = normalizedPosition; 
            }
        }
    }
}