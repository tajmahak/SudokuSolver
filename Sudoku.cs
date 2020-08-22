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

            StrategyResult result = new StrategyResult();
            result.Success = true;
            result.StrategyType = StrategyType.CreateTable;
            AddStage(table, result);

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

            StrategyResult result = new StrategyResult();
            result.Success = true;
            result.StrategyType = StrategyType.CreateTable;
            AddStage(table, result);

            Solve();
        }

        public void Solve()
        {
            Table table = CloneLastTable();
            SetInitialProbabledValues(table);

            StrategyResult initResult = new StrategyResult();
            initResult.Success = true;
            initResult.StrategyType = StrategyType.InitializeProbableValues;
            AddStage(table, initResult);

            StrategyType[] strategyTypes = StrategyHelper.GetStrategies();
            while (true)
            {
                table = CloneLastTable();

                StrategyResult result = null;
                foreach (StrategyType strategyType in strategyTypes)
                {
                    StrategyInfo strategyInfo = StrategyHelper.GetStrategy(strategyType);
                    result = ApplyStrategy(strategyInfo, table);
                    if (result.Success)
                    {
                        break;
                    }
                }

                if (result.Success)
                {
                    AddStage(table, result);
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

        private StrategyResult ApplyStrategy(StrategyInfo strategyInfo, Table table)
        {
            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Table))
            {
                StrategyResult result = strategyInfo.StrategyMethod.Invoke(table.Cells);
                if (result.Success)
                {
                    return result;
                }
            }

            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Block))
            {
                for (int b = 0; b < table.Length; b++)
                {
                    Range block = table.SelectBlock(b);
                    StrategyResult result = strategyInfo.StrategyMethod.Invoke(block);
                    if (result.Success)
                    {
                        return result;
                    }
                }
            }

            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Line))
            {
                for (int r = 0; r < table.Length; r++)
                {
                    Range row = table.SelectRow(r);
                    StrategyResult result = strategyInfo.StrategyMethod.Invoke(row);
                    if (result.Success)
                    {
                        return result;
                    }
                }

                for (int c = 0; c < table.Length; c++)
                {
                    Range column = table.SelectColumn(c);
                    StrategyResult result = strategyInfo.StrategyMethod.Invoke(column);
                    if (result.Success)
                    {
                        return result;
                    }
                }
            }

            return StrategyResult.EmptyResult;
        }

        private Table CloneLastTable()
        {
            return LastStage.Table.Clone();
        }

        private void AddStage(Table table, StrategyResult result)
        {
            Stage stage = new Stage(table, result);
            Stages.Add(stage);
        }

        private void Clear()
        {
            Stages.Clear();
        }
    }
}
