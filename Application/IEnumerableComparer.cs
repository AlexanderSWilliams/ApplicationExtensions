using System;
using System.Collections.Generic;

namespace Application.Types
{
    public class IEnumerableComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        private readonly EqualityComparer<T> _comparer;

        public IEnumerableComparer()
        {
            _comparer = EqualityComparer<T>.Default;
        }

        public IEnumerableComparer(EqualityComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (x == null || y == null)
                return false;
            if (Object.ReferenceEquals(x, y))
                return true;

            using (var xEnumerator = x.GetEnumerator())
            using (var yEnumerator = y.GetEnumerator())
            {
                while (xEnumerator.MoveNext())
                {
                    if (!yEnumerator.MoveNext())
                        return false;
                    if (!_comparer.Equals(xEnumerator.Current, yEnumerator.Current))
                        return false;
                }

                if (yEnumerator.MoveNext())
                    return false;
            }

            return true;
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            if (obj == null)
                return 0;

            var Result = 0;
            var i = 0;
            foreach (var elem in obj)
            {
                Result = Result ^ (i + elem.GetHashCode());
                i++;
            }

            return Result;
        }
    }

    public class ISortedComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        private readonly EqualityComparer<T> _comparer;

        public ISortedComparer()
        {
            _comparer = EqualityComparer<T>.Default;
        }

        public ISortedComparer(EqualityComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            if (x == null || y == null)
                return false;
            if (Object.ReferenceEquals(x, y))
                return true;

            using (var xEnumerator = x.GetEnumerator())
            using (var yEnumerator = y.GetEnumerator())
            {
                while (xEnumerator.MoveNext())
                {
                    if (!yEnumerator.MoveNext())
                        return false;
                    if (!_comparer.Equals(xEnumerator.Current, yEnumerator.Current))
                        return false;
                }

                if (yEnumerator.MoveNext())
                    return false;
            }

            return true;
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            if (obj == null)
                return 0;

            var Result = 0;
            foreach (var elem in obj)
            {
                Result = Result ^ elem.GetHashCode();
            }

            return Result;
        }
    }
}