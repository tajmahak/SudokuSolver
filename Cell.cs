using System.Collections.Generic;
using System.Text;

namespace SudokuSolver
{
    internal class Cell
    {
        public int BlockIndex { get; private set; }
        public int RowIndex { get; private set; }
        public int ColumnIndex { get; private set; }

        public int? Value { get; set; }
        public HashSet<int> ProbableValues { get; private set; }
        public bool IsDefault { get; set; }

        public Cell(int blockIndex, int rowIndex, int columnIndex)
        {
            BlockIndex = blockIndex;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;

            ProbableValues = new HashSet<int>();
        }

        public bool ContainsAnyValue(HashSet<int> values)
        {
            if (Value != null)
            {
                if (values.Contains(Value.Value))
                {
                    return true;
                }
            }

            foreach (int pValue in ProbableValues)
            {
                if (values.Contains(pValue))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsAnyValue(params int[] values)
        {
            HashSet<int> hashSet = new HashSet<int>(values);
            return ContainsAnyValue(hashSet);
        }

        public bool ContainsAllValues(HashSet<int> values)
        {
            if (ProbableValues.Count == 0)
            {
                return false;
            }
            else
            {
                foreach (int pValue in values)
                {
                    if (!ProbableValues.Contains(pValue))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool ContainsAllValues(params int[] values)
        {
            HashSet<int> hashSet = new HashSet<int>(values);
            return ContainsAllValues(hashSet);
        }

        public Cell Clone()
        {
            Cell copyCell = new Cell(BlockIndex, RowIndex, ColumnIndex);
            copyCell.Value = Value;
            copyCell.IsDefault = IsDefault;
            foreach (int pVal in ProbableValues)
            {
                copyCell.ProbableValues.Add(pVal);
            }
            return copyCell;
        }

        public void Clear()
        {
            Value = null;
            ProbableValues.Clear();
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append($"{BlockIndex}: ({RowIndex};{ColumnIndex}) = ");
            if (Value != null)
            {
                str.Append(Value);
            }
            else
            {
                str.Append("[");
                foreach (int pValue in ProbableValues)
                {
                    str.Append(pValue);
                }
                str.Append("]");
            }

            return str.ToString();
        }
    }
}
