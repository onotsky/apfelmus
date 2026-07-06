using System;

namespace ApfelmusFramework.Classes.Help
{
    /// <summary>
    /// Generische In-Place-Quicksort-Implementierung fuer IComparable-Werte. Kopiert das Eingabe-
    /// Array im Konstruktor und sortiert es aufsteigend ueber <see cref="Sort"/>.
    /// </summary>
    public class QuickSort<T> where T:IComparable
    {
        T[] input;

        public QuickSort(T[] values)
        {
            input = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                input[i] = values[i];
            }
        }

        public void Sort()
        {
            Sorting(0, input.Length - 1);
        }

        private int GetPivotPoint(int p1, int p2)
        {
            int pivot = p1;
            int m = p1 + 1;
            int n = p2;
            while ((m < p2) &&
                   (input[pivot].CompareTo(input[m]) >= 0))
            {
                m++;
            }

            while ((n > p1) &&
                   (input[pivot].CompareTo(input[n]) <= 0))
            {
                n--;
            }
            while (m < n)
            {
                T temp = input[m];
                input[m] = input[n];
                input[n] = temp;

                while ((m < p2) &&
                       (input[pivot].CompareTo(input[m]) >= 0))
                {
                    m++;
                }

                while ((n > p1) &&
                       (input[pivot].CompareTo(input[n]) <= 0))
                {
                    n--;
                }

            }
            if (pivot != n)
            {
                T temp2 = input[n];
                input[n] = input[pivot];
                input[pivot] = temp2;

            }
            return n;
        }

        private void Sorting(int p1, int p2)
        {
            if (p2 == p1)
            {
                return;
            }
            else
            {
                int pivot = GetPivotPoint(p1, p2);
                if (pivot > p1)
                    Sorting(p1, pivot - 1);
                if (pivot < p2)
                    Sorting(pivot + 1, p2);
            }
        }
    }
}
