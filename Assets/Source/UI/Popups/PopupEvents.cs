using MageLock.Events;

namespace MageLock.UI
{
    public class PopupOpenedEvent : IEventData
    {
        public PopupType PopupType { get; }
        public PopupOptions Options { get; }
        
        public PopupOpenedEvent(PopupType popupType, PopupOptions options)
        {
            PopupType = popupType;
            Options = options;
        }
    }
    
    public class PopupClosedEvent : IEventData
    {
        public PopupType PopupType { get; }
        public PopupOptions Options { get; }
        
        public PopupClosedEvent(PopupType popupType, PopupOptions options)
        {
            PopupType = popupType;
            Options = options;
        }
    }
    
    public class AllPopupsClosedEvent : IEventData
    {
        public AllPopupsClosedEvent() { }
    }
}