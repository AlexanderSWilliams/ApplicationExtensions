using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.IDisposableExtensions
{
    public static class IDisposableExtensions
    {
        public static S Using<T, S>(this T source, Func<T, S> funcBody) where T : IDisposable
        {
            using (source)
            {
                return funcBody(source);
            }
        }

        public static void Using<T, S>(this T source, Action<T> actionBody) where T : IDisposable
        {
            using (source)
            {
                actionBody(source);
            }
        }
    }
}