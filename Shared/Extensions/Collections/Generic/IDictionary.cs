using System.Linq;

namespace System.Collections.Generic
{
    public static class IDictionaryExtensions
    {
        public static Dictionary<string, object> Flatten(this IDictionary<string, object> me)
        {
            return me.SelectMany(kvp =>
            {
                if (kvp.Value is IDictionary<string, object>)
                    return (kvp.Value as IDictionary<string, object>).Flatten().Select(subKvp => new KeyValuePair<string, object>(kvp.Key + "." + subKvp.Key, subKvp.Value));
                else
                    return new [] { kvp };
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        public static Dictionary<string, object> Expand(this IDictionary<string, object> me)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in me)
                result.Set(kvp.Key, kvp.Value);
            return result;
        }
        public static void Set(this IDictionary<string, object> me, string path, object value)
        {
            if (path.Contains("."))
            {
                string key = path.Remove(path.IndexOf("."));
                if (!me.ContainsKey(key) || !(me[key] is IDictionary<string, object>))
                    me[key] = new Dictionary<string, object>();
                (me[key] as IDictionary<string, object>).Set(path.Substring(path.IndexOf(".") + 1), value);
            }
            else
            {
                if (me.ContainsKey(path))
                    me[path] = value;
                else
                    me.Add(path, value);
            }
        }

        public static TKey IndexOf<TKey, TValue>(this IDictionary<TKey, TValue> me, TValue value)
        {
            foreach (var pair in me)
            {
                if (pair.Value.Equals(value))
                    return pair.Key;
            }

            return default(TKey);
        }
    }
}