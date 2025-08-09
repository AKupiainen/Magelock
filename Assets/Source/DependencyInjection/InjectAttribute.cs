using System;

namespace MageLock.DependencyInjection
{
    /// <summary>
    /// Marks a field, property, or method for dependency injection
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class InjectAttribute : Attribute
    {
        /// <summary>
        /// Optional name for named dependency resolution
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whether this injection is required. If false, injection failures will be logged but not cause exceptions
        /// </summary>
        public bool Required { get; set; } = true;
    }

    /// <summary>
    /// Marks a dependency as optional. If not available, will use default value or skip injection
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OptionalInjectAttribute : Attribute
    {
        /// <summary>
        /// Default value to use if dependency cannot be resolved
        /// </summary>
        public object DefaultValue { get; }

        public OptionalInjectAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Specifies injection priority. Higher priority injections happen first
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class InjectPriorityAttribute : Attribute
    {
        /// <summary>
        /// Priority value. Higher values inject first
        /// </summary>
        public int Priority { get; }

        public InjectPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }

    /// <summary>
    /// Marks a parameterless method to be called after all dependency injection is complete
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PostInjectAttribute : Attribute
    {
        /// <summary>
        /// Execution order for multiple PostInject methods. Lower values execute first
        /// </summary>
        public int Order => 0;
    }

    /// <summary>
    /// Marks a dependency as conditional, only injected if a condition is met
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConditionalInjectAttribute : Attribute
    {
        /// <summary>
        /// Name of the boolean field/property that determines if injection should occur
        /// </summary>
        public string ConditionMember { get; }

        /// <summary>
        /// Expected value of the condition member for injection to occur
        /// </summary>
        public bool ExpectedValue => true;

        public ConditionalInjectAttribute(string conditionMember)
        {
            ConditionMember = conditionMember;
        }
    }

    /// <summary>
    /// Provides metadata for dependency injection analysis and debugging
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InjectableTypeAttribute : Attribute
    {
        /// <summary>
        /// Description of this injectable type
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this type requires dependency validation at startup
        /// </summary>
        public bool ValidateAtStartup { get; set; } = false;

        /// <summary>
        /// Expected number of dependencies this type should receive
        /// </summary>
        public int ExpectedDependencyCount { get; set; } = -1;

        public InjectableTypeAttribute() { }
        
        public InjectableTypeAttribute(string description)
        {
            Description = description;
        }
    }
}