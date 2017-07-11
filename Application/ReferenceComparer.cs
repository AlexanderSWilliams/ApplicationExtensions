using System;
using System.Collections.Generic;

namespace Application.Types
{
    public class ReferenceComparer : IEqualityComparer<object>
    {
        public bool Equals(object x, object y)
        {
            return Object.ReferenceEquals(x, y);
        }

        public int GetHashCode(object codeh)
        {
            return 0;
        }
    }
}