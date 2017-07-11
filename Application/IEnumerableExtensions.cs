using Application.IComparerExtensions;
using Application.IListExtensions;
using Application.Linq;
using Application.Types;
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

        /// <summary>
        /// Breaks the list into a list of consecutive sublists.  When the result of togglePartitionPredicate changes from its previous evaluation,
        /// it ends the existing sublist and begins a new one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="togglePartitionPredicate"></param>
        /// <returns></returns>
        public static IEnumerable<List<T>> ChunkifyToList<T>(this IEnumerable<T> source, Func<IList<T>, T, bool> togglePartitionPredicate)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    yield break;

                var result = new List<T>();
                var Toggle = togglePartitionPredicate(result, enumerator.Current);
                result.Add(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    if (Toggle == togglePartitionPredicate(result, enumerator.Current))
                        result.Add(enumerator.Current);
                    else
                    {
                        yield return result;
                        result = new List<T>();
                        Toggle = togglePartitionPredicate(result, enumerator.Current);
                        result.Add(enumerator.Current);
                    }
                }
                yield return result;
            }
        }

        public static IEnumerable<IEnumerable<T>> CombineWithoutRepetition<T>(this IEnumerable<T> source, int take = -1)
        {
            if (take == -1)
                return CombineWithoutRepetition(source, source.Count()).Distinct(new IEnumerableComparer<T>());
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

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            var knownKeys = new HashSet<TKey>(comparer ?? EqualityComparer<TKey>.Default);
            return source.Where(element => knownKeys.Add(keySelector(element)));
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

        public static bool FirstOccurencesAreSorted<T>(this IEnumerable<T> source, Comparer<T> comparer = null)
        {
            comparer = comparer ?? Comparer<T>.Default;
            var Occurences = new HashSet<T>(comparer.ToEqualityComparer());
            foreach (var elem in source)
            {
                if (Occurences.Add(elem) && Occurences.Any(y => comparer.Compare(elem, y) == -1))
                    return false;
            }

            return true;
        }

        public static T? FirstOrNull<T>(this IEnumerable<T> source) where T : struct
        {
            return source.Any() ? source.First() : default(T?);
        }

        public static IEnumerable<U> FullJoin<S, T, K, U>(this IEnumerable<S> outer, IEnumerable<T> inner, Func<S, K> outerKeySelector, Func<T, K> innerKeySelector, Func<S, IEnumerable<T>, U> resultSelector)
        {
            return outer.FullJoin(inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<K>.Default);
        }

        public static IEnumerable<U> FullJoin<S, T, K, U>(this IEnumerable<S> outer, IEnumerable<T> inner, Func<S, K> outerKeySelector, Func<T, K> innerKeySelector, Func<S, IEnumerable<T>, U> resultSelector, IEqualityComparer<K> comparer, S defaultValue = default(S))
        {
            var InnerLookup = inner.ToLookup(innerKeySelector, comparer);
            foreach (var outerElement in outer)
            {
                var Key = outerKeySelector(outerElement);
                yield return resultSelector(outerElement, InnerLookup[Key]);
            }

            var OuterLookup = outer.ToLookup(outerKeySelector, comparer);
            foreach (var innerElement in inner)
            {
                var Key = innerKeySelector(innerElement);
                var Values = OuterLookup[Key];
                if (!Values.Any())
                    yield return resultSelector(defaultValue, new[] { innerElement });
            }
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

        public static IEnumerable<T> Interleave<T>(this IEnumerable<IEnumerable<T>> source)
        {
            var Enumerators = source.Select(x => x.GetEnumerator()).ToArray();
            var EndNotReached = true;

            while (EndNotReached)
            {
                var IntermediateResult = new List<T>();
                foreach (var enumerator in Enumerators)
                {
                    if (!enumerator.MoveNext())
                    {
                        EndNotReached = false;
                        break;
                    }
                    IntermediateResult.Add(enumerator.Current);
                }
                if (EndNotReached)
                {
                    foreach (var elem in IntermediateResult)
                    {
                        yield return elem;
                    }
                }
                else
                    yield break;
            }
        }

        public static IEnumerable<T> InterleaveWithDefaults<T>(this IEnumerable<IEnumerable<T>> source, T defaultValue = default(T))
        {
            var Enumerators = source.Select(x => x.GetEnumerator()).ToArray();
            var EndNotReached = true;

            while (EndNotReached)
            {
                EndNotReached = false;
                var IntermediateResult = new List<T>();
                for (var i = 0; i < Enumerators.Length; i++)
                {
                    var enumerator = Enumerators[i];

                    if (enumerator == null)
                        IntermediateResult.Add(defaultValue);
                    else if (enumerator.MoveNext())
                    {
                        IntermediateResult.Add(enumerator.Current);
                        EndNotReached = true;
                    }
                    else
                    {
                        IntermediateResult.Add(defaultValue);
                        Enumerators[i] = null;
                    }
                }
                if (EndNotReached)
                {
                    foreach (var elem in IntermediateResult)
                    {
                        yield return elem;
                    }
                }
                else
                    yield break;
            }
        }

        public static IEnumerable<T> Interpose<T>(this IEnumerable<T> source, T seperator)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    yield break;

                yield return enumerator.Current;
                while (enumerator.MoveNext())
                {
                    yield return seperator;
                    yield return enumerator.Current;
                }
            }
        }

        public static HashSet<T> Intersect<T>(this IEnumerable<IEnumerable<T>> source, IEqualityComparer<T> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<T>.Default;

            return source.Aggregate(x => new HashSet<T>(x, comparer), (result, x) =>
            {
                var Result = new HashSet<T>(comparer);
                foreach (var elem in x)
                {
                    if (result.Contains(elem))
                        Result.Add(elem);
                }
                return Result;
            });
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static IEnumerable<U> LeftJoin<S, T, K, U>(this IEnumerable<S> outer, IEnumerable<T> inner, Func<S, K> outerKeySelector, Func<T, K> innerKeySelector, Func<S, T, U> resultSelector)
        {
            return outer.LeftJoin(inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<K>.Default);
        }

        public static IEnumerable<U> LeftJoin<S, T, K, U>(this IEnumerable<S> outer, IEnumerable<T> inner, Func<S, K> outerKeySelector, Func<T, K> innerKeySelector, Func<S, T, U> resultSelector, IEqualityComparer<K> comparer)
        {
            var lookup = inner.ToLookup(innerKeySelector, comparer);
            foreach (var outerElement in outer)
            {
                var Key = outerKeySelector(outerElement);
                var Values = lookup[Key];
                foreach (var Value in Values)
                {
                    yield return resultSelector(outerElement, Value);
                }
            }
        }

        public static List<Dictionary<string, string>> ListOfListOfStringToDictionary(this IEnumerable<IList<string>> listOfListOfStrings)
        {
            var entityList = new List<Dictionary<string, string>>();
            if (listOfListOfStrings == null || !listOfListOfStrings.Any())
                return entityList;
            var Keys = listOfListOfStrings.First().TakeWhile(x => !string.IsNullOrWhiteSpace(x)).ToList();
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

        public static IGrouping<TKey, TSource> MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            comparer = comparer ?? Comparer<TKey>.Default;
            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                    return null;

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
                return Grouping.Create(MaximumKey, MaximumElements);
            }
        }

        public static IEnumerable<T> MergeBy<T, Key>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, Key> selector, Comparer<Key> comparer = null)
        {
            comparer = comparer ?? Comparer<Key>.Default;

            using (var firstEnumerator = first.GetEnumerator())
            using (var secondEnumerator = second.GetEnumerator())
            {
                var FirstOngoing = firstEnumerator.MoveNext();
                var SecondOngoing = secondEnumerator.MoveNext();

                while (FirstOngoing && SecondOngoing)
                {
                    var A = firstEnumerator.Current;
                    var B = secondEnumerator.Current;
                    var a = selector(A);
                    var b = selector(B);

                    while (comparer.Compare(a, b) != 1)
                    {
                        yield return A;

                        if (firstEnumerator.MoveNext())
                        {
                            A = firstEnumerator.Current;
                            a = selector(A);
                        }
                        else
                        {
                            FirstOngoing = false;
                            break;
                        }
                    }

                    do
                    {
                        yield return B;

                        if (secondEnumerator.MoveNext())
                        {
                            B = secondEnumerator.Current;
                            b = selector(B);
                        }
                        else
                        {
                            SecondOngoing = false;
                            break;
                        }
                    } while (comparer.Compare(a, b) == 1);
                }

                if (FirstOngoing)
                {
                    do
                    {
                        yield return firstEnumerator.Current;
                    } while (firstEnumerator.MoveNext());
                }

                if (SecondOngoing)
                {
                    do
                    {
                        yield return secondEnumerator.Current;
                    } while (secondEnumerator.MoveNext());
                }
            }
        }

        public static IGrouping<TKey, TSource> MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            comparer = comparer ?? Comparer<TKey>.Default;
            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                    return null;

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
                return Grouping.Create(MinimumKey, MinimumElements);
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
                return PermuteWithoutRepetition(source, source.Count()).Distinct(new IEnumerableComparer<T>());
            if (take == 0)
                return new[] { new T[0] };

            return source.SelectMany((e, i) =>
            {
                var SourceList = source.ToList();
                var Index = SourceList.IndexOf(e);
                if (Index != -1)
                    SourceList.RemoveAt(Index);

                return SourceList.PermuteWithoutRepetition(take - 1);
            }, (e, p) => new[] { e }.Concat(p));
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

        public static Dictionary<K, List<S>> ToDictionaryList<T, K, S>(this IEnumerable<T> source, Func<T, K> keySelector, Func<T, S> elementSelector)
        {
            return source.ToLookup(keySelector, elementSelector).ToDictionary(x => x.Key, x => x.ToList());
        }

        public static Dictionary<K, List<T>> ToDictionaryList<T, K>(this IEnumerable<T> source, Func<T, K> keySelector)
        {
            return source.ToLookup(keySelector).ToDictionary(x => x.Key, x => x.ToList());
        }

        public static Dictionary<K, List<S>> ToDictionaryList<T, K, S>(this IEnumerable<T> source, Func<T, K> keySelector, Func<T, S> elementSelector, IEqualityComparer<K> comparer)
        {
            return source.ToLookup(keySelector, elementSelector, comparer).ToDictionary(x => x.Key, x => x.ToList());
        }

        public static Dictionary<K, List<T>> ToDictionaryList<T, K>(this IEnumerable<T> source, Func<T, K> keySelector, IEqualityComparer<K> comparer)
        {
            return source.ToLookup(keySelector, comparer).ToDictionary(x => x.Key, x => x.ToList());
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer = null)
        {
            return new HashSet<TSource>(source, comparer ?? EqualityComparer<TSource>.Default);
        }

        public static IEnumerable<IEnumerable<S>> Transpose<S>(this IEnumerable<IEnumerable<S>> source)
        {
            var Enumerators = source.Select(x => x.GetEnumerator()).ToList();

            try
            {
                return Transpose(Enumerators);
            }
            finally
            {
                foreach (var enumerator in Enumerators)
                    enumerator.Dispose();
            }
        }

        public static IEnumerable<List<S>> TransposeWithDefaults<S>(this IEnumerable<IEnumerable<S>> source, S defaultValue)
        {
            var Enumerators = source.Select(x => x.GetEnumerator()).ToList();

            try
            {
                return TransposeWithDefaults(Enumerators, defaultValue);
            }
            finally
            {
                foreach (var enumerator in Enumerators)
                    enumerator.Dispose();
            }
        }

        public static List<T> Union<T>(this IEnumerable<IEnumerable<T>> source)
        {
            var result = new List<T>();
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    using (var innerEnumerator = enumerator.Current.GetEnumerator())
                    {
                        while (innerEnumerator.MoveNext())
                        {
                            result.Add(innerEnumerator.Current);
                        }
                    }
                }
            }

            return result;
        }

        public static IEnumerable<T> XOR<T>(this IEnumerable<IEnumerable<T>> source, IEqualityComparer<T> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<T>.Default;

            return source.SelectMany(x => x).GroupBy(x => x, comparer).Where(x => x.Count() % 2 == 1).Select(x => x.Key);
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
                            EnumeratorList.RemoveRange(EnumeratorList.Count - seperatorLength, seperatorLength);
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

        private static IEnumerable<IEnumerable<S>> Transpose<S>(IList<IEnumerator<S>> enumerators)
        {
            while (enumerators.All(x => x.MoveNext()))
                yield return enumerators.Select(x => x.Current);
        }

        private static IEnumerable<List<S>> TransposeWithDefaults<S>(IList<IEnumerator<S>> enumerators, S defaultValue)
        {
            while (true)
            {
                var NotFinished = false;

                var Row = new List<S>();
                foreach (var enumerator in enumerators)
                {
                    var EnumeratorNotFinished = enumerator.MoveNext();
                    Row.Add(EnumeratorNotFinished ? enumerator.Current : defaultValue);
                    NotFinished = NotFinished || EnumeratorNotFinished;
                }

                if (NotFinished)
                    yield return Row;
                else
                    break;
            }
        }
    }
}