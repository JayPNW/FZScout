namespace FZScout;

static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> createValue)
        where TKey : notnull
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = createValue(key);
            dictionary[key] = value;
        }

        return value;
    }

    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue valueToAddIfMissing)
        where TKey : notnull
    {
        return GetOrAdd(dictionary, key, _ => valueToAddIfMissing);
    }
}
