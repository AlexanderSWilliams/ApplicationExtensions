using Application.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.IEnumerableExtensions
{
    public static class IEnumerableExtensions
    {
        public static TAccumulate Aggregate<TSource, TAccumulate>(
            this IEnumerable<TSource> source,
            Func<TSource, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> func)
        {
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                    return default(TAccumulate);

                var result = seedSelector(iterator.Current);
                while (iterator.MoveNext())
                    result = func(result, iterator.Current);
                return result;
            }
        }

        public static IEnumerable<List<T>> Chunkify<T>(this IEnumerable<T> source, int chunkSize)
        {
            if (chunkSize < 1)
                return Enumerable.Empty<List<T>>();
            return source.Paginate((new[] { 0, chunkSize }).Repeat());
        }

        public static IEnumerable<IEnumerable<T>> CombineWithoutRepetition<T>(this IEnumerable<T> source, int take = -1)
        {
            if (take == -1)
                return CombineWithoutRepetition(source, source.Count());
            if (take == 0)
                return new[] { new T[0] };
            return source.SelectMany((e, i) => source.Skip(i + 1).CombineWithoutRepetition(take - 1), (e, c) => new[] { e }.Concat(c));
        }

        public static IEnumerable<IEnumerable<T>> CombineWithRepetition<T>(this IEnumerable<T> source, int take = -1)
        {
            if (take == -1)
                return CombineWithRepetition(source, source.Count());
            if (take == 0)
                return new[] { new T[0] };
            return source.SelectMany((e, i) => source.Skip(i).CombineWithRepetition(source.Count()), (e, c) => new[] { e }.Concat(c));
        }

        public static List<List<T>> DistinctGroupBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<TKey>.Default;
            var Groups = new List<List<T>>();

            var KeyGroups = new List<HashSet<TKey>>();

            foreach (var item in enumerable)
            {
                var Key = keySelector(item);
                var KeyIndex = KeyGroups.IndexForSortedList(x => !x.Contains(Key));

                if (KeyIndex >= 0)
                {
                    KeyGroups[KeyIndex].Add(Key);
                    Groups[KeyIndex].Add(item);
                }
                else
                {
                    KeyGroups.Add(new HashSet<TKey>(new[] { Key }, comparer));
                    Groups.Add(new List<T> { item });
                }
            }

            return Groups;
        }

        public static List<T> ExceptNonDisctint<T>(this IEnumerable<T> first, HashSet<T> second)
        {
            var result = new List<T>();
            using (var enumerator = first.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    if (!second.Contains(enumerator.Current))
                        result.Add(enumerator.Current);
            }
            return result;
        }

        public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : struct
        {
            return source.Any() ? source.First() : default(T?);
        }

        public static int IndexForSortedList<T>(this List<T> source, Func<T, bool> predicate)
        {
            int min = 0, max = source.Count - 1;
            while (min < max)
            {
                var mid = (max - min) / 2 + min;

                if (predicate(source[mid]))
                    max = mid;
                else
                    min = mid + 1;
            }

            if (max == min && predicate(source[min]))
                return min;
            else
                return -1;
        }

        public static int IndexOf<TSource>(this IEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            int i = 0;

            foreach (TSource element in source)
            {
                if (predicate(element))
                    return i;

                i++;
            }

            return -1;
        }

        public static List<int> IndicesOf<TSource>(this IEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            int i = 0;

            var result = new List<int>();
            foreach (TSource element in source)
            {
                if (predicate(element))
                    result.Add(i);
                i++;
            }

            return result;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static List<Dictionary<string, string>> ListOfListOfStringToDictionary(this IEnumerable<IList<string>> listOfListOfStrings)
        {
            var entityList = new List<Dictionary<string, string>>();
            if (listOfListOfStrings == null || !listOfListOfStrings.Any())
                return entityList;
            var Keys = listOfListOfStrings.First();
            if (Keys == null || !Keys.Any() || Keys.Any(x => x == null))
                return entityList;

            var Indexes = Keys.Select((x, index) => new { Value = x, ColumnIndex = index }).GroupBy(x => x.Value).Where(x => x.Count() > 1)
             .Select(x => x.Select((y, groupIndex) => new { y.ColumnIndex, GroupIndex = groupIndex })).SelectMany(x => x).ToList();

            foreach (var index in Indexes)
            {
                if (index.GroupIndex != 0)
                    Keys[index.ColumnIndex] += index.GroupIndex;
            }

            var ColumnCount = Keys.Count();
            foreach (var row in listOfListOfStrings.Skip(1).ToList())
            {
                if (row.Count != ColumnCount)
                    break;

                var entityRow = new Dictionary<string, string>();
                for (var index = 0; index < ColumnCount; index++)
                {
                    try
                    {
                        entityRow.Add(Keys[index], row[index]);
                    }
                    catch (Exception e)
                    {
                        throw new ApplicationException(Keys[index]);
                        throw;
                    }
                }
                entityList.Add(entityRow);
            }

            return entityList;
        }

        public static IEnumerable<IGrouping<TKey, TSource>> MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            comparer = comparer ?? Comparer<TKey>.Default;
            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                    yield break;

                var MaximumElement = sourceIterator.Current;
                var MaximumKey = selector(MaximumElement);
                var MaximumElements = new List<TSource> { MaximumElement };

                while (sourceIterator.MoveNext())
                {
                    var CurrentElement = sourceIterator.Current;
                    var CurrentKey = selector(CurrentElement);
                    var CompareValue = comparer.Compare(CurrentKey, MaximumKey);
                    if (CompareValue > 0)
                    {
                        MaximumKey = CurrentKey;
                        MaximumElements = new List<TSource> { CurrentElement };
                    }
                    else if (CompareValue == 0)
                    {
                        MaximumElements.Add(CurrentElement);
                    }
                }
                yield return Grouping.Create(MaximumKey, MaximumElements);
            }
        }

        public static IEnumerable<IGrouping<TKey, TSource>> MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            comparer = comparer ?? Comparer<TKey>.Default;
            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                    yield break;

                var MinimumElement = sourceIterator.Current;
                var MinimumKey = selector(MinimumElement);
                var MinimumElements = new List<TSource> { MinimumElement };
                while (sourceIterator.MoveNext())
                {
                    var CurrentElement = sourceIterator.Current;
                    var CurrentKey = selector(CurrentElement);
                    var CompareValue = comparer.Compare(CurrentKey, MinimumKey);
                    if (CompareValue < 0)
                    {
                        MinimumKey = CurrentKey;
                        MinimumElements = new List<TSource> { CurrentElement };
                    }
                    else if (CompareValue == 0)
                    {
                        MinimumElements.Add(CurrentElement);
                    }
                }
                yield return Grouping.Create(MinimumKey, MinimumElements);
            }
        }

        public static IEnumerable<U> OrderedGroupJoin<S, T, U>(this IEnumerable<S> outer, IEnumerable<T> inner, Func<S, T, int> comparator, Func<S, T, U> resultSelector)
        {
            using (var outerEnumerator = outer.GetEnumerator())
            using (var innerEnumerator = inner.GetEnumerator())
            {
                if (!outerEnumerator.MoveNext() || !innerEnumerator.MoveNext())
                    yield break;

                while (true)
                {
                    var comparison = comparator(outerEnumerator.Current, innerEnumerator.Current);
                    if (comparison == 0)
                    {
                        yield return resultSelector(outerEnumerator.Current, innerEnumerator.Current);

                        if (!outerEnumerator.MoveNext() || !innerEnumerator.MoveNext())
                            yield break;
                    }
                    else if (comparison < 0)
                    {
                        if (!outerEnumerator.MoveNext())
                            yield break;
                    }
                    else
                    {
                        if (!innerEnumerator.MoveNext())
                            yield break;
                    }
                }
            }
        }

        public static IEnumerable<List<T>> Paginate<T>(this IEnumerable<T> source, IEnumerable<int> skipTakeLengths)
        {
            using (var iterator = source.GetEnumerator())
            using (var skipTakeiterator = skipTakeLengths.GetEnumerator())
            {
                while (skipTakeiterator.MoveNext())
                {
                    var SkipLength = skipTakeiterator.Current;
                    for (int i = 0; i < SkipLength; i++)
                    {
                        if (!iterator.MoveNext())
                            yield break;
                    }

                    if (!skipTakeiterator.MoveNext())
                        yield break;

                    var result = source.Take(skipTakeiterator.Current, iterator).ToList();

                    if (!result.Any())
                        yield break;
                    yield return result;
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> PermuteWithoutRepetition<T>(this IEnumerable<T> source, int take = -1)
        {
            if (take == -1)
                return PermuteWithoutRepetition(source, source.Count());
            if (take == 0)
                return new[] { new T[0] };
            return source.SelectMany((e, i) => source.Except(new[] { e }).PermuteWithoutRepetition(take - 1), (e, p) => new[] { e }.Concat(p));
        }

        public static IEnumerable<IEnumerable<T>> PermuteWithRepetition<T>(this IEnumerable<T> source, int take = -1)
        {
            if (take == -1)
                return PermuteWithRepetition(source, source.Count());
            if (take == 0)
                return new[] { new T[0] };
            return source.SelectMany((e, i) => source.PermuteWithRepetition(take - 1), (e, p) => new[] { e }.Concat(p));
        }

        public static IEnumerable<int> Range(int start, int count, int increment)
        {
            for (int i = 0; i < count; i++)
            {
                yield return start + i * increment;
            }
        }

        public static IEnumerable<TResult> Repeat<TResult>(this IEnumerable<TResult> source)
        {
            while (true)
            {
                foreach (var item in source)
                    yield return item;
            }
        }

        public static IEnumerable<U> Scan<T, U>(this IEnumerable<T> input, Func<U, T, U> next, Func<T, U> initialState)
        {
            using (var iterator = input.GetEnumerator())
            {
                if (!iterator.MoveNext())
                    yield break;

                var state = initialState(iterator.Current);
                yield return state;

                while (iterator.MoveNext())
                {
                    state = next(state, iterator.Current);
                    yield return state;
                }
            }
        }

        public static IEnumerable<TResult> SelectManySafely<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, IEnumerable<TResult>> selector)
        {
            return source.SelectManySafely((value, index) => selector(value), (o, seq) => seq);
        }

        public static IEnumerable<TResult> SelectManySafely<TSource, TCollection, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
                yield break;

            int index = 0;
            foreach (var item in source)
            {
                var Collection = collectionSelector(item, index++);
                if (Collection == null)
                    continue;
                foreach (var collectionItem in Collection)
                {
                    yield return resultSelector(item, collectionItem);
                }
            }
        }

        public static IEnumerable<List<T>> Split<T>(this IEnumerable<T> enumerable, T[][] seperator, bool removeEmptyEntries = false, IEqualityComparer<T> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<T>.Default;
            if (!seperator.Any())
                yield return enumerable.ToList();

            using (var enumerator = enumerable.GetEnumerator())
                while (enumerator.MoveNext())
                {
                    var result = enumerator.Split(seperator, comparer);

                    if (removeEmptyEntries)
                    {
                        if (result.Any())
                            yield return result;
                    }
                    else
                        yield return result;
                }
        }

        public static List<T> SubList<T>(this List<T> data, int index, int length)
        {
            var result = new List<T>();
            for (int i = 0; i < length; i++)
            {
                result.Add(data[index + i]);
            }
            return result;
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer = null)
        {
            return new HashSet<TSource>(source, comparer ?? EqualityComparer<TSource>.Default);
        }

        private static List<T> Split<T>(this IEnumerator<T> enumerator, T[][] seperator, IEqualityComparer<T> comparer)
        {
            var EnumeratorList = new List<T>();
            var EnumeratorLength = 0;

            do
            {
                do
                {
                    EnumeratorList.Add(enumerator.Current);
                    EnumeratorLength++;

                    for (int seperatorIndex = 0, numberOfSeperators = seperator.Length; seperatorIndex < numberOfSeperators; seperatorIndex++)
                    {
                        var currentSeperator = seperator[seperatorIndex];
                        var currentSeperatorLength = currentSeperator.Length;

                        if (currentSeperatorLength > EnumeratorLength)
                            continue;

                        var MatchesSeperator = true;
                        for (int j = 0, enumeratorOffset = EnumeratorLength - currentSeperatorLength; j < currentSeperatorLength; j++)
                        {
                            if (!comparer.Equals(EnumeratorList[enumeratorOffset + j], currentSeperator[j]))
                            {
                                MatchesSeperator = false;
                                break;
                            }
                        }

                        if (MatchesSeperator)
                        {
                            var seperatorLength = seperator[seperatorIndex].Length;
                            EnumeratorList.RemoveRange(EnumeratorList.Count() - seperatorLength, seperatorLength);
                            return EnumeratorList;
                        }
                    }
                } while (enumerator.MoveNext());
            } while (enumerator.MoveNext());

            return EnumeratorList;
        }

        private static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count, IEnumerator<TSource> iterator)
        {
            for (int i = 0; i < count && iterator.MoveNext(); i++)
            {
                yield return iterator.Current;
            }
        }
    }
}