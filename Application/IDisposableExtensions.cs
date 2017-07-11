using System;

namespace Application.IDisposableExtensions
{
    public static class IDisposableExtensions
    {
        public static S Using<T, S>(this T source, Func<T, S> tryBody) where T : IDisposable
        {
            try
            {
                return tryBody(source);
            }
            finally
            {
                if (source != null)
                    ((IDisposable)source).Dispose();
            }
        }

        public static S Using<T, R, S>(this T source, Func<T, R> tryBody, Func<T, R, S> finallyBody) where T : IDisposable
        {
            var FinallyResult = default(S);
            var TryResult = default(R);
            try
            {
                TryResult = tryBody(source);
            }
            finally
            {
                FinallyResult = finallyBody(source, TryResult);
                if (source != null)
                    ((IDisposable)source).Dispose();
            }
            return FinallyResult;
        }

        public static S Using<T, R, S>(this T source, Func<T, R> tryBody, Func<Exception, T, bool> catchBody, Func<T, R, S> finallyBody) where T : IDisposable
        {
            var FinallyResult = default(S);
            var TryResult = default(R);
            try
            {
                TryResult = tryBody(source);
            }
            catch (Exception e)
            {
                if (!catchBody(e, source))
                    throw;
            }
            finally
            {
                FinallyResult = finallyBody(source, TryResult);
                if (source != null)
                    ((IDisposable)source).Dispose();
            }
            return FinallyResult;
        }

        public static void Using<T>(this T source, Action<T> tryBody) where T : IDisposable
        {
            try
            {
                tryBody(source);
            }
            finally
            {
                if (source != null)
                    ((IDisposable)source).Dispose();
            }
        }

        public static void Using<T, R>(this T source, Func<T, R> tryBody, Action<T, R> finallyBody) where T : IDisposable
        {
            var TryResult = default(R);
            try
            {
                TryResult = tryBody(source);
            }
            finally
            {
                finallyBody(source, TryResult);
                if (source != null)
                    ((IDisposable)source).Dispose();
            }
        }

        public static void Using<T, R>(this T source, Func<T, R> tryBody, Func<Exception, T, bool> catchBody, Action<T, R> finallyBody) where T : IDisposable
        {
            var TryResult = default(R);
            try
            {
                TryResult = tryBody(source);
            }
            catch (Exception e)
            {
                if (!catchBody(e, source))
                    throw;
            }
            finally
            {
                finallyBody(source, TryResult);
                if (source != null)
                    ((IDisposable)source).Dispose();
            }
        }
    }
}