
using System;
using System.Reflection;

namespace SudokuSolver
{
    internal static class StrategyHelper
    {
        public static StrategyType[] GetStrategies()
        {
            StrategyType[] array = (StrategyType[])Enum.GetValues(typeof(StrategyType));

            // Отсеивание инициализирующих стратегий
            array = Array.FindAll(array, x => !(x == StrategyType.CreateTable || x == StrategyType.InitializeProbableValues));

            return array;
        }

        public static StrategyInfo GetStrategy(StrategyType strategyType)
        {
            MethodInfo[] methods = typeof(StrategyHelper).GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attrs = method.GetCustomAttributes(typeof(StrategyAttribute), false);
                if (attrs.Length > 0)
                {
                    StrategyAttribute attr = (StrategyAttribute)attrs[0];
                    if (attr.StrategyType == strategyType)
                    {
                        StrategyMethod strategyMethod = (StrategyMethod)Delegate.CreateDelegate(typeof(StrategyMethod), method);
                        return new StrategyInfo(attr.StrategyType, strategyMethod, attr.StrategyArea);
                    }
                }
            }

            throw new Exception("Стратегия не найдена.");
        }


        [Strategy(StrategyType.SetValueToSolvedCell, StrategyArea.Table)]
        private static bool SetValueToSolvedCell(Range range)
        {
            Cell cell = range.Find(x => x.ProbableValues.Count == 1);

            if (cell != null)
            {
                int value = cell.GetFirstProbableValue();
                cell.Value = value;
                cell.ProbableValues.Clear();

                Range block = range.Select(x => x != cell && x.BlockIndex == cell.BlockIndex);
                foreach (Cell otherCell in block)
                {
                    otherCell.ProbableValues.Remove(value);
                }

                Range row = range.Select(x => x != cell && x.RowIndex == cell.RowIndex);
                foreach (Cell otherCell in row)
                {
                    otherCell.ProbableValues.Remove(value);
                }

                Range column = range.Select(x => x != cell && x.ColumnIndex == cell.ColumnIndex);
                foreach (Cell otherCell in column)
                {
                    otherCell.ProbableValues.Remove(value);
                }

                return true;
            }

            return false;
        }

     
        [Strategy(StrategyType.HiddenSingles, StrategyArea.Block | StrategyArea.Line)]
        private static bool HiddenSingles(Range range)
        {
            // https://www.sudokuwiki.org/Getting_Started

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

            return false;
        }

        [Strategy(StrategyType.NakedPairsTriples, StrategyArea.Block | StrategyArea.Line)]
        private static bool NakedPairsTriples(Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NP

            return NakedSet(range, 3);
        }

        [Strategy(StrategyType.HiddenPairsTriples, StrategyArea.Block | StrategyArea.Line)]
        private static bool HiddenPairsTriples(Range range)
        {
            // https://www.sudokuwiki.org/Hidden_Candidates#HP

            return HiddenSet(range, 3);
        }

        [Strategy(StrategyType.NakedQuards, StrategyArea.Block | StrategyArea.Line)]
        private static bool NakedQuards(Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NQ

            return NakedSet(range, 4);
        }

        [Strategy(StrategyType.HiddenQuards, StrategyArea.Block | StrategyArea.Line)]
        private static bool HiddenQuards(Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NQ

            return HiddenSet(range, 4);
        }

        [Strategy(StrategyType.PointingPairs, StrategyArea.Line)]
        private static bool PointingPairs(Range range)
        {
            // https://www.sudokuwiki.org/Intersection_Removal#IR

            return false;
        }

        [Strategy(StrategyType.BoxLineReduction, StrategyArea.Block)]
        private static bool BoxLineReduction(Range range)
        {
            // https://www.sudokuwiki.org/Intersection_Removal#LBR

            int blockIndex = range[0].BlockIndex;
            Range allCells = range.Table.Cells;

            Range emptyBlockCells = range.SelectEmptyCells();
            foreach (int v in emptyBlockCells.GetProbableValuesHashSet())
            {
                int hits = 0;
                bool rowOrColumn = false;
                Range candidateCells = null;

                // Проверка строк
                foreach (int r in emptyBlockCells.GetRowsHashSet())
                {
                    // диапазон возможного значения, которое находится в строке рассматриваемого блока
                    Range probableBlockCells = emptyBlockCells.Select(x => x.RowIndex == r && x.ProbableValues.Contains(v));
                    if (probableBlockCells.Count > 0)
                    {
                        // ячейки из других блоков, которые могут содержать вероятное значение
                        Range otherCells = allCells.Select(x => x.BlockIndex != blockIndex && x.RowIndex == r && (x.Value == v || x.ProbableValues.Contains(v)));
                        if (otherCells.Count == 0)
                        {
                            hits++;
                            rowOrColumn = true;
                            candidateCells = probableBlockCells;
                        }
                    }
                }

                // Проверка столбцов
                foreach (int c in emptyBlockCells.GetColumnsHashSet())
                {
                    // диапазон возможного значения, которое находится в строке рассматриваемого блока
                    Range probableBlockCells = emptyBlockCells.Select(x => x.ColumnIndex == c && x.ProbableValues.Contains(v));
                    if (probableBlockCells.Count > 0)
                    {
                        // ячейки из других блоков, которые могут содержать вероятное значение
                        Range otherCells = allCells.Select(x => x.BlockIndex != blockIndex && x.ColumnIndex == c && (x.Value == v || x.ProbableValues.Contains(v)));
                        if (otherCells.Count == 0)
                        {
                            hits++;
                            rowOrColumn = false;
                            candidateCells = probableBlockCells;
                        }
                    }
                }

                if (hits == 1)
                {
                    Range removeValueRange;
                    if (rowOrColumn)
                    {
                        int rowIndex = candidateCells[0].RowIndex;
                        removeValueRange = emptyBlockCells.Select(x => x.RowIndex != rowIndex && x.ProbableValues.Contains(v));
                    }
                    else
                    {
                        int columnIndex = candidateCells[0].ColumnIndex;
                        removeValueRange = emptyBlockCells.Select(x => x.ColumnIndex != columnIndex && x.ProbableValues.Contains(v));
                    }

                    if (removeValueRange.Count > 0)
                    {
                        int clearCount = 0;
                        foreach (Cell cell in removeValueRange)
                        {
                            if (cell.ProbableValues.Remove(v))
                            {
                                clearCount++;
                            }
                        }

                        return clearCount > 0;
                    }
                }
            }

            return false;
        }


        private static bool NakedSet(Range range, int maxDepth)
        {
            // если в диапазоне встречаются ячейки с одинаковыми возможными значениями, 
            // соответственно эти значения могут быть только у этих ячеек

            Range emptyCells = range.SelectEmptyCells();

            for (int i = 0; i < emptyCells.Count; i++)
            {
                Cell cell = emptyCells[i];

                Range containingCells = emptyCells.Select(x => x != cell && cell.ProbableIsContaining(x));
                if (containingCells.Count > 0)
                {
                    containingCells.Add(cell);
                    if (containingCells.Count == cell.ProbableValues.Count)
                    {
                        if (containingCells.Count <= maxDepth)
                        {
                            int clearCount = 0;

                            Range filteredCells = emptyCells.Select(x => !containingCells.Contains(x));
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
                }
            }

            return false;
        }

        private static bool HiddenSet(Range range, int maxDepth)
        {
            return false;
        }
    }
}
