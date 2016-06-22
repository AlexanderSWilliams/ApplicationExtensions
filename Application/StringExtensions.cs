using System;

namespace Application.StringExtensions
{
    public static class StringExtensions
    {
        public static bool? ToBool(this string obj)
        {
            bool value;
            if (obj != null && bool.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static bool ToBoolOrThrow(this string obj, string errorMessage = "Input string is not a valid boolean.")
        {
            bool value;
            if (obj != null && bool.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static DateTime? ToDateTime(this string obj)
        {
            DateTime value;
            if (obj != null && DateTime.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static DateTime ToDateTimeOrThrow(this string obj, string errorMessage = "Input string is not a valid date.")
        {
            DateTime value;
            if (obj != null && DateTime.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static decimal? ToDecimal(this string obj)
        {
            decimal value;
            if (obj != null && decimal.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static decimal ToDecimalOrThrow(this string obj, string errorMessage = "Input string is not a valid decimal.")
        {
            decimal value;
            if (obj != null && decimal.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static Guid? ToGuid(this string obj)
        {
            Guid value;
            if (obj != null && Guid.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static Guid ToGuidOrThrow(this string obj, string errorMessage = "Input string is not a valid guid.")
        {
            Guid value;
            if (obj != null && Guid.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static int? ToInt32(this string obj)
        {
            int value;
            if (obj != null && int.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static int ToInt32OrThrow(this string obj, string errorMessage = "Input string is not a valid integer.")
        {
            int value;
            if (obj != null && int.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static Int64? ToInt64(this string obj)
        {
            long value;
            if (obj != null && long.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static long ToInt64OrThrow(this string obj, string errorMessage = "Input string is not a valid long integer.")
        {
            long value;
            if (obj != null && long.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }

        public static TimeSpan? ToTimeSpan(this string obj)
        {
            TimeSpan value;
            if (obj != null && TimeSpan.TryParse(obj, out value))
                return value;
            else
                return null;
        }

        public static TimeSpan ToTimeSpanOrThrow(this string obj, string errorMessage = "Input string is not a valid time.")
        {
            TimeSpan value;
            if (obj != null && TimeSpan.TryParse(obj, out value))
                return value;
            else
                throw new ApplicationException(errorMessage);
        }
    }
}