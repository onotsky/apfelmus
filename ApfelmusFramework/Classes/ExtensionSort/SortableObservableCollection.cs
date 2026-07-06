using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ApfelmusFramework.Classes.ExtensionSort
{
    /// <summary>
    /// Erweiterungsmethode zum In-Place-Sortieren einer ObservableCollection.
    /// </summary>
    public static class SortExtension
    {
        /// <summary>
        /// Sortiert die Collection stabil aufsteigend nach dem per <paramref name="selector"/>
        /// gewaehlten Schluessel - bewusst per Move() (Bubblesort) statt Neuaufbau, damit die
        /// Collection-Instanz erhalten bleibt und Bindings/DataGrid-Auswahl nicht verloren gehen.
        /// </summary>
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
