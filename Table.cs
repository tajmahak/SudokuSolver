using System;
using System.Collections.Generic;

namespace SudokuSolver
{
    internal class Table
    {
        public int BlockLength { get; private set; }
        public int BlockCount => Length;
        public int Length { get; private set; }
        public Cell this[int row, int column] => cellsMatrix[row, column];
        public Range Cells => new Range(this, cellsList);
        private readonly List<Cell> cellsList;
        private readonly Cell[,] cellsMatrix;

        public Table(int blockLength, bool initialize)
        {
            BlockLength = blockLength;
            Length = blockLength * blockLength;
            cellsList = new List<Cell>(Length * Length);
            cellsMatrix = new Cell[Length, Length];

            if (initialize)
            {
                InitializeCells();
            }
        }


        public Range Select(Predicate<Cell> match)
        {
            return new Range(this, cellsList.FindAll(match));
        }

        public Range SelectBlock(int blockIndex)
        {
            return Select(x => x.BlockIndex == blockIndex);
        }

        public Range SelectRow(int rowIndex)
        {
            return Select(x => x.RowIndex == rowIndex);
        }

        public Range SelectColumn(int columnIndex)
        {
            return Select(x => x.ColumnIndex == columnIndex);
        }

        public bool IsCorrect()
        {
            for (int b = 0; b < BlockCount; b++)
            {
                Range block = SelectBlock(b);
                if (!block.GetIsCorrect())
                {
                    return false;
                }
            }

            for (int r = 0; r < Length; r++)
            {
                Range row = SelectRow(r);
                if (!row.GetIsCorrect())
                {
                    return false;
                }
            }

            for (int c = 0; c < Length; c++)
            {
                Range column = SelectColumn(c);
                if (!column.GetIsCorrect())
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsFilled()
        {
            return cellsList.TrueForAll(x => x.Value != null);
        }

        public Table Clone()
        {
            Table table = new Table(BlockLength, false);
            table.InitializeCells(cellsList);
            return table;
        }


        private void Clear()
        {
            foreach (Cell cell in cellsList)
            {
                cell.Clear();
            }
        }

        private void InitializeCells()
        {
            for (int r = 0; r < Length; r++)
            {
                int blockRowIndex = r / BlockLength;

                for (int c = 0; c < Length; c++)
                {
                    int blockColumnIndex = c / BlockLength;
                    int blockIndex = blockRowIndex * BlockLength + blockColumnIndex;

                    Cell cell = new Cell(blockIndex, r, c);
                    cellsList.Add(cell);
                }
            }

            InitializeMatrix();
        }

        private void InitializeCells(List<Cell> srcCellList)
        {
            foreach (Cell cell in srcCellList)
            {
                Cell copyCell = cell.Clone();
                cellsList.Add(copyCell);
            }

            InitializeMatrix();
        }

        private void InitializeMatrix()
        {
            foreach (Cell cell in cellsList)
            {
                cellsMatrix[cell.RowIndex, cell.ColumnIndex] = cell;
            }
        }
    }
}
