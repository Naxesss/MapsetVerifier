using System;
using System.Collections.Generic;

namespace MapsetVerifier.Checks.Utils;

public static class GeneralUtils
{
    // Comparison
    public static bool IsWithin(this double num, double range, double of)
        => num <= of + range && num >= of - range;
    
    // Dictionary
    public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys, TValue value)
    {
        foreach (var key in keys)
            dictionary.Add(key, value);
    }

    public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys, Func<TValue> valueFactory)
    {
        foreach (var key in keys)
            dictionary.Add(key, valueFactory());
    }

    // Collections
    public static T SafeGetIndex<T>(this List<T> collection, int index)
        => index < collection.Count ? collection[index] : default;
    
    public static double TakeLowerAbsValue(double first, double second)
    {
        return Math.Abs(first) < Math.Abs(second) ? first : second;
    }
    
    public static double TakeHigherAbsValue(double first, double second)
    {
        return Math.Abs(first) > Math.Abs(second) ? first : second;
    }
}