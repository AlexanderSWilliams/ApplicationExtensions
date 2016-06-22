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

        /* Very efficent if the amont << range. Terrible if amount == range. */

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