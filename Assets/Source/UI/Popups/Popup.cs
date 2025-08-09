using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;

namespace MageLock.UI
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
        
        private Tween _currentTween;
        private bool _isAnimating;
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

            _currentTween?.Kill();

            _isAnimating = true;
            scalingTransform.localScale = Vector3.zero;
            
            _currentTween = scalingTransform.DOScale(Vector3.one, animationDuration)
                .SetEase(openEase)
                .OnComplete(() => {
                    _currentTween = null;
                    _isAnimating = false;
                });
        }

        public virtual void SetData(object data) {}

        protected virtual void OnPopupOpened() {}

        protected virtual void Close()
        {
            if (_isAnimating)
                return;
            
            if (scalingTransform == null)
            {
                ClosePopup();
                return;
            }
            
            if (_currentTween != null)
                _currentTween.Kill();
            
            _isAnimating = true;
            
            _currentTween = scalingTransform.DOScale(Vector3.zero, animationDuration)
                .SetEase(closeEase)
                .OnComplete(() => {
                    _currentTween = null;
                    _isAnimating = false;
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
                
            if (_isAnimating)
                return;
                
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;
                
            Close();
        }

        protected virtual void OnDestroy()
        {
            if (_currentTween == null)
                return;
                
            _currentTween.Kill();
            _currentTween = null;
        }
    }
}