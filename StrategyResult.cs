﻿using System.Collections.Generic;

namespace SudokuSolver
{
    internal class StrategyResult
    {
        public StrategyResult(StrategyType strategyType)
        {
            StrategyType = strategyType;
        }

        public StrategyType StrategyType { get; private set; }

        public bool Success { get; set; }

        public List<Cell> RelationCells { get; private set; } = new List<Cell>();

        public List<Cell> RelationValues { get; private set; } = new List<Cell>();

        public List<Cell> RemovedValues { get; private set; } = new List<Cell>();


        public void AddRelationCell(int row, int column)
        {
            if (!RelationCells.Exists(x => x.RowIndex == row && x.ColumnIndex == column))
            {
                Cell cell = new Cell(-1, row, column);
                RelationCells.Add(cell);
            }
        }

        public void AddRelationCell(Cell cell)
        {
            AddRelationCell(cell.RowIndex, cell.ColumnIndex);
        }

        public void AddRelationCell(ICollection<Cell> cells)
        {
            foreach (Cell cell in cells)
            {
                AddRelationCell(cell.RowIndex, cell.ColumnIndex);
            }
        }


        public void AddRelationValues(int row, int column, params int[] values)
        {
            Cell cell = RelationValues.Find(x => x.RowIndex == row && x.ColumnIndex == column);
            if (cell == null)
            {
                cell = new Cell(-1, row, column);
                RelationValues.Add(cell);
            }
            foreach (int value in values)
            {
                cell.ProbableValues.Add(value);
            }
        }

        public void AddRelationValues(Cell cell, params int[] values)
        {
            AddRelationValues(cell.RowIndex, cell.ColumnIndex, values);
        }

        public void AddRelationValues(ICollection<Cell> cells, params int[] values)
        {
            foreach (Cell cell in cells)
            {
                AddRelationValues(cell, values);
            }
        }


        public void AddRemovedValues(int row, int column, params int[] values)
        {
            Cell cell = RemovedValues.Find(x => x.RowIndex == row && x.ColumnIndex == column);
            if (cell == null)
            {
                cell = new Cell(-1, row, column);
                RemovedValues.Add(cell);
            }
            foreach (int value in values)
            {
                cell.ProbableValues.Add(value);
            }
        }


        public Cell GetRelationCell(int row, int column)
        {
            return RelationCells.Find(x => x.RowIndex == row && x.ColumnIndex == column);
        }

        public Cell GetRelationValueCell(int row, int column)
        {
            return RelationValues.Find(x => x.RowIndex == row && x.ColumnIndex == column);
        }

        public Cell GetRemovedValueCell(int row, int column)
        {
            return RemovedValues.Find(x => x.RowIndex == row && x.ColumnIndex == column);
        }
    }
}
