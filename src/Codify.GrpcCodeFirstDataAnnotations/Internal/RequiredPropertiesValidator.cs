using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Codify.GrpcCodeFirstDataAnnotations.Internal;

/// <summary>
///     Provides shared logic and caching for required properties validation across multiple types.
/// </summary>
/// <remarks>
///     This class centralizes the management of nullability context and validator instance caching to optimize validation performance.
///     It is intended for internal use and is not thread-safe for instance members, but static members are safe for concurrent access.
/// </remarks>
internal static class RequiredPropertiesValidatorShared
{
    [ThreadStatic]
    private static NullabilityInfoContext? _nullabilityContext;

    internal static NullabilityInfoContext NullabilityContext => _nullabilityContext ??= new();

    private static readonly ConcurrentDictionary<(Type, int), object> Cache = new();

    internal static RequiredPropertiesValidator<T> GetOrCreate<T>(int maxDepth) where T : class
    {
        var key = (typeof(T), maxDepth);
        return (RequiredPropertiesValidator<T>)Cache.GetOrAdd(key, _ => new RequiredPropertiesValidator<T>(maxDepth));
    }
}

/// <summary>
///     Validates that all <c>required</c> and non-nullable properties are present on a model.
/// </summary>
internal class RequiredPropertiesValidator<T> where T : class
{

    private readonly List<(Func<T, object?> ParentAccessor, Func<object, object?> PropertyGetter, string DisplayName)> _rules = [];

    private readonly int _maxDepth;

    public RequiredPropertiesValidator(int maxDepth = 20)
    {
        _maxDepth = maxDepth;
        AddRulesRecursive(x => x, typeof(T), "", depth: 0);
    }

    /// <summary>
    ///     Validates the given instance and returns any <see cref="ValidationResult"/> failures
    ///     for required non-nullable properties that are <c>null</c>.
    /// </summary>
    public IList<ValidationResult> Validate(T instance)
    {
        var results = new List<ValidationResult>();

        foreach (var (parentAccessor, propertyGetter, displayName) in _rules)
        {
            var parent = parentAccessor(instance);
            if (parent is null) continue; // parent itself is null — its own rule will already report it

            if (propertyGetter(parent) is null)
                results.Add(new ValidationResult($"'{displayName}' must not be null", [displayName]));
        }

        return results;
    }

    private void AddRulesRecursive(
        Func<T, object?> parentAccessor,
        Type type,
        string prefix,
        int depth)
    {
        if (depth > _maxDepth)
            return;

        // Only process types with required members
        if (!type.IsDefined(typeof(RequiredMemberAttribute), inherit: false))
            return;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Value types are never null
            if (property.PropertyType.IsValueType) continue;

            var prop = property;
            var isNullable = RequiredPropertiesValidatorShared.NullabilityContext.Create(property).WriteState == NullabilityState.Nullable;
            var displayName = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";

            // Register a not-null rule for non-nullable reference properties
            if (!isNullable)
            {
                var accessor = parentAccessor;
                _rules.Add((accessor, parent => prop.GetValue(parent), displayName));
            }

            // Recurse into nested complex types
            if (property.PropertyType != typeof(string) && !property.PropertyType.IsArray)
            {
                AddRulesRecursive(x =>
                {
                    var parent = parentAccessor(x);
                    return parent is null ? null : prop.GetValue(parent);
                }, property.PropertyType, displayName, depth + 1);
            }
        }
    }
}