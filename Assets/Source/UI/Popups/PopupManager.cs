using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MageLock.Events;
using MageLock.DependencyInjection;

namespace MageLock.UI
{
    public enum PopupType
    {
        None,
        Settings,
        LanguageSelection,
        MatchMaking,
        Shop,
        Characters
    }

    public class PopupManager : MonoBehaviour
    {
        [System.Serializable]
        public class PopupPrefabEntry
        {
            public PopupType type;
            public GameObject prefab;
        }

        [SerializeField] private List<PopupPrefabEntry> popupPrefabs = new();
        [SerializeField] private int basePopupSortOrder = 1000;
        
        private readonly Stack<GameObject> _activePopups = new();
        private readonly Dictionary<PopupType, GameObject> _activePopupsByType = new();
        private readonly Dictionary<GameObject, int> _popupSortOrders = new();
        private int _currentSortOrder;

        [Inject] private DIContainer _diContainer;

        private void Awake()
        {
            _currentSortOrder = basePopupSortOrder;
            _diContainer ??= DIContainer.Instance;
        }

        private GameObject GetPopupPrefab(PopupType popupType)
        {
            if (popupType == PopupType.None)
            {
                Debug.LogWarning("Attempted to get None popup type");
                return null;
            }

            foreach (PopupPrefabEntry entry in popupPrefabs)
            {
                if (entry.type == popupType && entry.prefab != null)
                {
                    return entry.prefab;
                }
            }

            Debug.LogError($"No prefab found with PopupType: {popupType}");
            return null;
        }

        public bool IsPopupActive(PopupType popupType)
        {
            return _activePopupsByType.ContainsKey(popupType);
        }

        public GameObject ShowPopup(PopupType popupType)
        {
            if (IsPopupActive(popupType))
            {
                Debug.LogWarning($"Popup of type {popupType} is already active. Cannot open duplicate.");
                return _activePopupsByType[popupType];
            }

            GameObject prefab = GetPopupPrefab(popupType);
            
            if (prefab == null)
            {
                return null;
            }

            return ShowPopupFromPrefab(prefab, popupType);
        }

        private GameObject ShowPopupFromPrefab(GameObject prefab, PopupType popupType)
        {
            if (prefab == null)
            {
                Debug.LogError("Attempted to show null popup prefab");
                return null;
            }

            GameObject popupInstance = Instantiate(prefab);
            
            SetPopupSortingOrder(popupInstance);
            
            Popup popupComponent = popupInstance.GetComponent<Popup>();

            if (popupComponent != null)
            {
                popupComponent.PopupType = popupType;
            }
            else
            {
                Debug.LogWarning("Popup prefab does not have a Popup component");
            }

            InjectDependenciesIntoPopup(popupInstance);

            if (popupComponent != null)
            {
                popupComponent.Initialize();
            }

            _activePopups.Push(popupInstance);
            _activePopupsByType[popupType] = popupInstance;

            Debug.Log($"[PopupManager] Successfully created and injected popup: {popupType} with GameObject: {popupInstance.name}");

            return popupInstance;
        }
        
        private void InjectDependenciesIntoPopup(GameObject popupInstance)
        {
            if (popupInstance == null)
            {
                Debug.LogError("[PopupManager] Cannot inject dependencies into null popup instance");
                return;
            }

            if (_diContainer == null)
            {
                Debug.LogError("[PopupManager] DIContainer is null. Cannot inject dependencies into popup.");
                return;
            }

            try
            {
                Debug.Log($"[PopupManager] Starting dependency injection for popup: {popupInstance.name}");
                
                _diContainer.InjectIntoHierarchy(popupInstance);
                
                Debug.Log($"[PopupManager] Completed dependency injection for popup hierarchy: {popupInstance.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PopupManager] Failed to inject dependencies into popup {popupInstance.name}: {e.Message}");
                Debug.LogError($"[PopupManager] Stack trace: {e.StackTrace}");
            }
        }

