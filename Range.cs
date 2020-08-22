using System;
using System.Collections.Generic;

namespace SudokuSolver
{
    internal class Range : List<Cell>
    {
        public Table Table { get; private set; }

        public bool IsRow => GetRowsHashSet().Count == 1;

        public bool IsColumn => GetColumnsHashSet().Count == 1;

        public Range(Table table) : base()
        {
            Table = table;
        }

        public Range(Table table, IEnumerable<Cell> collection) : base(collection)
        {
            Table = table;
        }


        public Range Select(Predicate<Cell> match)
        {
            return new Range(Table, FindAll(match));
        }

        public Range SelectEmptyCells()
        {
            return Select(x => x.Value == null);
        }

        public Range SelectFilledCells()
        {
            return Select(x => x.Value != null);
        }

        public HashSet<int> GetValuesHashSet()
        {
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < Count; i++)
            {
                int? value = this[i].Value;
                if (value != null)
                {
                    hashSet.Add(value.Value);
                }
            }
            return hashSet;
        }

        public HashSet<int> GetProbableValuesHashSet()
        {
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < Count; i++)
            {
                foreach (int pVal in this[i].ProbableValues)
                {
                    hashSet.Add(pVal);
                }
            }
            return hashSet;
        }

        public HashSet<int> GetRowsHashSet()
        {
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < Count; i++)
            {
                int row = this[i].RowIndex;
                hashSet.Add(row);
            }
            return hashSet;
        }

        public HashSet<int> GetColumnsHashSet()
        {
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < Count; i++)
            {
                int column = this[i].ColumnIndex;
                hashSet.Add(column);
            }
            return hashSet;
        }

        public bool GetIsCorrect()
        {
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < Count; i++)
            {
                int? value = this[i].Value;
                if (value != null)
                {
                    if (hashSet.Contains(value.Value))
                    {
                        return false;
                    }
                    hashSet.Add(value.Value);
                }
            }
            return true;
        }
    }
}
