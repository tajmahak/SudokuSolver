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

        public void LoadFromExcel(string[] data)
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
                        cell.IsDefault = true;
                        cell.Value = int.Parse(value);
                    }
                }
            }

            AddStage(table, StrategyType.CreateTable);

            Solve();
        }

        public void LoadFromSequence(string data)
        {
            Clear();

            Table table = new Table(BlockLength, true);

            int index = 0;
            for (int r = 0; r < table.Length; r++)
            {
                for (int c = 0; c < table.Length; c++)
                {
                    string value = data[index++].ToString();
                    if (value != "0")
                    {
                        Cell cell = table[r, c];
                        cell.IsDefault = true;
                        cell.Value = int.Parse(value);
                    }
                }
            }

            AddStage(table, StrategyType.CreateTable);

            Solve();
        }

        public void Solve()
        {
            Table table = CloneLastTable();
            SetInitialProbabledValues(table);
            AddStage(table, StrategyType.InitializeProbableValues);

            StrategyType[] strategyTypes = StrategyHelper.GetStrategies();
            while (true)
            {
                table = CloneLastTable();

                bool success = false;
                StrategyInfo successStrategy = null;

                foreach (StrategyType strategyType in strategyTypes)
                {
                    StrategyInfo strategyInfo = StrategyHelper.GetStrategy(strategyType);
                    success = ApplyStrategy(strategyInfo, table);
                    if (success)
                    {
                        successStrategy = strategyInfo;
                        break;
                    }
                }

                if (success)
                {
                    AddStage(table, successStrategy.StrategyType);
                }
                else
                {
                    break;
                }
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

        private bool ApplyStrategy(StrategyInfo strategyInfo, Table table)
        {
            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Table))
            {
                bool success = strategyInfo.StrategyMethod.Invoke(table.Cells);
                if (success)
                {
                    return true;
                }
            }

            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Block))
            {
                for (int b = 0; b < table.Length; b++)
                {
                    Range block = table.SelectBlock(b);
                    bool success = strategyInfo.StrategyMethod.Invoke(block);
                    if (success)
                    {
                        return true;
                    }
                }
            }

            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Line))
            {
                for (int r = 0; r < table.Length; r++)
                {
                    Range row = table.SelectRow(r);
                    bool success = strategyInfo.StrategyMethod.Invoke(row);
                    if (success)
                    {
                        return true;
                    }
                }

                for (int c = 0; c < table.Length; c++)
                {
                    Range column = table.SelectColumn(c);
                    bool success = strategyInfo.StrategyMethod.Invoke(column);
                    if (success)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Table CloneLastTable()
        {
            return LastStage.Table.Clone();
        }

        private void AddStage(Table table, StrategyType strategyType)
        {
            Stage stage = new Stage(table, strategyType);
            Stages.Add(stage);
        }

        private void Clear()
        {
            Stages.Clear();
        }
    }
}
