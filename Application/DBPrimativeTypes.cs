using System;
using System.Collections.Generic;

namespace Application.Types
{
    public static class DBPrimativeTypes
    {
        public static HashSet<Type> Types = new HashSet<Type> {
            typeof(string),
            typeof(decimal),
            typeof(decimal?),
            typeof(long),
            typeof(long?),
            typeof(int),
            typeof(int?),
            typeof(Int16),
            typeof(Int16?),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(TimeSpan),
            typeof(TimeSpan?),
            typeof(bool),
            typeof(bool?),
            typeof(Guid),
            typeof(Guid?)
        };
    }
}