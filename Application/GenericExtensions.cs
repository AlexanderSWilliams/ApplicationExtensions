using System;

namespace Application.GenericExtensions
{
    public static class GenericExtensions
    {
        public static bool? ToBool<T>(this T obj)
        {
            try
            {
                return obj != null ? (bool?)Convert.ChangeType(obj, typeof(bool)) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static bool ToBoolOrThrow<T>(this T obj, string errorMessage = "Input string is not a valid boolean.")
        {
            try
            {
                return (bool)Convert.ChangeType(obj, typeof(bool));
            }
            catch (Exception e)
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static DateTime? ToDateTime<T>(this T obj)
        {
            try
            {
                return obj != null ? (DateTime?)Convert.ChangeType(obj, typeof(DateTime)) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static DateTime ToDateTimeOrThrow<T>(this T obj, string errorMessage = "Input string is not a valid date.")
        {
            try
            {
                return (DateTime)Convert.ChangeType(obj, typeof(DateTime));
            }
            catch (Exception e)
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static decimal? ToDecimal<T>(this T obj)
        {
            try
            {
                return obj != null ? (decimal?)Convert.ChangeType(obj, typeof(decimal)) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static decimal ToDecimalOrThrow<T>(this T obj, string errorMessage = "Input string is not a valid decimal.")
        {
            try
            {
                return (decimal)Convert.ChangeType(obj, typeof(decimal));
            }
            catch (Exception e)
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static Guid? ToGuid<T>(this T obj)
        {
            try
            {
                return obj != null ? (Guid?)Convert.ChangeType(obj, typeof(Guid)) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static Guid ToGuidOrThrow<T>(this T obj, string errorMessage = "Input string is not a valid guid.")
        {
            try
            {
                return (Guid)Convert.ChangeType(obj, typeof(Guid));
            }
            catch (Exception e)
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static int? ToInt32<T>(this T obj)
        {
            try
            {
                return obj != null ? (int?)Convert.ChangeType(obj, typeof(int)) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static int ToInt32OrThrow<T>(this T obj, string errorMessage = "Input string is not a valid integer.")
        {
            try
            {
                return (int)Convert.ChangeType(obj, typeof(int));
            }
            catch (Exception e)
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static Int64? ToInt64<T>(this T obj)
        {
            try
            {
                return obj != null ? (long?)Convert.ChangeType(obj, typeof(long)) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static long ToInt64OrThrow<T>(this T obj, string errorMessage = "Input string is not a valid long integer.")
        {
            try
            {
                return (long)Convert.ChangeType(obj, typeof(long));
            }
            catch (Exception e)
            {
                throw new ApplicationException(errorMessage);
            }
        }

        public static TimeSpan? ToTimeSpan<T>(this T obj)
        {
            try
            {
                return obj != null ? (TimeSpan?)Convert.ChangeType(obj, typeof(TimeSpan)) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static TimeSpan ToTimeSpanOrThrow<T>(this T obj, string errorMessage = "Input string is not a valid time.")
        {
            try
            {
                return (TimeSpan)Convert.ChangeType(obj, typeof(TimeSpan));
            }
            catch (Exception e)
            {
                throw new ApplicationException(errorMessage);
            }
        }
    }
}