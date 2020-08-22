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

            StrategyResult result = new StrategyResult(StrategyType.CreateTable);
            result.Success = true;
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

            StrategyResult result = new StrategyResult(StrategyType.CreateTable);
            result.Success = true;
            AddStage(table, result);

            Solve();
        }

        public void Solve()
        {
            Table table = CloneLastTable();

            StrategyInfo initStrategy = StrategyHelper.GetInitializeProbableValuesStrategy();
            ApplyStrategy(initStrategy, table);

            StrategyInfo[] strategies = StrategyHelper.GetStrategies();
            bool success;
            do
            {
                success = false;
                table = CloneLastTable();
                foreach (StrategyInfo strategy in strategies)
                {
                    bool successStrategy = ApplyStrategy(strategy, table);
                    if (successStrategy)
                    {
                        success = true;
                        break;
                    }
                }

            } while (success);
        }

        private Table CloneLastTable()
        {
            return LastStage.Table.Clone();
        }

        private bool ApplyStrategy(StrategyInfo strategyInfo, Table table)
        {
            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Table))
            {
                StrategyResult result = new StrategyResult(strategyInfo.StrategyType);
                strategyInfo.StrategyMethod.Invoke(result, table.Cells);
                if (result.Success)
                {
                    AddStage(table, result);
                    return true;
                }
            }

            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Block))
            {
                for (int b = 0; b < table.Length; b++)
                {
                    Range block = table.SelectBlock(b);
                    StrategyResult result = new StrategyResult(strategyInfo.StrategyType);
                    strategyInfo.StrategyMethod.Invoke(result, block);
                    if (result.Success)
                    {
                        AddStage(table, result);
                        return true;
                    }
                }
            }

            if (strategyInfo.StrategyArea.HasFlag(StrategyArea.Line))
            {
                for (int r = 0; r < table.Length; r++)
                {
                    Range row = table.SelectRow(r);
                    StrategyResult result = new StrategyResult(strategyInfo.StrategyType);
                    strategyInfo.StrategyMethod.Invoke(result, row);
                    if (result.Success)
                    {
                        AddStage(table, result);
                        return true;
                    }
                }

                for (int c = 0; c < table.Length; c++)
                {
                    Range column = table.SelectColumn(c);
                    StrategyResult result = new StrategyResult(strategyInfo.StrategyType);
                    strategyInfo.StrategyMethod.Invoke(result, column);
                    if (result.Success)
                    {
                        AddStage(table, result);
                        return true;
                    }
                }
            }

            return false;
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
