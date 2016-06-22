using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Application.Linq
{
    public static class Grouping
    {
        public static IGrouping<TKey, TElement> Create<TKey, TElement>(TKey key, IEnumerable<TElement> elements)
        {
            return new Grouping<TKey, TElement>(key, elements);
        }
    }

    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly List<TElement> _elements;

        public Grouping(TKey key, IEnumerable<TElement> elements)
        {
            Key = key;
            _elements = elements.ToList();
        }

        public TKey Key { get; set; }

        public IEnumerator<TElement> GetEnumerator()
        {
            return this._elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}