using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovIRC
{
    static class Extensions
    {
        public static int IndexOf<TSource>(this IEnumerable<TSource> source,
                                           Func<TSource, bool> predicate)
        {
            int i = 0;

            foreach (TSource element in source)
            {
                if (predicate(element))
                    return i;

                i++;
            }

            return -1;
        }
    }
}
