using Application.IDictionaryExtensions;
using Application.IEnumerableExtensions;
using Application.PropertyFieldInfoExtensions;
using Application.TypeExtensions;
using Application.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.ObjectExtensions
{
    public static class ObjectExtensions
    {
        public static void InjectInto<T>(this object source, ref T target)
        {
            if (source == null)
                return;

            if (target == null)
            {
                target = (T)typeof(T).GetInstanceOfReferenceType();
            }

            var stringDictionary = source as IDictionary<string, string>;
            if (stringDictionary != null)
            {
                stringDictionary.InjectFromStringDictionary(target);
                return;
            }

            var objectDictionary = source as IDictionary<string, object>;
            if (objectDictionary != null)
            {
                objectDictionary.InjectFromObjectDictionary(target);
                return;
            }

            var targetProps = target.GetType().DBPrimativeAssignablePropsAndFields();
            var CommonProps = targetProps.OrderedGroupJoin(source.GetType().DBPrimativePropsAndFields(), (o, i) => String.Compare(o.Name(), i.Name()), (o, i) => new
            {
                targetProp = o,
                sourceProp = i
            });

            foreach (var prop in CommonProps)
            {
                prop.targetProp.InjectValueFrom(target, prop.sourceProp.Type(), prop.sourceProp.GetValue(source));
            }
        }

        public static List<T> InjectIntoList<T>(this IEnumerable<object> source) where T : new()
        {
            var result = new List<T>();
            foreach (var item in source)
            {
                var TItem = new T();
                item.InjectInto(ref TItem);
                result.Add(TItem);
            }

            return result;
        }

        public static void InjectIntoRecursively<T>(this object source, ref T target)
        {
            if (source == null)
                return;

            source.InjectInto(ref target);

            var CommonProps = typeof(T).ComplexAssignablePropsAndFields().OrderedGroupJoin(source.GetType().ComplexPropsAndFields(),
                (o, i) => String.Compare(o.Name(), i.Name()), (o, i) => new
                {
                    targetProp = o,
                    sourceProp = i
                });

            if (!CommonProps.Any())
                return;

            foreach (var prop in CommonProps)
            {
                var targetPropValue = prop.targetProp.GetValue(target);
                if (targetPropValue == null)
                {
                    var Value = prop.targetProp.Type().GetInstanceOfReferenceType();
                    prop.targetProp.SetValue(target, Value);
                    targetPropValue = Value;
                }
                prop.sourceProp.GetValue(source).InjectIntoRecursively(ref targetPropValue);
            }
        }

        public static string ToSafeString(this object obj, bool useEnumValue = false)
        {
            if (obj == null)
                return null;

            var Type = obj.GetType();
            if (DBPrimativeTypes.Types.Contains(Type))
                return obj.ToString();
            Type = Nullable.GetUnderlyingType(Type) ?? Type;
            return (Type.IsEnum ? (useEnumValue ? Convert.ChangeType(obj, Enum.GetUnderlyingType(Type)).ToString() : obj.ToString()) : null);
        }

        public static IDictionary<string, string> ToStringStringDictionary(this object data)
        {
            if (data == null)
                return new Dictionary<string, string>();

            var OriginalDictionary = data as IDictionary<string, string>;
            if (OriginalDictionary != null)
                return OriginalDictionary;

            var ObjectDictionary = data as IDictionary<string, object>;
            if (ObjectDictionary != null)
                return ObjectDictionary.ToDictionary(x => x.Key, x => x.Value.ToSafeString());

            var Type = data.GetType();
            return Type.DBPrimativePropsAndFields().Concat(Type.ComplexPropsAndFields()).ToDictionary(x => x.Name(), x => x.GetStringValue(data));
        }
    }
}