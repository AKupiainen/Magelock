using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;

namespace BrawlLine.UI
{
    public abstract class Popup : MonoBehaviour
    {
        [Header("Popup Base References")]
        [SerializeField] protected Button closeButton;
        
        [Header("Animation Settings")]
        [SerializeField] protected float animationDuration = 0.3f;
        [SerializeField] protected Ease openEase = Ease.OutBack;
        [SerializeField] protected Ease closeEase = Ease.InBack;
        [SerializeField] protected RectTransform scalingTransform;
        
        [Header("Input Settings")]
        [SerializeField] protected bool allowBackKeyClose = true;
        
        private Tween currentTween;
        private bool isAnimating;
        public PopupType PopupType { get; set; }

        public virtual void Initialize()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            
            if (scalingTransform == null)
                scalingTransform = transform as RectTransform;
            
            OpenPopup();
            OnPopupOpened();
        }

        protected void OpenPopup()
        {
            if (scalingTransform == null)
                return;

            currentTween?.Kill();

            isAnimating = true;
            scalingTransform.localScale = Vector3.zero;
            
            currentTween = scalingTransform.DOScale(Vector3.one, animationDuration)
                .SetEase(openEase)
                .OnComplete(() => {
                    currentTween = null;
                    isAnimating = false;
                });
        }

        public virtual void SetData(object data) {}

        protected virtual void OnPopupOpened() {}

        protected virtual void Close()
        {
            if (isAnimating)
                return;
            
            if (scalingTransform == null)
            {
                ClosePopup();
                return;
            }
            
            if (currentTween != null)
                currentTween.Kill();
            
            isAnimating = true;
            
            currentTween = scalingTransform.DOScale(Vector3.zero, animationDuration)
                .SetEase(closeEase)
                .OnComplete(() => {
                    currentTween = null;
                    isAnimating = false;
                    ClosePopup();
                });
        }

        protected virtual void ClosePopup()
        {
            PopupController.CloseCurrentPopup();
        }

        protected virtual void Update()
        {
            if (!allowBackKeyClose)
                return;
                
            if (isAnimating)
                return;
                
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;
                
            Close();
        }

        protected virtual void OnDestroy()
        {
            if (currentTween == null)
                return;
                
            currentTween.Kill();
            currentTween = null;
        }
    }
}