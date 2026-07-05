using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ApfelmusFramework.Classes.ExtensionSort
{
    public static class SortExtension
    {
        public static void Sort<TSource, TValue>(this ObservableCollection<TSource> source, Func<TSource, TValue> selector)
        {
            for (int i = source.Count - 1; i >= 0; i--)
            {
                for (int j = 1; j <= i; j++)
                {
                    TSource o1 = source.ElementAt(j - 1);
                    TSource o2 = source.ElementAt(j);
                    TValue x = selector(o1);
                    TValue y = selector(o2);
                    var comparer = Comparer<TValue>.Default;
                    if (comparer.Compare(x, y) > 0)
                    {
                        source.Move(j,j-1);
                    }
                }
            }
        }
    }

}
