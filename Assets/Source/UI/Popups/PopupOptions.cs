using System;
using System.Linq;

namespace MageLock.UI
{
    public enum ViewType
    {
        MainMenu,
        PlayerProfile,
        Currency
    }

    [Flags]
    public enum ViewAction
    {
        None = 0,
        Disable = 1 << 0,
        Hide = 1 << 1,
        Animate = 1 << 2,
        BringToFront = 1 << 3,
        SetSortingOrder = 1 << 4
    }

    [Serializable]
    public struct ViewConfig
    {
        public ViewType viewType;
        public ViewAction actions;
        public int sortingOrder;

        public ViewConfig(ViewType viewType, ViewAction actions, int sortingOrder = 0)
        {
            this.viewType = viewType;
            this.actions = actions;
            this.sortingOrder = sortingOrder;
        }

        public bool ShouldDisable => actions.HasFlag(ViewAction.Disable);
        public bool ShouldHide => actions.HasFlag(ViewAction.Hide);
        public bool ShouldAnimate => actions.HasFlag(ViewAction.Animate);
        public bool ShouldBringToFront => actions.HasFlag(ViewAction.BringToFront);
        public bool ShouldSetSortingOrder => actions.HasFlag(ViewAction.SetSortingOrder);
    }

    [Serializable]
    public struct PopupOptions
    {
        public ViewConfig[] ViewConfigs { get; set; }

        public PopupOptions(params ViewConfig[] viewConfigs)
        {
            ViewConfigs = viewConfigs ?? Array.Empty<ViewConfig>();
        }

        public static PopupOptions ForView(ViewType viewType, ViewAction actions, int sortingOrder = 0)
        {
            return new PopupOptions(new ViewConfig(viewType, actions, sortingOrder));
        }

        public static PopupOptions ForViews(params ViewConfig[] viewConfigs)
        {
            return new PopupOptions(viewConfigs);
        }

        public static PopupOptions None => new PopupOptions(Array.Empty<ViewConfig>()); 

        public static PopupOptions Disable(ViewType viewType) =>
            ForView(viewType, ViewAction.Disable);

        public static PopupOptions Hide(ViewType viewType) =>
            ForView(viewType, ViewAction.Hide);

        public static PopupOptions HideAnimated(ViewType viewType) =>
            ForView(viewType, ViewAction.Hide | ViewAction.Animate);

        public static PopupOptions BringToFront(ViewType viewType) =>
            ForView(viewType, ViewAction.BringToFront);

        public static PopupOptions DisableAll(params ViewType[] viewTypes)
        {
            if (viewTypes == null || viewTypes.Length == 0)
                viewTypes = GetAllViewTypes();
            
            return ForViews(viewTypes.Select(vt => new ViewConfig(vt, ViewAction.Disable)).ToArray());
        }

        public static PopupOptions HideAll(params ViewType[] viewTypes)
        {
            if (viewTypes == null || viewTypes.Length == 0)
                viewTypes = GetAllViewTypes();
            
            return ForViews(viewTypes.Select(vt => new ViewConfig(vt, ViewAction.Hide)).ToArray());
        }

        public static PopupOptions HideAllAnimated(params ViewType[] viewTypes)
        {
            if (viewTypes == null || viewTypes.Length == 0)
                viewTypes = GetAllViewTypes();
            
            return ForViews(viewTypes.Select(vt => new ViewConfig(vt, ViewAction.Hide | ViewAction.Animate)).ToArray());
        }

        public static PopupOptions DisableAllExcept(ViewType exceptViewType)
        {
            var allViews = GetAllViewTypes();
            var viewsToDisable = allViews.Where(vt => vt != exceptViewType).ToArray();
            
            var configs = new ViewConfig[viewsToDisable.Length + 1];
            
            for (int i = 0; i < viewsToDisable.Length; i++)
            {
                configs[i] = new ViewConfig(viewsToDisable[i], ViewAction.Disable);
            }
            
            configs[^1] = new ViewConfig(exceptViewType, ViewAction.BringToFront); // Ensure the "except" view is brought to front
            
            return ForViews(configs);
        }

        public static PopupOptions HideAllExcept(ViewType exceptViewType)
        {
            var allViews = GetAllViewTypes();
            var viewsToHide = allViews.Where(vt => vt != exceptViewType).ToArray();
            
            var configs = new ViewConfig[viewsToHide.Length + 1];
            
            for (int i = 0; i < viewsToHide.Length; i++)
            {
                configs[i] = new ViewConfig(viewsToHide[i], ViewAction.Hide);
            }
            
            configs[^1] = new ViewConfig(exceptViewType, ViewAction.BringToFront); // Ensure the "except" view is brought to front
            
            return ForViews(configs);
        }

