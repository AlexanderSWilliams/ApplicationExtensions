using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Application.TypeExtensions
{
    public static class TypeExtensions
    {
        private static Dictionary<Type, Type> UnderlyingTypeDictionary = new Dictionary<Type, Type>();

        private static readonly object UnderlyingTypeDictionaryLock = new object();

        private static readonly IDictionary<Type, ConstructorInfo> ReferenceTypeDefaultConstructorStorage = new Dictionary<Type, ConstructorInfo>();

        private static readonly object ReferenceTypeDefaultConstructorStorageLock = new object();

        public static object GetInstanceOfReferenceType(this Type type)
        {
            ConstructorInfo result;
            if (ReferenceTypeDefaultConstructorStorage.TryGetValue(type, out result))
                return result?.Invoke(null);

            lock (ReferenceTypeDefaultConstructorStorageLock)
            {
                if (ReferenceTypeDefaultConstructorStorage.TryGetValue(type, out result))
                    return result?.Invoke(null);

                var Constructor = type.GetConstructor(Type.EmptyTypes);
                ReferenceTypeDefaultConstructorStorage.Add(type, Constructor);
                return Constructor?.Invoke(null);
            }
        }

        public static Type GetUnderlyingValueOrNumericType(this Type type)
        {
            Type result;
            if (UnderlyingTypeDictionary.TryGetValue(type, out result))
                return result;

            lock (UnderlyingTypeDictionaryLock)
            {
                if (UnderlyingTypeDictionary.TryGetValue(type, out result))
                    return result;

                var enumType = Nullable.GetUnderlyingType(type) ?? type;
                var underlyingOrEnumType = enumType.IsEnum ? Enum.GetUnderlyingType(enumType) : enumType;
                TypeExtensions.UnderlyingTypeDictionary.Add(type, underlyingOrEnumType);

                return underlyingOrEnumType;
            }
        }

        /// <summary>
        /// Attempts to return the value in the desired underlying type.  Avoid calling if value.GetType() == type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object ParseValue(this Type type, object value)
        {
            if (value == null)
                return null;

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (underlyingType.IsEnum)
            {
                var stringValue = value as string;
                return stringValue != null ? Enum.ToObject(underlyingType, Convert.ChangeType(value, Enum.GetUnderlyingType(underlyingType))) :
                    Enum.Parse(underlyingType, stringValue);
            }
            else if (underlyingType == typeof(TimeSpan))
            {
                var stringValue = value as string;
                if (stringValue != null)
                    return stringValue.Contains(' ') ? DateTime.ParseExact(stringValue, "h:mm tt", (IFormatProvider)CultureInfo.InvariantCulture).TimeOfDay :
                        DateTime.ParseExact(stringValue, "h:mm", (IFormatProvider)CultureInfo.InvariantCulture).TimeOfDay;
            }
            return Convert.ChangeType(value, underlyingType);
        }
    }
}