using Application.IComparerExtensions;
using Application.IListExtensions;
using Application.Linq;
using Application.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.LinkedListExtensions
{
    public static class LinkedListExtensions
    {
        public static LinkedListNode<T> FirstLinkedListNode<T>(this LinkedList<T> source, Func<T, bool> predicate)
        {
            var node = source.First;
            while (node != null)
            {
                if (predicate(node.Value))
                    return node;
                node = node.Next;
            }

            return null;
        }
    }
}