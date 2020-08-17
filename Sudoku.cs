using System.Collections.Generic;
using System.Linq;

namespace SudokuSolver
{
    internal class Sudoku
    {
        public int BlockLength { get; private set; }
        public List<Stage> Stages { get; private set; } = new List<Stage>();
        public Stage LastStage => Stages.Last();

        public Sudoku(int blockLength)
        {
            BlockLength = blockLength;
        }

        public void Load(string[] data)
        {
            Clear();

            Table table = new Table(BlockLength, true);

            for (int r = 0; r < table.Length; r++)
            {
                string[] split = data[r].Split('\t');
                for (int c = 0; c < table.Length; c++)
                {
                    string value = split[c];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Cell cell = table[r, c];
                        cell.Value = int.Parse(value);
                    }
                }
            }

            AddStage(table);

            Solve();
        }

        public void Solve()
        {
            Table table = CloneLastTable();
            SetInitialProbabledValues(table);
            AddStage(table);

            while (true)
            {
                table = CloneLastTable();

                bool success = SetProbableValue(table);
                if (success)
                {
                    AddStage(table);
                    continue;
                }

                success = FilterProbableValues(table,
                    FilterProbableValues_1,
                    FilterProbableValues_2);

                if (success)
                {
                    AddStage(table);
                    continue;
                }

                break;
            }
        }



        private void SetInitialProbabledValues(Table table)
        {
            // Установка всех возможных значений по умолчанию
            foreach (Cell cell in table.Cells)
            {
                cell.ProbableValues.Clear();
                if (cell.Value == null)
                {
                    for (int v = 1; v <= table.Length; v++)
                    {
                        cell.ProbableValues.Add(v);
                    }
                }
            }

            // Фильтрация лишних значений
            for (int b = 0; b < table.Length; b++)
            {
                Range block = table.SelectBlock(b);
                FilterInitialProbableValues(block);
            }
            for (int r = 0; r < table.Length; r++)
            {
                Range row = table.SelectRow(r);
                FilterInitialProbableValues(row);
            }
            for (int c = 0; c < table.Length; c++)
            {
                Range column = table.SelectColumn(c);
                FilterInitialProbableValues(column);
            }
        }

        private void FilterInitialProbableValues(Range range)
        {
            HashSet<int> existsValues = range.GetValuesHashSet();
            Range emptyCells = range.SelectEmptyCells();

            foreach (Cell emptyCell in emptyCells)
            {
                foreach (int existsValue in existsValues)
                {
                    emptyCell.ProbableValues.Remove(existsValue);
                }
            }
        }


        private bool SetProbableValue(Table table)
        {
            Cell cell = table.Cells.Find(x => x.ProbableValues.Count == 1);

            if (cell != null)
            {
                cell.Value = cell.GetFirstProbableValue();
                cell.ProbableValues.Clear();

                Range block = table.Select(x => x.BlockIndex == cell.BlockIndex);
                ClearProbableValue(block, cell.Value.Value);

                Range row = table.Select(x => x.RowIndex == cell.RowIndex);
                ClearProbableValue(row, cell.Value.Value);

                Range column = table.Select(x => x.ColumnIndex == cell.ColumnIndex);
                ClearProbableValue(column, cell.Value.Value);

                return true;
            }

            return false;
        }

        private void ClearProbableValue(Range range, int value)
        {
            foreach (Cell cell in range)
            {
                cell.ProbableValues.Remove(value);
            }
        }


        private delegate bool SudokuFilter(Range range);

        private bool FilterProbableValues(Table table, params SudokuFilter[] sudokuFilters)
        {
            foreach (SudokuFilter sudokuFilter in sudokuFilters)
            {
                for (int b = 0; b < table.Length; b++)
                {
                    Range block = table.SelectBlock(b);
                    bool success = sudokuFilter(block);
                    if (success)
                    {
                        return true;
                    }
                }
                for (int r = 0; r < table.Length; r++)
                {
                    Range row = table.SelectRow(r);
                    bool success = sudokuFilter(row);
                    if (success)
                    {
                        return true;
                    }
                }
                for (int c = 0; c < table.Length; c++)
                {
                    Range column = table.SelectColumn(c);
                    bool success = sudokuFilter(column);
                    if (success)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool FilterProbableValues_1(Range range)
        {
            // если в диапазоне из возможных значений ячейки находится то, которое не повторяется 
            // в других ячейках, то единственным вариантом для ячейки будет именно это значение

            Range emptyCells = range.SelectEmptyCells();

            for (int v = 1; v <= range.Table.Length; v++)
            {
                int repeatCount = 0;
                Cell candidateCell = null;

                foreach (Cell emptyCell in emptyCells)
                {
                    if (emptyCell.ProbableValues.Contains(v))
                    {
                        if (candidateCell == null)
                        {
                            candidateCell = emptyCell;
                        }
                        repeatCount++;
                    }
                }

                if (repeatCount == 1)
                {
                    candidateCell.ProbableValues.Clear();
                    candidateCell.ProbableValues.Add(v);
                    return true;
                }
            }

            return false;
        }

        private bool FilterProbableValues_2(Range range)
        {
            // если в диапазоне встречаются ячейки с одинаковыми возможными значениями, 
            // соответственно эти значения могут быть только у этих ячеек

            Range emptyCells = range.SelectEmptyCells();

            for (int i = 0; i < emptyCells.Count - 1; i++)
            {
                Cell cell = emptyCells[i];

                Range similarCells = emptyCells.Select(x => x != cell && x.ProbableIsEquals(cell));
                similarCells.Add(cell);

                if (cell.ProbableValues.Count == similarCells.Count)
                {
                    int clearCount = 0;

                    Range filteredCells = emptyCells.Select(x => !similarCells.Contains(x));
                    foreach (Cell filteredCell in filteredCells)
                    {
                        foreach (int pValue in cell.ProbableValues)
                        {
                            if (filteredCell.ProbableValues.Remove(pValue))
                            {
                                clearCount++;
                            }
                        }
                    }

                    return clearCount > 0;
                }
            }

            return false;
        }


        private Table CloneLastTable()
        {
            return LastStage.Table.Clone();
        }

        private void AddStage(Table table)
        {
            Stage stage = new Stage(table);
            Stages.Add(stage);
        }

        private void Clear()
        {
            Stages.Clear();
        }
    }
}
