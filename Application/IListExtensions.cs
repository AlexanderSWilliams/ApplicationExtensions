using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.IListExtensions
{
    public static class IListExtensions
    {
        /// <summary>
        /// Returns a list of simulated drawn elements. Notes: The order of the source elements might change.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static List<T> GetRandomElements<T>(this IList<T> source, int amount)
        {
            if (amount < 0.63212055882 * source.Count) /* Optimal ratio is 1 - exp(-1) */
            {
                var RandomNumbers = GetDistinctRandomNumbers(0, source.Count, amount);
                var result = new List<T>();
                foreach (var num in RandomNumbers)
                {
                    result.Add(source[num]);
                }
                return result;
            }
            else /* fall back to Fisher Yates Shuffle */
            {
                Shuffle(source);
                return source.Take(amount).ToList();
            }
        }

        public static int IndexOfNextNewItem<T>(IList<T> list, int index, T item, IComparer<T> comparer)
        {
            var Length = list.Count;
            var NewItem = index < Length ? list[index] : item;

            while (comparer.Compare(NewItem, item) == 0)
            {
                index++;
                if (index < Length)
                    NewItem = list[index];
                else
                    return index;
            }
            return index;
        }

        public static int IndexOfNth<T>(this IList<T> source, T instance, int nth)
        {
            int s = -1;

            for (int i = 0; i < nth; i++)
            {
                s = source.IndexOfNth(instance, s + 1);

                if (s == -1) break;
            }

            return s;
        }

        public static int IndexForFirstTrue<T>(this IList<T> source, Func<T, bool> predicate)
        {
            int min = 0, max = source.Count - 1, diff = max - min;
            while (diff > 0)
            {
                var mid = min + diff / 2;

                if (predicate(source[mid]))
                    max = mid;
                else
                    min = mid + 1;

                diff = max - min;
            }

            return source.Count > 0 && predicate(source[min]) ? min : -1;
        }

        public static int IndexForLastTrue<T>(this IList<T> source, Func<T, bool> predicate)
        {
            int lo = -1, hi = source.Count;

            while (hi - lo > 1)
            {
                int mid = lo + (hi - lo) / 2;
                if (predicate(source[mid]))
                {
                    lo = mid;
                }
                else hi = mid;
            }

            return lo;
        }

        public static T[] SortedExcept<T>(this IList<T> first, IList<T> second)
        {
            return SortedExcept(first, second, Comparer<T>.Default);
        }

        public static T[] SortedExcept<T>(this IList<T> first, IList<T> second, IComparer<T> comparer)
        {
            var FirstLength = first.Count;
            var SecondLength = second.Count;

            if (FirstLength == 0)
                return new T[0];

            if (SecondLength == 0)
                return first as T[] ?? first.ToArray();

            var FirstIndex = 0;
            var SecondIndex = 0;

            var TotalLength = FirstLength + SecondLength;
            var ExceptIndex = 0;
            var Except = new T[TotalLength];

            var A = first[FirstIndex];
            var B = second[SecondIndex];
            while (FirstIndex < FirstLength && SecondIndex < SecondLength)
            {
                while (comparer.Compare(A, B) == -1)
                {
                    Except[ExceptIndex] = A;
                    ExceptIndex++;

                    FirstIndex++;
                    if (FirstIndex < FirstLength)
                    {
                        A = first[FirstIndex];
                    }
                    else
                        break;
                }

                while (comparer.Compare(A, B) == 0)
                {
                    FirstIndex++;
                    if (FirstIndex < FirstLength)
                    {
                        A = first[FirstIndex];
                    }
                    else
                        break;

                    SecondIndex++;
                    if (SecondIndex < SecondLength)
                    {
                        B = second[SecondIndex];
                    }
                    else
                        break;
                }

                while (comparer.Compare(A, B) == 1)
                {
                    SecondIndex++;
                    if (SecondIndex < SecondLength)
                    {
                        B = second[SecondIndex];
                    }
                    else
                        break;
                }
            }

            while (FirstIndex < FirstLength)
            {
                Except[ExceptIndex] = first[FirstIndex];
                ExceptIndex++;
                FirstIndex++;
            }

            Array.Resize(ref Except, ExceptIndex);

            return Except;
        }

        public static T[] SortedIntersection<T>(this IList<T> first, IList<T> second)
        {
            return SortedIntersection(first, second, Comparer<T>.Default);
        }

        public static T[] SortedIntersection<T>(this IList<T> first, IList<T> second, IComparer<T> comparer)
        {
            var FirstLength = first.Count;
            var SecondLength = second.Count;

            if (FirstLength == 0 || SecondLength == 0)
                return new T[0];

            var FirstIndex = 0;
            var SecondIndex = 0;

            var TotalLength = FirstLength + SecondLength;
            var IntersectionIndex = 0;
            var Intersection = new T[TotalLength];

            var A = first[FirstIndex];
            var B = second[SecondIndex];
            while (FirstIndex < FirstLength && SecondIndex < SecondLength)
            {
                while (comparer.Compare(A, B) == -1)
                {
                    FirstIndex++;
                    if (FirstIndex < FirstLength)
                    {
                        A = first[FirstIndex];
                    }
                    else
                        break;
                }

                while (comparer.Compare(A, B) == 0)
                {
                    Intersection[IntersectionIndex] = A;
                    IntersectionIndex++;

                    FirstIndex++;
                    if (FirstIndex < FirstLength)
                    {
                        A = first[FirstIndex];
                    }
                    else
                        break;

                    SecondIndex++;
                    if (SecondIndex < SecondLength)
                    {
                        B = second[SecondIndex];
                    }
                    else
                        break;
                }

                while (comparer.Compare(A, B) == 1)
                {
                    SecondIndex++;
                    if (SecondIndex < SecondLength)
                    {
                        B = second[SecondIndex];
                    }
                    else
                        break;
                }
            }

            Array.Resize(ref Intersection, IntersectionIndex);

            return Intersection;
        }

        public static T[] SortedMerge<T>(this IList<T> first, IList<T> second)
        {
            return SortedMerge(first, second, Comparer<T>.Default);
        }

        public static T[] SortedMerge<T>(this IList<T> first, IList<T> second, IComparer<T> comparer)
        {
            var FirstLength = first.Count;
            var SecondLength = second.Count;
            if (FirstLength == 0)
                return second as T[] ?? second.ToArray();
            if (SecondLength == 0)
                return first as T[] ?? first.ToArray();

            var FirstIndex = 0;
            var SecondIndex = 0;

            var Result = new T[FirstLength + SecondLength];
            var ResultIndex = 0;
            while (FirstIndex < FirstLength && SecondIndex < SecondLength)
            {
                var A = first[FirstIndex];
                var B = second[SecondIndex];

                while (comparer.Compare(A, B) == -1)
                {
                    Result[ResultIndex] = A;
                    ResultIndex++;
                    FirstIndex++;

                    if (FirstIndex < FirstLength)
                        A = first[FirstIndex];
                    else
                        break;
                }

                do
                {
                    Result[ResultIndex] = B;
                    ResultIndex++;
                    SecondIndex++;

                    if (SecondIndex < SecondLength)
                        B = second[SecondIndex];
                    else
                        break;
                } while (comparer.Compare(B, A) == -1);
            }

            while (FirstIndex < FirstLength)
            {
                Result[ResultIndex] = first[FirstIndex];
                ResultIndex++;
                FirstIndex++;
            }

            while (SecondIndex < SecondLength)
            {
                Result[ResultIndex] = second[SecondIndex];
                ResultIndex++;
                SecondIndex++;
            }

            return Result;
        }

        public static T[] SortedMergeDistinctly<T>(this IList<T> first, IList<T> second)
        {
            return SortedMergeDistinctly(first, second, Comparer<T>.Default);
        }

        public static T[] SortedMergeDistinctly<T>(this IList<T> first, IList<T> second, IComparer<T> comparer)
        {
            var FirstLength = first.Count;
            var SecondLength = second.Count;
            if (FirstLength == 0)
                return second as T[] ?? second.ToArray();
            if (SecondLength == 0)
                return first as T[] ?? first.ToArray();

            var FirstIndex = 0;
            var SecondIndex = 0;

            var Result = new T[FirstLength + SecondLength];
            var ResultIndex = 0;
            while (FirstIndex < FirstLength && SecondIndex < SecondLength)
            {
                var A = first[FirstIndex];
                var B = second[SecondIndex];

                while (comparer.Compare(A, B) == -1)
                {
                    Result[ResultIndex] = A;
                    ResultIndex++;
                    FirstIndex++;
                    FirstIndex = IndexOfNextNewItem(first, FirstIndex, A, comparer);

                    if (FirstIndex < FirstLength)
                        A = first[FirstIndex];
                    else
                        break;
                }

                SecondIndex = ResultIndex != 0 ? IndexOfNextNewItem(second, SecondIndex, Result[ResultIndex - 1], comparer) : 0;

                do
                {
                    Result[ResultIndex] = B;
                    ResultIndex++;
                    SecondIndex++;
                    SecondIndex = IndexOfNextNewItem(second, SecondIndex, B, comparer);

                    if (SecondIndex < SecondLength)
                        B = second[SecondIndex];
                    else
                        break;
                } while (comparer.Compare(B, A) == -1);

                FirstIndex = ResultIndex != 0 ? IndexOfNextNewItem(first, FirstIndex, Result[ResultIndex - 1], comparer) : 0;
            }

            var CurrentElement = ResultIndex != 0 ? Result[ResultIndex - 1] : default(T);

            FirstIndex = ResultIndex != 0 ? IndexOfNextNewItem(first, FirstIndex, CurrentElement, comparer) : 0;
            while (FirstIndex < FirstLength)
            {
                var A = first[FirstIndex];
                Result[ResultIndex] = A;
                ResultIndex++;
                FirstIndex++;
                FirstIndex = IndexOfNextNewItem(first, FirstIndex, A, comparer);
            }

            SecondIndex = ResultIndex != 0 ? IndexOfNextNewItem(second, SecondIndex, CurrentElement, comparer) : 0;
            while (SecondIndex < SecondLength)
            {
                var B = second[SecondIndex];
                Result[ResultIndex] = B;
                ResultIndex++;
                SecondIndex++;
                SecondIndex = IndexOfNextNewItem(second, SecondIndex, B, comparer);
            }

            Array.Resize(ref Result, ResultIndex);
            return Result;
        }

        public static List<T> SubList<T>(this IList<T> data, int index, int length)
        {
            var result = new List<T>();
            for (int i = 0; i < length; i++)
            {
                result.Add(data[index + i]);
            }
            return result;
        }

        public static List<T> SubList<T>(this IList<T> data, int index)
        {
            var result = new List<T>();
            for (int i = 0, length = data.Count - index; i < length; i++)
            {
                result.Add(data[index + i]);
            }
            return result;
        }

        /* Very efficent if the amont << range. Terrible if amount == range. */

        private static IEnumerable<int> GetDistinctRandomNumbers(int inclusiveMin, int exclusiveMax, int amount)
        {
            var Amount = System.Math.Min(exclusiveMax - inclusiveMin, amount);
            var result = new HashSet<int>();
            while (result.Count != Amount)
                result.Add(Math.Random.GetRandomNumber(inclusiveMin, exclusiveMax));
            return result;
        }

        private static void Shuffle<T>(IList<T> source)
        {
            int n = source.Count;
            for (int i = 0, stop = n - 2; i <= stop; i++)
            {
                var j = Math.Random.GetRandomNumber(i, n); // GetRandomNumber gives i <= j < n
                var value = source[i];
                source[i] = source[j];
                source[j] = value;
            }
        }
    }
}