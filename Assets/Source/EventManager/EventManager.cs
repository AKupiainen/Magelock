using System;
using System.Collections.Generic;
using UnityEngine;


namespace BrawlLine.Events
{
    /// <summary>
    /// Base interface for all event data
    /// </summary>
    public interface IEventData {}
    
    /// <summary>
    /// Interface for the event manager to support dependency injection
    /// </summary>
    public interface IEventManager
    {
        void Subscribe<T>(Action<T> callback) where T : IEventData;
        void Unsubscribe<T>(Action<T> callback) where T : IEventData;
        void TriggerEvent<T>(T eventData) where T : IEventData;
        void UnsubscribeAll<T>() where T : IEventData;
        void ClearAllSubscriptions();
        int GetSubscriberCount<T>() where T : IEventData;
        Type[] GetRegisteredEventTypes();
#if UNITY_EDITOR
        void DebugPrintAllSubscriptions();
#endif
    }
    
    /// <summary>
    /// Generic event manager that handles type-based events with custom data
    /// </summary>
    public class EventManager : IEventManager
    {
        private readonly Dictionary<Type, List<Delegate>> eventSubscriptions = new();
        private readonly object lockObject = new();
        
        /// <summary>
        /// Subscribe to an event type with a callback
        /// </summary>
        /// <typeparam name="T">Event data type that implements IEventData</typeparam>
        /// <param name="callback">Callback function to be called when event is triggered</param>
        public void Subscribe<T>(Action<T> callback) where T : IEventData
        {
            if (callback == null)
            {
                Debug.LogWarning("Attempted to subscribe with null callback");
                return;
            }
            
            lock (lockObject)
            {
                Type eventType = typeof(T);
                
                if (!eventSubscriptions.ContainsKey(eventType))
                {
                    eventSubscriptions[eventType] = new List<Delegate>();
                }
                
                eventSubscriptions[eventType].Add(callback);
                Debug.Log($"Subscribed to event: {eventType.Name}. Total subscribers: {eventSubscriptions[eventType].Count}");
            }
        }
        
        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        /// <typeparam name="T">Event data type that implements IEventData</typeparam>
        /// <param name="callback">Callback function to remove</param>
        public void Unsubscribe<T>(Action<T> callback) where T : IEventData
        {
            if (callback == null)
            {
                Debug.LogWarning("Attempted to unsubscribe with null callback");
                return;
            }
            
            lock (lockObject)
            {
                Type eventType = typeof(T);
                
                if (eventSubscriptions.ContainsKey(eventType))
                {
                    bool removed = eventSubscriptions[eventType].Remove(callback);
                    
                    if (removed)
                    {
                        Debug.Log($"Unsubscribed from event: {eventType.Name}. Remaining subscribers: {eventSubscriptions[eventType].Count}");
                        
                        if (eventSubscriptions[eventType].Count == 0)
                        {
                            eventSubscriptions.Remove(eventType);
                            Debug.Log($"Removed empty subscription list for: {eventType.Name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Callback not found for event type: {eventType.Name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"No subscriptions found for event type: {eventType.Name}");
                }
            }
        }
        
        /// <summary>
        /// Trigger an event with the specified data
        /// </summary>
        /// <typeparam name="T">Event data type that implements IEventData</typeparam>
        /// <param name="eventData">The event data to pass to subscribers</param>
        public void TriggerEvent<T>(T eventData) where T : IEventData
        {
            if (eventData == null)
            {
                Debug.LogWarning($"Attempted to trigger event with null data for type: {typeof(T).Name}");
                return;
            }
            
            List<Delegate> subscribers = null;
            
            lock (lockObject)
            {
                Type eventType = typeof(T);
                
                if (eventSubscriptions.TryGetValue(eventType, out var subscription))
                {
                    subscribers = new List<Delegate>(subscription);
                }
            }
            
            if (subscribers != null && subscribers.Count > 0)
            {
                Debug.Log($"Triggering event: {typeof(T).Name} with {subscribers.Count} subscribers");
                
                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        var callback = subscriber as Action<T>;
                        callback?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error invoking callback for event {typeof(T).Name}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
            else
            {
                Debug.Log($"No subscribers for event: {typeof(T).Name}");
            }
        }
        
        /// <summary>
        /// Unsubscribe all callbacks for a specific event type
        /// </summary>
        /// <typeparam name="T">Event data type to clear</typeparam>
        public void UnsubscribeAll<T>() where T : IEventData
        {
            lock (lockObject)
            {
                Type eventType = typeof(T);
                
                if (eventSubscriptions.ContainsKey(eventType))
                {
                    int count = eventSubscriptions[eventType].Count;
                    eventSubscriptions.Remove(eventType);
                    Debug.Log($"Unsubscribed all {count} callbacks for event: {eventType.Name}");
                }
            }
        }
        
        /// <summary>
        /// Clear all event subscriptions
        /// </summary>
        public void ClearAllSubscriptions()
        {
            lock (lockObject)
            {
                int totalCount = 0;
                foreach (var kvp in eventSubscriptions)
                {
                    totalCount += kvp.Value.Count;
                }
                
                eventSubscriptions.Clear();
                Debug.Log($"Cleared all event subscriptions. Total callbacks removed: {totalCount}");
            }
        }
        
        /// <summary>
        /// Get the number of subscribers for a specific event type
        /// </summary>
        /// <typeparam name="T">Event data type to check</typeparam>
        /// <returns>Number of subscribers</returns>
        public int GetSubscriberCount<T>() where T : IEventData
        {
            lock (lockObject)
            {
                Type eventType = typeof(T);
                
                if (eventSubscriptions.ContainsKey(eventType))
                {
                    return eventSubscriptions[eventType].Count;
                }
                
                return 0;
            }
        }
        
        /// <summary>
        /// Get all registered event types
        /// </summary>
        /// <returns>Array of registered event types</returns>
        public Type[] GetRegisteredEventTypes()
        {
            lock (lockObject)
            {
                var types = new Type[eventSubscriptions.Keys.Count];
                eventSubscriptions.Keys.CopyTo(types, 0);
                return types;
            }
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Debug method to log all current subscriptions
        /// </summary>
        [ContextMenu("Debug Print All Subscriptions")]
        public void DebugPrintAllSubscriptions()
        {
            lock (lockObject)
            {
                Debug.Log("=== Event Manager Subscriptions ===");
                
                if (eventSubscriptions.Count == 0)
                {
                    Debug.Log("No active subscriptions");
                    return;
                }
                
                foreach (var kvp in eventSubscriptions)
                {
                    Debug.Log($"Event: {kvp.Key.Name} - Subscribers: {kvp.Value.Count}");
                }
                
                Debug.Log("=== End Subscriptions ===");
            }
        }
#endif
    }
    
    /// <summary>
    /// Static helper class for easier access to EventManager using dependency injection
    /// </summary>
    public static class EventsBus
    {
        private static IEventManager _eventManager;
        
        /// <summary>
        /// Initialize the EventsBus with an IEventManager instance
        /// This should be called during application startup after DI container setup
        /// </summary>
        /// <param name="manager">The event manager instance to use</param>
        public static void Initialize(IEventManager manager)
        {
            _eventManager = manager;
            Debug.Log("EventsBus initialized successfully");
        }
        
        /// <summary>
        /// Check if EventsBus has been initialized
        /// </summary>
        public static bool IsInitialized => _eventManager != null;
        
        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        /// <typeparam name="T">Event data type</typeparam>
        /// <param name="callback">Callback function</param>
        public static void Subscribe<T>(Action<T> callback) where T : IEventData
        {
            if (_eventManager == null)
            {
                Debug.LogError("EventsBus not initialized! Call EventsBus.Initialize() first.");
                return;
            }

            _eventManager.Subscribe(callback);
        }
        
        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        /// <typeparam name="T">Event data type</typeparam>
        /// <param name="callback">Callback function</param>
        public static void Unsubscribe<T>(Action<T> callback) where T : IEventData
        {
            if (_eventManager == null)
            {
                Debug.LogError("EventsBus not initialized! Call EventsBus.Initialize() first.");
                return;
            }

            _eventManager.Unsubscribe(callback);
        }
        
        /// <summary>
        /// Trigger an event
        /// </summary>
        /// <typeparam name="T">Event data type</typeparam>
        /// <param name="eventData">Event data</param>
        public static void Trigger<T>(T eventData) where T : IEventData
        {
            if (_eventManager == null)
            {
                Debug.LogError("EventsBus not initialized! Call EventsBus.Initialize() first.");
                return;
            }

            _eventManager.TriggerEvent(eventData);
        }
        
        /// <summary>
        /// Unsubscribe all callbacks for a specific event type
        /// </summary>
        /// <typeparam name="T">Event data type</typeparam>
        public static void UnsubscribeAll<T>() where T : IEventData
        {
            if (_eventManager == null)
            {
                Debug.LogError("EventsBus not initialized! Call EventsBus.Initialize() first.");
                return;
            }

            _eventManager.UnsubscribeAll<T>();
        }
        
        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        public static void ClearAll()
        {
            if (_eventManager == null)
            {
                Debug.LogError("EventsBus not initialized! Call EventsBus.Initialize() first.");
                return;
            }

            _eventManager.ClearAllSubscriptions();
        }
        
        /// <summary>
        /// Get subscriber count for an event type
        /// </summary>
        /// <typeparam name="T">Event data type</typeparam>
        /// <returns>Number of subscribers</returns>
        public static int GetSubscriberCount<T>() where T : IEventData
        {
            if (_eventManager == null)
            {
                Debug.LogError("EventsBus not initialized! Call EventsBus.Initialize() first.");
                return 0;
            }
            
            return _eventManager.GetSubscriberCount<T>();
        }
        
        /// <summary>
        /// Reset the EventsBus (useful for testing or cleanup)
        /// </summary>
        public static void Reset()
        {
            if (_eventManager != null)
            {
                _eventManager.ClearAllSubscriptions();
                _eventManager = null;
                Debug.Log("EventsBus reset");
            }
        }
    }
}