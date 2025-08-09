using System.Collections.Generic;
using System;
using UnityEngine;
using MageLock.Events;

namespace MageLock.UI
{
    public class ViewManager
    {
        private readonly Dictionary<ViewType, BaseView> _viewRegistry = new();
        private readonly Dictionary<ViewType, ViewState> _viewStates = new();
        private struct ViewState
        {
            public int OriginalSortingOrder;
        }

        public void Initialize()
        {
            EventsBus.Subscribe<PopupOpenedEvent>(OnPopupOpened);
            EventsBus.Subscribe<PopupClosedEvent>(OnPopupClosed);
            EventsBus.Subscribe<AllPopupsClosedEvent>(OnAllPopupsClosed);
        }

        public void Cleanup()
        {
            EventsBus.Unsubscribe<PopupOpenedEvent>(OnPopupOpened);
            EventsBus.Unsubscribe<PopupClosedEvent>(OnPopupClosed);
            EventsBus.Unsubscribe<AllPopupsClosedEvent>(OnAllPopupsClosed);

            _viewRegistry.Clear();
            _viewStates.Clear();
        }

        private void OnPopupOpened(PopupOpenedEvent eventData)
        {
            Debug.Log($"[ViewManager] Popup opened: {eventData.PopupType}");
            ApplyPopupOptions(eventData.Options);
        }

        private void OnPopupClosed(PopupClosedEvent eventData)
        {
            Debug.Log($"[ViewManager] Popup closed: {eventData.PopupType}");
            RestorePopupOptions(eventData.Options);
        }

        private void OnAllPopupsClosed(AllPopupsClosedEvent eventData)
        {
            Debug.Log("[ViewManager] All popups closed, restoring all views");
            RestoreAllViews();
        }

        public void RegisterView(BaseView view)
        {
            var viewType = GetViewTypeFromComponent(view);

            if (_viewRegistry.TryAdd(viewType, view))
            {
                Debug.Log($"[ViewManager] Registered view: {viewType}");
            }
            else
            {
                Debug.LogWarning($"[ViewManager] View already registered: {viewType}");
            }
        }

        public void UnregisterView(BaseView view)
        {
            var viewType = GetViewTypeFromComponent(view);
            if (_viewRegistry.TryGetValue(viewType, out BaseView existingView) && existingView == view)
            {
                _viewRegistry.Remove(viewType);
                _viewStates.Remove(viewType);
                Debug.Log($"[ViewManager] Unregistered view: {viewType}");
            }
            else
            {
                Debug.LogWarning($"[ViewManager] Attempted to unregister non-matching or missing view: {viewType}");
            }
        }

        private ViewType GetViewTypeFromComponent(BaseView view)
        {
            return view switch
            {
                MainMenuView => ViewType.MainMenu,
                PlayerProfileView => ViewType.PlayerProfile,
                CurrencyView => ViewType.Currency,
                _ => default
            };
        }

        public BaseView GetView(ViewType viewType)
        {
            _viewRegistry.TryGetValue(viewType, out BaseView view);
            return view;
        }

        public T GetView<T>() where T : BaseView
        {
            foreach (var view in _viewRegistry.Values)
            {
                if (view is T typedView)
                    return typedView;
            }
            return null;
        }

        private void ApplyViewConfig(ViewConfig config)
        {
            var view = GetView(config.viewType);
            if (view == null)
            {
                Debug.LogWarning($"View not found for type: {config.viewType}");
                return;
            }

            StoreViewState(config.viewType, view);

            if (config.ShouldHide)
            {
                view.Hide(config.ShouldAnimate);
            }
            else if (config.ShouldDisable)
            {
                view.SetInteractable(false);
            }

            if (config.ShouldBringToFront)
            {
                view.BringToFront();
            }
            else if (config.ShouldSetSortingOrder)
            {
                view.SetSortingOrder(config.sortingOrder);
            }
        }

        public void RestoreViewConfig(ViewConfig config)
        {
            var view = GetView(config.viewType);
            if (view == null)
            {
                Debug.LogWarning($"View not found for type: {config.viewType}");
                return;
            }

            if (config.ShouldHide)
            {
                view.Show(config.ShouldAnimate);
            }
            else if (config.ShouldDisable)
            {
                view.SetInteractable(true);
            }

            if (config.ShouldBringToFront || config.ShouldSetSortingOrder)
            {
                view.RestoreOriginalSortingOrder();
            }
        }

        private void StoreViewState(ViewType viewType, BaseView view)
        {
            if (!_viewStates.ContainsKey(viewType))
            {
                _viewStates[viewType] = new ViewState
                {
                    OriginalSortingOrder = 0
                };
            }
        }

        public void ApplyPopupOptions(PopupOptions options)
        {
            if (options.ViewConfigs == null) return;

            foreach (var config in options.ViewConfigs)
            {
                ApplyViewConfig(config);
            }
        }

        public void RestorePopupOptions(PopupOptions options)
        {
            if (options.ViewConfigs == null) return;

            foreach (var config in options.ViewConfigs)
            {
                RestoreViewConfig(config);
            }
        }

        public void RestoreAllViews()
        {
            foreach (var view in _viewRegistry.Values)
            {
                if (view != null)
                {
                    view.Show();
                    view.SetInteractable(true);
                    view.RestoreOriginalSortingOrder();
                }
            }

            _viewStates.Clear();
        }

        public void RefreshPlayerProfile()
        {
            var profileView = GetView<PlayerProfileView>();
            profileView.RefreshDisplay();
        }

        public void AddPlayerExperience(int amount)
        {
            var profileView = GetView<PlayerProfileView>();
            profileView.AddExperienceWithFeedback(amount);
        }

        public void PerformActionOnView<T>(Action<T> action) where T : BaseView
        {
            var view = GetView<T>();
            if (view != null)
            {
                action(view);
            }
        }

        public void BringViewToFront(ViewType viewType)
        {
            var view = GetView(viewType);
            view.BringToFront();
        }

        public void SetViewSortingOrder(ViewType viewType, int order)
        {
            var view = GetView(viewType);
            view.SetSortingOrder(order);
        }
    }
}