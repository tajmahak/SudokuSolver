using System.Collections.Generic;

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

        public bool ContainsAnyValue(params int[] values)
        {
            HashSet<int> hashSet = new HashSet<int>(values);

            if (Value != null)
            {
                if (hashSet.Contains(Value.Value))
                {
                    return true;
                }
            }

            foreach (int pValue in ProbableValues)
            {
                if (hashSet.Contains(pValue))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsAllValues(params int[] values)
        {
            HashSet<int> hashSet = new HashSet<int>(values);

            foreach (int pValue in ProbableValues)
            {
                if (!hashSet.Contains(pValue))
                {
                    return false;
                }
            }

            return true;
        }

        // Указывает, встречаются ли возможные значения указанной ячейки внутри возможных значений этой ячейки.
        public bool ProbableIsContaining(Cell otherCell)
        {
            if (ProbableValues.Count >= otherCell.ProbableValues.Count)
            {
                foreach (int pVal in otherCell.ProbableValues)
                {
                    if (!ProbableValues.Contains(pVal))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
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
            return $"{BlockIndex}: ({RowIndex};{ColumnIndex}) = {Value} [{ProbableValues.Count}]";
        }
    }
}
