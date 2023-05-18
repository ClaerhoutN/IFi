using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IFi.Utilities
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Yield<T>(this T t)
        {
            yield return t;
        }
    }
}
