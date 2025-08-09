using UnityEngine;
using MageLock.Localization;
using MageLock.DependencyInjection;
using JetBrains.Annotations;

namespace MageLock.UI
{
    public abstract class BaseView : MonoBehaviour
    {
        [Header("View Settings")]
        [SerializeField] protected bool isActiveOnStart = true;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected Canvas canvas;

        [Inject] private ViewManager _viewManager;

        private int _originalSortingOrder;
        private bool _hasStoredOriginalOrder;

        protected virtual void Awake()
        {
            if (canvas != null)
            {
                _originalSortingOrder = canvas.sortingOrder;
                _hasStoredOriginalOrder = true;
            }
        }

        protected virtual void Start()
        {
            DIContainer.Instance.InjectIntoHierarchy(gameObject);

            Initialize();
            SetupLocalization();
            SubscribeToEvents();

            if (isActiveOnStart)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        [UsedImplicitly]
        [PostInject]
        private void OnDependenciesInjected()
        {
            _viewManager.RegisterView(this);
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromEvents();
            _viewManager.UnregisterView(this);
        }

        protected virtual void Initialize() { }

        protected virtual void SetupLocalization()
        {
            UpdateLocalizedText();
            LocalizationService.OnLanguageChangedCallback += OnLanguageChanged;
        }

        protected virtual void SubscribeToEvents() { }

        protected virtual void UnsubscribeFromEvents()
        {
            LocalizationService.OnLanguageChangedCallback -= OnLanguageChanged;
        }

        protected virtual void OnLanguageChanged(SystemLanguage language)
        {
            UpdateLocalizedText();
        }

        protected virtual void UpdateLocalizedText() { }

        public virtual void Show(bool animate = false)
        {
            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            OnShow();
        }

        public virtual void Hide(bool animate = false)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            OnHide();
        }

        public virtual void SetInteractable(bool interactable)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable;
            }
        }

        public virtual void BringToFront()
        {
            if (canvas != null)
            {
                canvas.sortingOrder = 1000; 
            }
        }

        public virtual void SetSortingOrder(int order)
        {
            if (canvas != null)
            {
                canvas.sortingOrder = order;
            }
        }

        public virtual void RestoreOriginalSortingOrder()
        {
            if (canvas != null && _hasStoredOriginalOrder)
            {
                canvas.sortingOrder = _originalSortingOrder;
            }
        }

        public bool IsVisible()
        {
            return gameObject.activeInHierarchy && (canvasGroup == null || canvasGroup.alpha > 0f);
        }

        protected virtual void OnShow() { }

        protected virtual void OnHide() { }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
                
                if (canvas == null)
                {
                    canvas = GetComponentInParent<Canvas>();
                }
            }
        }
#endif
    }
}