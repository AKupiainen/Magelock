using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MageLock.DependencyInjection
{
    public class DIContainer
    {
        private static DIContainer _instance;
        public static DIContainer Instance
        {
            get { return _instance ??= new DIContainer(); }
        }

        private readonly Dictionary<Type, object> singletonInstances = new();
        private readonly Dictionary<Type, Func<object>> factories = new();
        private readonly Dictionary<Type, LifetimeType> lifetimes = new();
        private readonly Dictionary<string, Type> namedBindings = new();
        private readonly Dictionary<string, object> namedSingletons = new();
        private readonly HashSet<Type> currentlyResolving = new();
        private readonly HashSet<GameObject> managedGameObjects = new();
        private readonly Dictionary<Type, List<Type>> interfaceImplementations = new();

        private enum LifetimeType
        {
            Singleton,
            Transient
        }

        private DIContainer() { }

        #region Registration Methods

        public void RegisterSingleton<T>(string name, T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            var type = typeof(T);
            var key = $"{type.FullName}#{name}";
            namedSingletons[key] = instance;
            namedBindings[name] = type;
            lifetimes[type] = LifetimeType.Singleton;
            
            Inject(instance);
            Debug.Log($"[DIContainer] Registered named singleton: {name} ({typeof(T).Name})");
        }

        public void RegisterSingleton<T>(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = typeof(T);
            singletonInstances[type] = instance;
            lifetimes[type] = LifetimeType.Singleton;
            
            Inject(instance);
        }

        public void RegisterSingleton<T>() where T : class, new()
        {
            RegisterSingleton<T, T>();
        }

        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var interfaceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);
            
            lifetimes[interfaceType] = LifetimeType.Singleton;
            
            if (!interfaceImplementations.ContainsKey(interfaceType))
            {
                interfaceImplementations[interfaceType] = new List<Type>();
            }
            
            interfaceImplementations[interfaceType].Add(implementationType);
            
            factories[interfaceType] = () =>
            {
                if (!singletonInstances.ContainsKey(interfaceType))
                {
                    var instance = new TImplementation();
                    Inject(instance);
                    singletonInstances[interfaceType] = instance;
                }
                return singletonInstances[interfaceType];
            };
        }

        public void RegisterSingletonFactory<T>(Func<T> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var type = typeof(T);
            lifetimes[type] = LifetimeType.Singleton;
            
            factories[type] = () =>
            {
                if (!singletonInstances.ContainsKey(type))
                {
                    var instance = factory();
                    if (instance != null)
                    {
                        Inject(instance);
                        singletonInstances[type] = instance;
                    }
                }
                return singletonInstances[type];
            };
        }

        public void RegisterMonoBehaviourSingleton<T>(T instance, bool dontDestroyOnLoad = true) where T : MonoBehaviour
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = typeof(T);
            singletonInstances[type] = instance;
            lifetimes[type] = LifetimeType.Singleton;
            
            if (dontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(instance.gameObject);
                managedGameObjects.Add(instance.gameObject);
            }
            
            Inject(instance);
            
            Debug.Log($"[DIContainer] Registered existing MonoBehaviour: {typeof(T).Name}");
        }

        public void RegisterMonoBehaviourSingleton<TInterface, TImplementation>(TImplementation instance, bool dontDestroyOnLoad = true) 
            where TImplementation : MonoBehaviour, TInterface
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var interfaceType = typeof(TInterface);
            singletonInstances[interfaceType] = instance;
            lifetimes[interfaceType] = LifetimeType.Singleton;
            
            if (dontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(instance.gameObject);
                managedGameObjects.Add(instance.gameObject);
            }
            
            Inject(instance);
            
            Debug.Log($"[DIContainer] Registered existing MonoBehaviour: {typeof(TImplementation).Name} as {typeof(TInterface).Name}");
        }

        public void RegisterMonoBehaviourSingleton<T>(bool dontDestroyOnLoad = true, string gameObjectName = null) 
            where T : MonoBehaviour
        {
            RegisterMonoBehaviourSingleton<T, T>(dontDestroyOnLoad, gameObjectName);
        }

        public void RegisterMonoBehaviourSingleton<TInterface, TImplementation>(bool dontDestroyOnLoad = true, string gameObjectName = null) 
            where TImplementation : MonoBehaviour, TInterface
        {
            var interfaceType = typeof(TInterface);
            lifetimes[interfaceType] = LifetimeType.Singleton;
            
            factories[interfaceType] = () =>
            {
                if (!singletonInstances.ContainsKey(interfaceType))
                {
                    var singletonInstance = InstantiateMonoBehaviour<TImplementation>(dontDestroyOnLoad, gameObjectName);
                    singletonInstances[interfaceType] = singletonInstance;
                }
                return singletonInstances[interfaceType];
            };
        }

        public void RegisterMonoBehaviourFromPrefab<T>(GameObject prefab, bool dontDestroyOnLoad = true) 
            where T : MonoBehaviour
        {
            RegisterMonoBehaviourFromPrefab<T, T>(prefab, dontDestroyOnLoad);
        }

        public void RegisterMonoBehaviourFromPrefab<TInterface, TImplementation>(GameObject prefab, bool dontDestroyOnLoad = true) 
            where TImplementation : MonoBehaviour, TInterface
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            var interfaceType = typeof(TInterface);
            lifetimes[interfaceType] = LifetimeType.Singleton;
            
            factories[interfaceType] = () =>
            {
                if (!singletonInstances.ContainsKey(interfaceType))
                {
                    var singletonInstance = InstantiateFromPrefab<TImplementation>(prefab, dontDestroyOnLoad);
                    singletonInstances[interfaceType] = singletonInstance;
                }
                return singletonInstances[interfaceType];
            };
        }

        public void RegisterTransient<T>() where T : class, new()
        {
            RegisterTransient<T, T>();
        }

        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var interfaceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);
            
            lifetimes[interfaceType] = LifetimeType.Transient;
            
            if (!interfaceImplementations.ContainsKey(interfaceType))
            {
                interfaceImplementations[interfaceType] = new List<Type>();
            }
            interfaceImplementations[interfaceType].Add(implementationType);
            
            factories[interfaceType] = () =>
            {
                var implementation = new TImplementation();
                Inject(implementation);
                return implementation;
            };
        }

        public void RegisterTransientFactory<T>(Func<T> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var type = typeof(T);
            lifetimes[type] = LifetimeType.Transient;
            
            factories[type] = () =>
            {
                var target = factory();
                if (target != null)
                {
                    Inject(target);
                }
                return target;
            };
        }

        public void RegisterCollection<T>(params T[] instances)
        {
            if (instances == null || instances.Length == 0)
                throw new ArgumentException("Instances cannot be null or empty", nameof(instances));

            var collectionType = typeof(IEnumerable<T>);
            var arrayType = typeof(T[]);
            var listType = typeof(List<T>);

            var instanceList = new List<T>(instances);
            
            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    Inject(instance);
                }
            }

            singletonInstances[collectionType] = instanceList;
            singletonInstances[arrayType] = instances;
            singletonInstances[listType] = instanceList;
            
            lifetimes[collectionType] = LifetimeType.Singleton;
            lifetimes[arrayType] = LifetimeType.Singleton;
            lifetimes[listType] = LifetimeType.Singleton;
        }

        #endregion

        #region Resolution Methods

        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        public T GetService<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            var type = typeof(T);
            var key = $"{type.FullName}#{name}";
            
            if (namedSingletons.TryGetValue(key, out object service))
            {
                return (T)service;
            }
            
            throw new InvalidOperationException($"Named service '{name}' of type {type.Name} is not registered.");
        }

        public object GetService(Type type)
        {
            if (currentlyResolving.Contains(type))
            {
                throw new InvalidOperationException($"Circular dependency detected while resolving {type.Name}");
            }

            try
            {
                currentlyResolving.Add(type);

                if (singletonInstances.TryGetValue(type, out object service))
                {
                    return service;
                }

                if (factories.TryGetValue(type, out Func<object> factory))
                {
                    return factory();
                }

                if (TryResolveImplicitSingleton(type, out object implicitService))
                {
                    return implicitService;
                }

                throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
            }
            finally
            {
                currentlyResolving.Remove(type);
            }
        }

        public IEnumerable<T> GetServices<T>()
        {
            var interfaceType = typeof(T);
            
            if (singletonInstances.TryGetValue(typeof(IEnumerable<T>), out object collection))
            {
                return (IEnumerable<T>)collection;
            }
            
            if (interfaceImplementations.TryGetValue(interfaceType, out List<Type> implementations))
            {
                var services = new List<T>();
                foreach (var implType in implementations)
                {
                    try
                    {
                        var service = GetService(implType);
                        if (service is T typedService)
                        {
                            services.Add(typedService);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[DIContainer] Failed to resolve implementation {implType.Name}: {e.Message}");
                    }
                }
                return services;
            }
            
            return new List<T>();
        }

        public bool TryGetService<T>(out T service)
        {
            try
            {
                service = GetService<T>();
                return true;
            }
            catch
            {
                service = default(T);
                return false;
            }
        }

        public bool TryGetService<T>(string name, out T service)
        {
            try
            {
                service = GetService<T>(name);
                return true;
            }
            catch
            {
                service = default(T);
                return false;
            }
        }

        #endregion

        #region Instantiation Methods

        public T InstantiateMonoBehaviour<T>(bool dontDestroyOnLoad = true, string gameObjectName = null) 
            where T : MonoBehaviour
        {
            var name = gameObjectName ?? typeof(T).Name;
            var gameObject = new GameObject(name);
            var component = gameObject.AddComponent<T>();
            
            if (dontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                managedGameObjects.Add(gameObject);
            }
            
            Inject(component);
            
            Debug.Log($"[DIContainer] Instantiated MonoBehaviour: {typeof(T).Name} on GameObject: {name}");
            return component;
        }

        public T InstantiateFromPrefab<T>(GameObject prefab, bool dontDestroyOnLoad = true) 
            where T : MonoBehaviour
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            var gameObject = UnityEngine.Object.Instantiate(prefab);
            var component = gameObject.GetComponent<T>();
            
            if (component == null)
                throw new InvalidOperationException($"Prefab {prefab.name} does not contain component of type {typeof(T).Name}");
            
            if (dontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                managedGameObjects.Add(gameObject);
            }
            
            InjectIntoHierarchy(gameObject);
            
            Debug.Log($"[DIContainer] Instantiated prefab: {prefab.name} and injected component: {typeof(T).Name}");
            return component;
        }

        public void InjectIntoHierarchy(GameObject gameObject)
        {
            if (gameObject == null) return;

            var allComponents = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
            
            foreach (var component in allComponents)
            {
                if (component != null && HasInjectableMembers(component.GetType()))
                {
                    Inject(component);
                }
            }
        }

        #endregion

        #region Injection Methods

        public void Inject(object target)
        {
            if (target == null) return;

            Type targetType = target is Type ? (Type)target : target.GetType();

            Type currentType = targetType;
            var injectableMembers = new List<(MemberInfo member, InjectAttribute attr, int priority)>();
            
            while (currentType != null && currentType != typeof(object))
            {
                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                if (target is Type)
                {
                    bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
                }
                
                FieldInfo[] fields = currentType.GetFields(bindingFlags);
                
                foreach (FieldInfo field in fields)
                {
                    var injectAttr = field.GetCustomAttribute<InjectAttribute>();
                    var optionalAttr = field.GetCustomAttribute<OptionalInjectAttribute>();
                    var conditionalAttr = field.GetCustomAttribute<ConditionalInjectAttribute>();
                    
                    if (injectAttr != null || optionalAttr != null)
                    {
                        if (conditionalAttr != null && !EvaluateCondition(target, conditionalAttr))
                        {
                            continue;
                        }

                        var priorityAttr = field.GetCustomAttribute<InjectPriorityAttribute>();
                        int priority = priorityAttr?.Priority ?? 0;
                        
                        injectableMembers.Add((field, injectAttr ?? new InjectAttribute { Required = false }, priority));
                    }
                }
                
                PropertyInfo[] properties = currentType.GetProperties(bindingFlags);
                
                foreach (PropertyInfo property in properties)
                {
                    if (!property.CanWrite) continue;

                    var injectAttr = property.GetCustomAttribute<InjectAttribute>();
                    var optionalAttr = property.GetCustomAttribute<OptionalInjectAttribute>();
                    var conditionalAttr = property.GetCustomAttribute<ConditionalInjectAttribute>();
                    
                    if (injectAttr != null || optionalAttr != null)
                    {
                        if (conditionalAttr != null && !EvaluateCondition(target, conditionalAttr))
                        {
                            continue;
                        }

                        var priorityAttr = property.GetCustomAttribute<InjectPriorityAttribute>();
                        int priority = priorityAttr?.Priority ?? 0;
                        
                        injectableMembers.Add((property, injectAttr ?? new InjectAttribute { Required = false }, priority));
                    }
                }
                
                currentType = currentType.BaseType;
            }

            // Sort by priority (highest first)
            injectableMembers.Sort((a, b) => b.priority.CompareTo(a.priority));

            foreach (var (member, attr, _) in injectableMembers)
            {
                try
                {
                    InjectMember(target, member, attr);
                }
                catch (Exception e)
                {
                    if (attr.Required)
                    {
                        Debug.LogError($"[DIContainer] Required injection failed for {member.Name} in {targetType.Name}: {e.Message}");
                        throw;
                    }
                    else
                    {
                        Debug.LogWarning($"[DIContainer] Optional injection failed for {member.Name} in {targetType.Name}: {e.Message}");
                    }
                }
            }
            
            if (!(target is Type))
            {
                CallPostInjectMethods(target);
            }
        }

        private void InjectMember(object target, MemberInfo member, InjectAttribute attr)
        {
            Type memberType;
            object currentValue;
            bool isStatic = target is Type;
            
            if (member is FieldInfo field)
            {
                memberType = field.FieldType;
                currentValue = isStatic ? field.GetValue(null) : field.GetValue(target);
            }
            else if (member is PropertyInfo property)
            {
                memberType = property.PropertyType;
                currentValue = isStatic ? property.GetValue(null) : property.GetValue(target);
            }
            else
            {
                return;
            }

            if (currentValue != null)
            {
                return; // Skip if already has a value
            }

            object dependency;
            
            if (!string.IsNullOrEmpty(attr.Name))
            {
                if (!TryGetService(memberType, attr.Name, out dependency))
                {
                    var optionalAttr = member.GetCustomAttribute<OptionalInjectAttribute>();
                    if (optionalAttr != null)
                    {
                        dependency = optionalAttr.DefaultValue;
                    }
                    else if (attr.Required)
                    {
                        throw new InvalidOperationException($"Named service '{attr.Name}' of type {memberType.Name} is not registered.");
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                if (!TryGetService(memberType, out dependency))
                {
                    var optionalAttr = member.GetCustomAttribute<OptionalInjectAttribute>();
                    if (optionalAttr != null)
                    {
                        dependency = optionalAttr.DefaultValue;
                    }
                    else if (attr.Required)
                    {
                        throw new InvalidOperationException($"Service of type {memberType.Name} is not registered.");
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (member is FieldInfo fieldInfo)
            {
                if (isStatic)
                    fieldInfo.SetValue(null, dependency);
                else
                    fieldInfo.SetValue(target, dependency);
            }
            else if (member is PropertyInfo propertyInfo)
            {
                if (isStatic)
                    propertyInfo.SetValue(null, dependency);
                else
                    propertyInfo.SetValue(target, dependency);
            }
        }

        private bool EvaluateCondition(object target, ConditionalInjectAttribute attr)
        {
            var type = target.GetType();
            var conditionMember = type.GetField(attr.ConditionMember, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) as MemberInfo
                                ?? type.GetProperty(attr.ConditionMember, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (conditionMember == null)
            {
                Debug.LogWarning($"[DIContainer] Condition member '{attr.ConditionMember}' not found on type {type.Name}");
                return false;
            }

            object value = null;
            if (conditionMember is FieldInfo field)
            {
                value = field.GetValue(target);
            }
            else if (conditionMember is PropertyInfo property)
            {
                value = property.GetValue(target);
            }

            if (value is bool boolValue)
            {
                return boolValue == attr.ExpectedValue;
            }

            Debug.LogWarning($"[DIContainer] Condition member '{attr.ConditionMember}' is not a boolean on type {type.Name}");
            return false;
        }

        private bool TryGetService(Type type, out object service)
        {
            try
            {
                service = GetService(type);
                return true;
            }
            catch
            {
                service = null;
                return false;
            }
        }

        private bool TryGetService(Type type, string name, out object service)
        {
            try
            {
                var key = $"{type.FullName}#{name}";
                return namedSingletons.TryGetValue(key, out service);
            }
            catch
            {
                service = null;
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private bool HasInjectableMembers(Type type)
        {
            Type currentType = type;
            
            while (currentType != null && currentType != typeof(object))
            {
                var fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (fields.Any(field => field.GetCustomAttribute<InjectAttribute>() != null || 
                                      field.GetCustomAttribute<OptionalInjectAttribute>() != null))
                    return true;

                var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (properties.Any(property => property.GetCustomAttribute<InjectAttribute>() != null || 
                                             property.GetCustomAttribute<OptionalInjectAttribute>() != null))
                    return true;
                
                currentType = currentType.BaseType;
            }

            return false;
        }

        private bool TryResolveImplicitSingleton(Type type, out object service)
        {
            service = null;

            if (type.IsInterface || type.IsAbstract || !HasParameterlessConstructor(type))
            {
                return false;
            }

            try
            {
                service = Activator.CreateInstance(type);
                Inject(service);
                
                singletonInstances[type] = service;
                lifetimes[type] = LifetimeType.Singleton;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool HasParameterlessConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        private void CallPostInjectMethods(object target)
        {
            if (target == null) return;

            Type targetType = target.GetType();
            var postInjectMethods = new List<(MethodInfo method, int order)>();

            Type currentType = targetType;
            while (currentType != null && currentType != typeof(object))
            {
                MethodInfo[] methods = currentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                
                foreach (MethodInfo method in methods)
                {
                    var postInjectAttr = method.GetCustomAttribute<PostInjectAttribute>();
                    if (postInjectAttr != null)
                    {
                        if (method.GetParameters().Length == 0)
                        {
                            postInjectMethods.Add((method, postInjectAttr.Order));
                        }
                        else
                        {
                            Debug.LogWarning($"[DIContainer] PostInject method {method.Name} on {targetType.Name} has parameters and will be skipped. PostInject methods must be parameterless.");
                        }
                    }
                }
                
                currentType = currentType.BaseType;
            }

            // Sort by order (lowest first)
            postInjectMethods.Sort((a, b) => a.order.CompareTo(b.order));

            foreach (var (method, _) in postInjectMethods)
            {
                try
                {
                    method.Invoke(target, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DIContainer] Failed to call PostInject method {method.Name} on {targetType.Name}: {e.Message}");
                }
            }
        }

        public bool IsRegistered(Type type)
        {
            return singletonInstances.ContainsKey(type) || factories.ContainsKey(type);
        }

        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        public bool IsRegistered<T>(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            var type = typeof(T);
            var key = $"{type.FullName}#{name}";
            return namedSingletons.ContainsKey(key);
        }

        public void ValidateRegistrations()
        {
            var errors = new List<string>();
            
            foreach (var kvp in factories)
            {
                try
                {
                    var service = kvp.Value();
                    if (service == null)
                    {
                        errors.Add($"Factory for {kvp.Key.Name} returned null");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"Factory for {kvp.Key.Name} threw exception: {e.Message}");
                }
            }
            
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Registration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        public void Clear()
        {
            foreach (var singletonInstance in singletonInstances.Values)
            {
                if (singletonInstance is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error disposing singleton: {e.Message}");
                    }
                }
            }

            foreach (var gameObject in managedGameObjects)
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            singletonInstances.Clear();
            factories.Clear();
            lifetimes.Clear();
            namedBindings.Clear();
            namedSingletons.Clear();
            interfaceImplementations.Clear();
            currentlyResolving.Clear();
            managedGameObjects.Clear();
        }

        // Additional helper methods for better functionality
        public Type[] GetRegisteredTypes()
        {
            var types = new HashSet<Type>();
            foreach (var type in singletonInstances.Keys)
                types.Add(type);
            foreach (var type in factories.Keys)
                types.Add(type);
            return types.ToArray();
        }

        public void Unregister<T>()
        {
            var type = typeof(T);
            
            if (singletonInstances.TryGetValue(type, out var instance))
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                singletonInstances.Remove(type);
            }
            
            factories.Remove(type);
            lifetimes.Remove(type);
            interfaceImplementations.Remove(type);
        }

        public bool HasCircularDependency(Type rootType)
        {
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();
            
            return HasCircularDependencyRecursive(rootType, visited, recursionStack);
        }

        private bool HasCircularDependencyRecursive(Type type, HashSet<Type> visited, HashSet<Type> recursionStack)
        {
            if (recursionStack.Contains(type))
                return true;
                
            if (!visited.Add(type))
                return false;

            recursionStack.Add(type);
            
            var dependencies = GetTypeDependencies(type);
            
            foreach (var dependency in dependencies)
            {
                if (HasCircularDependencyRecursive(dependency, visited, recursionStack))
                    return true;
            }
            
            recursionStack.Remove(type);
            return false;
        }

        private Type[] GetTypeDependencies(Type type)
        {
            var dependencies = new List<Type>();
            
            while (type != null && type != typeof(object))
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InjectAttribute>() != null || 
                        field.GetCustomAttribute<OptionalInjectAttribute>() != null)
                    {
                        dependencies.Add(field.FieldType);
                    }
                }
                
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                
                foreach (var property in properties)
                {
                    if (property.GetCustomAttribute<InjectAttribute>() != null || 
                        property.GetCustomAttribute<OptionalInjectAttribute>() != null)
                    {
                        dependencies.Add(property.PropertyType);
                    }
                }
                
                type = type.BaseType;
            }
            
            return dependencies.ToArray();
        }

        #endregion
    }
}