using Application.PropertyFieldInfoExtensions;
using Application.TypeExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.IDictionaryExtensions
{
    public static class IDictionaryExtensions
    {
        public static void AddOrIgnore<S, T>(this IDictionary<S, T> destination, S key, T value)
        {
            if (!destination.ContainsKey(key))
                destination.Add(key, value);
        }

        public static void AddOrOverWrite<S, T>(this IDictionary<S, T> destination, S key, T value)
        {
            if (destination.ContainsKey(key))
                destination[key] = value;
            else
                destination.Add(key, value);
        }

        public static S GetValueOrDefault<T, S>(this IDictionary<T, S> dict, T key) where S : new()
        {
            S value;
            if (key != null && dict.TryGetValue(key, out value))
                return value;
            return new S();
        }

        public static S GetValueOrDefault<T, S>(this IDictionary<T, S> dict, T key, S defaultValue) where S : class
        {
            S value;
            if (key != null && dict.TryGetValue(key, out value))
                return value;
            return defaultValue;
        }

        public static S? GetValueOrNull<T, S>(this IDictionary<T, S> dict, T key) where S : struct
        {
            S value;
            if (key != null && dict.TryGetValue(key, out value))
                return value;
            return null;
        }

        public static S GetValueOrThrow<T, S>(this IDictionary<T, S> dict, T key)
        {
            S value;
            if (key != null && dict.TryGetValue(key, out value))
                return value;

            throw new KeyNotFoundException("The key \"" + key + "\" was not present.");
        }

        public static void InjectFromObjectDictionary(this IDictionary<string, object> sourceDictionary, object target)
        {
            var CommonProps = target.GetType().DBPrimativeAssignablePropsAndFields().GroupJoin(sourceDictionary, o => o.Name(), i => i.Key, (o, i) => new
            {
                targetProp = o,
                sourceProp = i.FirstOrDefault()
            })
            .Where(x => x.sourceProp.Key != null);

            foreach (var prop in CommonProps)
            {
                var Value = prop.sourceProp.Value;
                prop.targetProp.InjectValueFrom(target, Value?.GetType(), Value);
            }
        }

        public static void InjectFromStringDictionary(this IDictionary<string, string> sourceDictionary, object target)
        {
            var CommonProps = target.GetType().DBPrimativeAssignablePropsAndFields().GroupJoin(sourceDictionary, o => o.Name(), i => i.Key,
                 (o, i) => new
                 {
                     targetProp = o,
                     sourceProp = i.FirstOrDefault()
                 })
                .Where(x => x.sourceProp.Key != null);

            foreach (var prop in CommonProps)
            {
                var Value = prop.sourceProp.Value;
                var TargetType = prop.targetProp.Type();
                var ConversionType = Nullable.GetUnderlyingType(TargetType) ?? TargetType;
                object obj;

                try
                {
                    obj = string.IsNullOrEmpty(Value) ? (ConversionType != typeof(string) ? null : Value) : ConversionType.ParseValue(Value);
                }
                catch (FormatException ex)
                {
                    throw new FormatException("ChangeType failed in InjectFromDictionary to convert property " + prop.sourceProp.Key + " with value " + Value + " to type " + ConversionType.Name);
                }
                prop.targetProp.SetValue(target, obj);
            }
        }

        public static Dictionary<K, List<V>> MergeIntoLists<K, V>(this IEnumerable<IDictionary<K, V>> source)
        {
            var Result = new Dictionary<K, List<V>>();
            foreach (var dict in source)
            {
                foreach (var pair in dict)
                {
                    List<V> value;
                    if (Result.TryGetValue(pair.Key, out value))
                        value.Add(pair.Value);
                    else
                        Result.Add(pair.Key, new List<V> { pair.Value });
                }
            }
            return Result;
        }

        public static Dictionary<K, V> MergeOrIgnore<K, V>(this IEnumerable<IDictionary<K, V>> source)
        {
            var Result = new Dictionary<K, V>();
            foreach (var dict in source)
            {
                foreach (var pair in dict)
                {
                    V value;
                    if (!Result.TryGetValue(pair.Key, out value))
                        Result.Add(pair.Key, pair.Value);
                }
            }
            return Result;
        }

        public static void MergeOrIgnore<S, T>(this IDictionary<S, T> destination, IDictionary<S, T> from)
        {
            foreach (var pair in from)
            {
                if (!destination.ContainsKey(pair.Key))
                    destination.Add(pair.Key, pair.Value);
            }
        }

        public static Dictionary<K, V> MergeOrOverWrite<K, V>(this IEnumerable<IDictionary<K, V>> source)
        {
            var Result = new Dictionary<K, V>();
            foreach (var dict in source)
            {
                foreach (var pair in dict)
                {
                    V value;
                    if (Result.TryGetValue(pair.Key, out value))
                        Result[pair.Key] = pair.Value;
                    else
                        Result.Add(pair.Key, pair.Value);
                }
            }
            return Result;
        }

        public static void MergeOrOverWrite<S, T>(this IDictionary<S, T> destination, IDictionary<S, T> from)
        {
            foreach (var pair in from)
            {
                if (destination.ContainsKey(pair.Key))
                    destination[pair.Key] = pair.Value;
                else
                    destination.Add(pair.Key, pair.Value);
            }
        }

        public static Dictionary<K, int> ToFrequency<K, V>(this IDictionary<K, IEnumerable<V>> dict)
        {
            return dict.ToDictionary(x => x.Key, x => x.Value.Count());
        }

        public static List<List<string>> ToListOfListOfStrings<K, V>(this IEnumerable<IDictionary<K, V>> source)
        {
            var result = new List<List<string>>();
            if (!source.Any())
                return result;
            var Keys = source.First().Keys.ToList();

            result.Add(Keys.Select(x => x?.ToString()).ToList());
            foreach (var row in source)
            {
                var rowResult = new List<string>();
                foreach (var key in Keys)
                {
                    rowResult.Add(row[key]?.ToString());
                }
                result.Add(rowResult);
            }

            return result;
        }
    }
}