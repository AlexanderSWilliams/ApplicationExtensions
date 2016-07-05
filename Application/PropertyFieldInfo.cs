using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Application.Types
{
    public class PropertyFieldInfo
    {
        public readonly FieldInfo Field;

        public readonly PropertyInfo Property;

        public PropertyFieldInfo(PropertyInfo property)
        {
            Property = property;
        }

        public PropertyFieldInfo(FieldInfo field)
        {
            Field = field;
        }
    }
}