        public static PopupOptions WithAction(ViewType viewType, ViewAction action, int sortingOrder = 0) =>
            ForView(viewType, action, sortingOrder);

        public static PopupOptions WithActions(ViewType viewType, ViewAction[] actions, int sortingOrder = 0)
        {
            var combinedActions = ViewAction.None;
            foreach (var action in actions)
            {
                combinedActions |= action;
            }
            return ForView(viewType, combinedActions, sortingOrder);
        }

        public static PopupOptions WithCustomSortingOrder(ViewType viewType, int sortingOrder, bool animate = false)
        {
            var actions = ViewAction.SetSortingOrder;
            if (animate) actions |= ViewAction.Animate;
            
            return ForView(viewType, actions, sortingOrder);
        }

        public static PopupOptions WithLayeredViews(
            (ViewType viewType, int sortingOrder)[] layers, 
            bool animate = false)
        {
            var configs = new ViewConfig[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                var actions = ViewAction.SetSortingOrder;
                if (animate) actions |= ViewAction.Animate;
                
                configs[i] = new ViewConfig(layers[i].viewType, actions, layers[i].sortingOrder);
            }
            return ForViews(configs);
        }

        public static PopupOptions DisableMainMenuWithViewOnTop(ViewType topViewType)
        {
            var configs = new System.Collections.Generic.List<ViewConfig>();
            configs.Add(new ViewConfig(ViewType.MainMenu, ViewAction.Disable));
            configs.Add(new ViewConfig(topViewType, ViewAction.BringToFront));
            return ForViews(configs.ToArray());
        }

        public static PopupOptions HideMainMenuWithViewOnTop(ViewType topViewType)
        {
            var configs = new System.Collections.Generic.List<ViewConfig>();
            configs.Add(new ViewConfig(ViewType.MainMenu, ViewAction.Hide));
            configs.Add(new ViewConfig(topViewType, ViewAction.BringToFront));
            return ForViews(configs.ToArray());
        }

        public static PopupOptions DisableMainMenu => Disable(ViewType.MainMenu);
        public static PopupOptions DisablePlayerProfile => Disable(ViewType.PlayerProfile);
        public static PopupOptions DisableCurrency => Disable(ViewType.Currency);
        
        public static PopupOptions HideMainMenu => Hide(ViewType.MainMenu);
        public static PopupOptions HidePlayerProfile => Hide(ViewType.PlayerProfile);
        public static PopupOptions HideCurrency => Hide(ViewType.Currency);

        public static PopupOptions BringCurrencyToFront => BringToFront(ViewType.Currency);
        public static PopupOptions BringPlayerProfileToFront => BringToFront(ViewType.PlayerProfile);
        public static PopupOptions BringMainMenuToFront => BringToFront(ViewType.MainMenu);

        public static PopupOptions DisableAllExceptCurrency => DisableAllExcept(ViewType.Currency);

        private static ViewType[] GetAllViewTypes()
        {
            return Enum.GetValues(typeof(ViewType)).Cast<ViewType>().ToArray();
        }

        public ViewConfig? GetConfigForView(ViewType viewType)
        {
            if (ViewConfigs == null || ViewConfigs.Length == 0) return null;

            foreach (var config in ViewConfigs)
            {
                if (config.viewType == viewType)
                    return config;
            }
            return null;
        }

        public bool HasAction(ViewAction action)
        {
            if (ViewConfigs == null || ViewConfigs.Length == 0) return false;

            foreach (var config in ViewConfigs)
            {
                if (config.actions.HasFlag(action))
                    return true;
            }
            return false;
        }

        public bool HasViewWithAction(ViewType viewType, ViewAction action)
        {
            var config = GetConfigForView(viewType);
            return config?.actions.HasFlag(action) ?? false;
        }

        public int GetSortingOrderForView(ViewType viewType)
        {
            var config = GetConfigForView(viewType);
            return config?.sortingOrder ?? 0;
        }

        public ViewType[] GetViewsWithAction(ViewAction action)
        {
            if (ViewConfigs == null || ViewConfigs.Length == 0) return new ViewType[0];

            var result = new System.Collections.Generic.List<ViewType>();

            foreach (var config in ViewConfigs)
            {
                if (config.actions.HasFlag(action))
                    result.Add(config.viewType);
            }

            return result.ToArray();
        }
    }
}