        private void SetPopupSortingOrder(GameObject popupInstance)
        {
            if (popupInstance == null)
            {
                Debug.LogError("Cannot set sorting order on null popup instance");
                return;
            }

            Canvas canvas = popupInstance.GetComponent<Canvas>();

            if (canvas == null)
            {
                canvas = popupInstance.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = _currentSortOrder;
            
            _popupSortOrders[popupInstance] = _currentSortOrder;
            _currentSortOrder++;
        }

        public T ShowPopup<T>(PopupType popupType, object data = null) where T : Popup
        {
            GameObject popupInstance = ShowPopup(popupType);

            if (popupInstance != null)
            {
                T popupComponent = popupInstance.GetComponent<T>();

                if (popupComponent != null && data != null)
                {
                    popupComponent.SetData(data);
                }
                
                return popupComponent;
            }

            return null;
        }

        public void CloseTopPopup()
        {
            if (_activePopups.Count > 0)
            {
                GameObject popup = _activePopups.Pop();
        
                if (popup == null)
                {
                    Debug.LogWarning("[PopupManager] Popup GameObject was destroyed during scene transition.");
            
                    if (_activePopups.Count == 0)
                    {
                        _currentSortOrder = basePopupSortOrder;
                    }
                    else
                    {
                        _currentSortOrder--;
                    }
                    
                    return;
                }
        
                var popupComponent = popup.GetComponent<Popup>();

                if (popupComponent != null)
                {
                    _activePopupsByType.Remove(popupComponent.PopupType);
                }
        
                if (_popupSortOrders.ContainsKey(popup))
                {
                    _popupSortOrders.Remove(popup);
                }
        
                if (_activePopups.Count == 0)
                {
                    _currentSortOrder = basePopupSortOrder;
                }
                else
                {
                    _currentSortOrder--;
                }
        
                Debug.Log($"[PopupManager] Closing popup: {popup.name}");
                Destroy(popup);
            }
        }

        public void CloseAllPopups()
        {
            Debug.Log($"[PopupManager] Closing all {_activePopups.Count} active popups");
            
            while (_activePopups.Count > 0)
            {
                GameObject popup = _activePopups.Pop();
                Destroy(popup);
            }
            
            _activePopupsByType.Clear();
            _popupSortOrders.Clear();
            _currentSortOrder = basePopupSortOrder;
        }

        public int GetActivePopupCount()
        {
            return _activePopups.Count;
        }

        public PopupType[] GetActivePopupTypes()
        {
            return _activePopupsByType.Keys.ToArray();
        }
    }

    public static class PopupController
    {
        private static readonly Stack<PopupType> PopupStack = new();
        private static readonly Dictionary<PopupType, PopupOptions> PopupOptionsCache = new();
        
        private static PopupManager PopupManagerInstance => DIContainer.Instance.GetService<PopupManager>();
        
        public static void ShowPopup(PopupType popupType, PopupOptions options = default)
        {
            if (PopupManagerInstance != null && PopupManagerInstance.IsPopupActive(popupType))
            {
                Debug.LogWarning($"Popup of type {popupType} is already open. Ignoring duplicate request.");
                return;
            }

            PopupOptionsCache[popupType] = options;
            PopupStack.Push(popupType);
            
            EventsBus.Trigger(new PopupOpenedEvent(popupType, options));
            
            InternalShowPopup(popupType);
        }
        
        public static void CloseCurrentPopup()
        {
            if (PopupStack.Count == 0)
            {
                return;
            }
            
            PopupType currentPopup = PopupStack.Pop();

            PopupOptionsCache.Remove(currentPopup, out var options);
            
            EventsBus.Trigger(new PopupClosedEvent(currentPopup, options));
            
            InternalClosePopup(currentPopup);
            
            if (PopupStack.Count > 0)
            {
                PopupType previousPopup = PopupStack.Peek();
                if (PopupOptionsCache.TryGetValue(previousPopup, out PopupOptions previousOptions))
                {
                    EventsBus.Trigger(new PopupOpenedEvent(previousPopup, previousOptions));
                }
            }
        }
        
        public static void CloseAllPopups()
        {
            while (PopupStack.Count > 0)
            {
                PopupType popupType = PopupStack.Pop();

                PopupOptionsCache.Remove(popupType, out _);
            }
            
            EventsBus.Trigger(new AllPopupsClosedEvent());
            PopupManagerInstance?.CloseAllPopups();
        }
        
        private static void InternalShowPopup(PopupType popupType)
        {
            var popupManager = PopupManagerInstance;
            if (popupManager != null)
            {
                popupManager.ShowPopup(popupType);
            }
            else
            {
                Debug.LogError("PopupManager is not registered in DI container when trying to show popup: " + popupType);
            }
        }
        
        private static void InternalClosePopup(PopupType popupType)
        {
            var popupManager = PopupManagerInstance;
            if (popupManager != null)
            {
                popupManager.CloseTopPopup();
            }
            else
            {
                Debug.LogError("PopupManager is not registered in DI container when trying to close popup: " + popupType);
            }
        }
        
        public static bool IsAnyPopupOpen()
        {
            return PopupStack.Count > 0;
        }
        
        public static bool IsPopupOpen(PopupType popupType)
        {
            return PopupManagerInstance != null && PopupManagerInstance.IsPopupActive(popupType);
        }
        
        public static PopupType? GetCurrentPopup()
        {
            return PopupStack.Count > 0 ? PopupStack.Peek() : null;
        }
        
        public static int GetPopupCount()
        {
            return PopupStack.Count;
        }
        
        public static PopupType[] GetActivePopupTypes()
        {
            return PopupManagerInstance?.GetActivePopupTypes() ?? new PopupType[0];
        }
    }
}