using UnityEngine;
using System.Collections.Generic;
using BrawlLine.Player;
using BrawlLine.Events;

namespace BrawlLine.UI
{
    public class CurrencyView : BaseView
    {
        [Header("Currency Widgets")]
        [SerializeField] private List<CurrencyWidget> currencyWidgets = new List<CurrencyWidget>();
        
        protected override void Initialize()
        {
            base.Initialize();
            SetupCurrencyWidgets();
        }
        
        protected override void OnShow()
        {
            base.OnShow();
            RefreshAllCurrencies();
        }
        
        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();
            EventsBus.Subscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
            EventsBus.Subscribe<PlayerCurrencyAddedEvent>(OnCurrencyAdded);
            EventsBus.Subscribe<PlayerCurrencySpentEvent>(OnCurrencySpent);
        }
        
        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();
            EventsBus.Unsubscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
            EventsBus.Unsubscribe<PlayerCurrencyAddedEvent>(OnCurrencyAdded);
            EventsBus.Unsubscribe<PlayerCurrencySpentEvent>(OnCurrencySpent);
        }
        
        private void OnCurrencyChanged(PlayerCurrencyChangedEvent eventData)
        {
            RefreshCurrency(eventData.CurrencyType);
        }
        
        private void OnCurrencyAdded(PlayerCurrencyAddedEvent eventData)
        {
            RefreshCurrency(eventData.CurrencyType);
        }
        
        private void OnCurrencySpent(PlayerCurrencySpentEvent eventData)
        {
            RefreshCurrency(eventData.CurrencyType);
        }
        
        private void SetupCurrencyWidgets()
        {
            foreach (var widget in currencyWidgets)
            {
                if (widget != null)
                {
                    widget.Initialize(widget.GetCurrencyType(), OnCurrencyWidgetClicked);
                }
            }
        }
        
        private void OnCurrencyWidgetClicked(CurrencyType currencyType)
        {
            Debug.Log($"Currency clicked: {currencyType}");
            OpenShop(currencyType);
        }
        
        private void OpenShop(CurrencyType currencyType)
        {
            PopupController.ShowPopup(PopupType.Shop, PopupOptions.HideAllExcept(ViewType.Currency));
        }
        
        public void RefreshAllCurrencies()
        {
            foreach (var widget in currencyWidgets)
            {
                if (widget != null)
                {
                    widget.UpdateDisplay();
                }
            }
        }
        
        public void RefreshCurrency(CurrencyType currencyType)
        {
            var widget = GetCurrencyWidget(currencyType);
            if (widget != null)
            {
                widget.UpdateDisplay();
            }
        }
        
        private CurrencyWidget GetCurrencyWidget(CurrencyType currencyType)
        {
            foreach (var widget in currencyWidgets)
            {
                if (widget != null && widget.GetCurrencyType() == currencyType)
                {
                    return widget;
                }
            }
            return null;
        }
        
        public void SetCurrencyWidgetInteractable(CurrencyType currencyType, bool interactable)
        {
            var widget = GetCurrencyWidget(currencyType);
            if (widget != null)
            {
                widget.SetInteractable(interactable);
            }
        }
        
        public void SetAllCurrencyWidgetsInteractable(bool interactable)
        {
            foreach (var widget in currencyWidgets)
            {
                if (widget != null)
                {
                    widget.SetInteractable(interactable);
                }
            }
        }
        
        public int GetCurrencyWidgetCount()
        {
            return currencyWidgets.Count;
        }
        
        public bool HasCurrencyWidget(CurrencyType currencyType)
        {
            return GetCurrencyWidget(currencyType) != null;
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (currencyWidgets == null)
            {
                currencyWidgets = new List<CurrencyWidget>();
            }
        }
#endif
    }
}