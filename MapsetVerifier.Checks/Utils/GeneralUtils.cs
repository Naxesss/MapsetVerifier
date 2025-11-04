using System;
using System.Collections.Generic;

namespace MapsetVerifier.Checks.Utils;

public static class GeneralUtils
{
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
    public static double TakeLowerAbsValue(double first, double second)
    {
        return Math.Abs(first) < Math.Abs(second) ? first : second;
    }
}