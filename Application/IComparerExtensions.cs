using System.Collections.Generic;

namespace Application.IComparerExtensions
{
    public static class IComparerExtensions
    {
        public static IEqualityComparer<T> ToEqualityComparer<T>(this IComparer<T> comparer)
        {
            return new IComparerEqualityComparer<T>(comparer);
        }

        public class IComparerEqualityComparer<T> : IEqualityComparer<T>
        {
            private IComparer<T> _comparer;

            public IComparerEqualityComparer(IComparer<T> comparer)
            {
                _comparer = comparer;
            }

            public bool Equals(T x, T y)
            {
                return _comparer.Compare(x, y) == 0;
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}