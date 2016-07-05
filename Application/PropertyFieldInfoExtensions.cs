using Application.IDictionaryExtensions;
using Application.TypeExtensions;
using Application.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Application.PropertyFieldInfoExtensions
{
    public static class PropertyFieldInfoExtensions
    {
        private static readonly IDictionary<Type, IEnumerable<PropertyFieldInfo>> ComplexAssignableStorage = new Dictionary<Type, IEnumerable<PropertyFieldInfo>>();
        private static readonly object ComplexAssignableStorageLock = new object();
        private static readonly IDictionary<Type, IEnumerable<PropertyFieldInfo>> ComplexStorage = new Dictionary<Type, IEnumerable<PropertyFieldInfo>>();
        private static readonly object ComplexStorageLock = new object();
        private static readonly IDictionary<Type, IEnumerable<PropertyFieldInfo>> DBPrimativeAssignableStorage = new Dictionary<Type, IEnumerable<PropertyFieldInfo>>();
        private static readonly object DBPrimativeAssignableStorageLock = new object();
        private static readonly IDictionary<Type, IEnumerable<PropertyFieldInfo>> DBPrimativeStorage = new Dictionary<Type, IEnumerable<PropertyFieldInfo>>();

        private static readonly object DBPrimativeStorageLock = new object();

        private static Dictionary<Type, byte> numericDictionary = new Dictionary<Type, byte>{
                            {typeof(byte), 1},
                            {typeof(short), 2},
                            {typeof(int), 3},
                            {typeof(float), 4},
                            {typeof(long), 5},
                            {typeof(double), 6},
                            {typeof(decimal), 7}
                        };

        public static IEnumerable<PropertyFieldInfo> ComplexAssignablePropsAndFields(this Type type)
        {
            var result = Enumerable.Empty<PropertyFieldInfo>();
            if (ComplexAssignableStorage.TryGetValue(type, out result))
                return result;

            lock (ComplexAssignableStorageLock)
            {
                if (ComplexAssignableStorage.TryGetValue(type, out result))
                    return result;

                var propertiesAndFields = ComplexPropsAndFields(type).Where(x => x.IsAssignable()).OrderBy(x => x.Name()).ToList();
                ComplexAssignableStorage.Add(type, propertiesAndFields);
                return propertiesAndFields;
            }
        }

        public static IEnumerable<PropertyFieldInfo> ComplexPropsAndFields(this Type type)
        {
            var result = Enumerable.Empty<PropertyFieldInfo>();
            if (ComplexStorage.TryGetValue(type, out result))
                return result;

            lock (ComplexStorageLock)
            {
                if (ComplexStorage.TryGetValue(type, out result))
                    return result;

                var propertiesAndFields = new List<PropertyFieldInfo>();
                foreach (var prop in type.GetProperties().Where(x => x.CanRead && x.GetGetMethod(true).IsPublic))
                {
                    propertiesAndFields.Add(new PropertyFieldInfo(prop));
                }

                foreach (var field in type.GetFields().Where(x => x.IsPublic))
                {
                    propertiesAndFields.Add(new PropertyFieldInfo(field));
                }

                propertiesAndFields = propertiesAndFields.GroupJoin(DBPrimativePropsAndFields(type), o => o.Name(), i => i.Name(), (o, i) => new
                {
                    Outer = o,
                    Inner = i
                })
                .Where(x => !x.Inner.Any())
                .Select(x => x.Outer)
                .OrderBy(x => x.Name())
                .ToList();

                ComplexStorage.Add(type, propertiesAndFields);
                return propertiesAndFields;
            }
        }

        public static IEnumerable<CustomAttributeData> CustomAttributes(this PropertyFieldInfo propertyFieldInfo)
        {
            return propertyFieldInfo.Property?.CustomAttributes ?? propertyFieldInfo.Field?.CustomAttributes;
        }

        public static IEnumerable<PropertyFieldInfo> DBPrimativeAssignablePropsAndFields(this Type type)
        {
            var result = Enumerable.Empty<PropertyFieldInfo>();
            if (DBPrimativeAssignableStorage.TryGetValue(type, out result))
                return result;

            lock (DBPrimativeAssignableStorageLock)
            {
                if (DBPrimativeAssignableStorage.TryGetValue(type, out result))
                    return result;

                var propertiesAndFields = DBPrimativePropsAndFields(type).Where(x => x.IsAssignable()).OrderBy(x => x.Name()).ToList();
                DBPrimativeAssignableStorage.Add(type, propertiesAndFields);
                return propertiesAndFields;
            }
        }

        public static IEnumerable<PropertyFieldInfo> DBPrimativePropsAndFields(this Type type)
        {
            var result = Enumerable.Empty<PropertyFieldInfo>();
            if (DBPrimativeStorage.TryGetValue(type, out result))
                return result;

            lock (DBPrimativeStorageLock)
            {
                if (DBPrimativeStorage.TryGetValue(type, out result))
                    return result;

                var propertiesAndFields = new List<PropertyFieldInfo>();

                var props = type.GetProperties().Where(x => x.CanRead && x.GetGetMethod(true).IsPublic);
                foreach (var prop in props)
                {
                    if ((DBPrimativeTypes.Types.Contains(prop.PropertyType) || (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType).IsEnum) && prop.GetIndexParameters().Length == 0)
                        propertiesAndFields.Add(new PropertyFieldInfo(prop));
                }

                var fields = type.GetFields().Where(x => x.IsPublic);
                foreach (var field in fields)
                {
                    if (DBPrimativeTypes.Types.Contains(field.FieldType) || (Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType).IsEnum)
                        propertiesAndFields.Add(new PropertyFieldInfo(field));
                }

                propertiesAndFields = propertiesAndFields.OrderBy(x => x.Name()).ToList();
                DBPrimativeStorage.Add(type, propertiesAndFields);
                return propertiesAndFields;
            }
        }

        public static PropertyFieldInfo Find(this IEnumerable<PropertyFieldInfo> propertyFieldInfos, string name)
        {
            return propertyFieldInfos.Where(x => x.Name() == name).FirstOrDefault();
        }

        public static string GetStringValue(this PropertyFieldInfo propertyFieldInfo, object obj, object[] index = null, bool useEnumValue = false)
        {
            var Value = GetValue(propertyFieldInfo, obj, index);
            if (Value == null)
                return null;

            var Type = propertyFieldInfo.Type();
            if (DBPrimativeTypes.Types.Contains(Type))
                return Value.ToString();
            Type = Nullable.GetUnderlyingType(Type) ?? Type;

            return Type.IsEnum && useEnumValue ? Convert.ChangeType(Value, Enum.GetUnderlyingType(Type)).ToString() : Value.ToString();
        }

        public static object GetValue(this PropertyFieldInfo propertyFieldInfo, object obj, object[] index = null)
        {
            return propertyFieldInfo.Property?.GetValue(obj, index) ?? propertyFieldInfo.Field?.GetValue(obj);
        }

        public static bool InjectValueFrom(this PropertyFieldInfo destinationField, object destinationInstance, Type sourceType, object value)
        {
            var destinationType = destinationField.Type();
            if (!destinationType.IsValueType && destinationType != typeof(string))
                return false;

            if (sourceType == null)
            {
                destinationField.SetValue(destinationInstance, null);
                return true;
            }

            if (sourceType == destinationType)
            {
                destinationField.SetValue(destinationInstance, value);
                return true;
            }

            var DestinationUnderlyingType = destinationType.GetUnderlyingValueOrNumericType();
            var SourceUnderlyingType = sourceType.GetUnderlyingValueOrNumericType();
            if (DestinationUnderlyingType == SourceUnderlyingType)
            {
                destinationField.SetValue(destinationInstance, destinationType.ParseValue(value));
                return true;
            }

            var DestinationNumericType = numericDictionary.GetValueOrNull(DestinationUnderlyingType);
            if (DestinationNumericType != null)
            {
                var SourceNumericType = numericDictionary.GetValueOrNull(SourceUnderlyingType);
                if (DestinationNumericType >= SourceNumericType)
                {
                    destinationField.SetValue(destinationInstance, destinationType.ParseValue(value));
                    return true;
                }
            }
            return false;
        }

        public static bool IsAssignable(this PropertyFieldInfo propertyFieldInfo)
        {
            if (propertyFieldInfo.Property != null)
                return propertyFieldInfo.Property.CanWrite && propertyFieldInfo.Property.GetSetMethod(true).IsPublic;
            else
                return !propertyFieldInfo.Field.IsLiteral && !propertyFieldInfo.Field.IsInitOnly;
        }

        public static string Name(this PropertyFieldInfo propertyFieldInfo)
        {
            return propertyFieldInfo.Property?.Name ?? propertyFieldInfo.Field?.Name;
        }

        public static void SetValue(this PropertyFieldInfo propertyFieldInfo, object obj, object value, object[] index = null)
        {
            if (propertyFieldInfo.Property != null)
                propertyFieldInfo.Property.SetValue(obj, value, index);
            else
                propertyFieldInfo.Field.SetValue(obj, value);
        }

        public static bool TakesParameters(this PropertyFieldInfo propertyFieldInfo)
        {
            return propertyFieldInfo.Property?.GetIndexParameters().Length > 0;
        }

        public static Type Type(this PropertyFieldInfo propertyFieldInfo)
        {
            return propertyFieldInfo.Property?.PropertyType ?? propertyFieldInfo.Field?.FieldType;
        }
    }
}