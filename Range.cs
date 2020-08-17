using System;
using System.Collections.Generic;

namespace SudokuSolver
{
    internal class Range : List<Cell>
    {
        public Table Table { get; private set; }

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

        public HashSet<int> GetAnyValueHashSet()
        {
            HashSet<int> hashSet = new HashSet<int>();
            foreach (int value in GetValuesHashSet())
            {
                hashSet.Add(value);
            }
            foreach (int value in GetProbableValuesHashSet())
            {
                hashSet.Add(value);
            }
            return hashSet;
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

        public bool ContainsAnyValue(int value)
        {
            foreach (Cell cell in this)
            {
                if (cell.Value == value)
                {
                    return true;
                }
            }
            return ContainsProbableValue(value);
        }

        public bool ContainsProbableValue(int value)
        {
            foreach (Cell cell in this)
            {
                if (cell.ProbableValues.Contains(value))
                {
                    return true;
                }
            }
            return false;
        }

        public int GetEmptyCellsCount()
        {
            int count = 0;
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Value == null)
                {
                    count++;
                }
            }
            return count;
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
