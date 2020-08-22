
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SudokuSolver
{
    internal static class StrategyHelper
    {
        public static StrategyInfo[] GetStrategies()
        {
            List<StrategyInfo> strategies = new List<StrategyInfo>();

            MethodInfo[] methods = typeof(StrategyHelper).GetMethods(BindingFlags.Public | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attrs = method.GetCustomAttributes(typeof(StrategyAttribute), false);
                if (attrs.Length > 0)
                {
                    StrategyAttribute attr = (StrategyAttribute)attrs[0];
                    StrategyMethod strategyMethod = (StrategyMethod)Delegate.CreateDelegate(typeof(StrategyMethod), method);
                    StrategyInfo strategyInfo = new StrategyInfo(attr.StrategyType, strategyMethod, attr.StrategyArea);
                    strategies.Add(strategyInfo);
                }
            }

            return strategies.ToArray();
        }


        public static StrategyInfo GetInitializeProbableValuesStrategy()
        {
            return new StrategyInfo(StrategyType.InitializeProbableValues, InitializeProbableValues, StrategyArea.Table);
        }

        public static void InitializeProbableValues(StrategyResult result, Range range)
        {
            Table table = range.Table;

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

            result.Success = true;
        }


        [Strategy(StrategyType.SetValueToSolvedCell, StrategyArea.Table)]
        public static void SetValueToSolvedCell(StrategyResult result, Range range)
        {
            Cell cell = range.Find(x => x.ProbableValues.Count == 1);

            if (cell != null)
            {
                int value = cell.ProbableValues.ToArray()[0];
                cell.Value = value;
                cell.ProbableValues.Clear();

                Range block = range.Select(x => x != cell && x.BlockIndex == cell.BlockIndex);
                RemoveProbableValues(block, value);

                Range row = range.Select(x => x != cell && x.RowIndex == cell.RowIndex);
                RemoveProbableValues(row, value);

                Range column = range.Select(x => x != cell && x.ColumnIndex == cell.ColumnIndex);
                RemoveProbableValues(column, value);

                result.Success = true;
            }
        }

        [Strategy(StrategyType.HiddenSingles, StrategyArea.Block | StrategyArea.Line)]
        public static void HiddenSingles(StrategyResult result, Range range)
        {
            // https://www.sudokuwiki.org/Getting_Started

            // если в диапазоне из возможных значений ячейки находится то, которое не повторяется 
            // в других ячейках, то единственным вариантом для ячейки будет именно это значение

            Range emptyCells = range.SelectEmptyCells();
            foreach (int pVal in emptyCells.GetProbableValuesHashSet())
            {
                Range candidateRange = emptyCells.Select(x => x.ProbableValues.Contains(pVal));
                if (candidateRange.Count == 1)
                {
                    Cell cell = candidateRange[0];
                    cell.ProbableValues.Clear();
                    cell.ProbableValues.Add(pVal);
                    result.Success = true;
                    break;
                }
            }
        }

        [Strategy(StrategyType.NakedPairsTriples, StrategyArea.Block | StrategyArea.Line)]
        public static void NakedPairsTriples(StrategyResult result, Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NP

            NakedStrategy(result, range, 3);
        }

        [Strategy(StrategyType.HiddenPairsTriples, StrategyArea.Block | StrategyArea.Line)]
        public static void HiddenPairsTriples(StrategyResult result, Range range)
        {
            // https://www.sudokuwiki.org/Hidden_Candidates#HP

            HiddenStrategy(result, range, 3);
        }

        [Strategy(StrategyType.NakedQuards, StrategyArea.Block | StrategyArea.Line)]
        public static void NakedQuards(StrategyResult result, Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NQ

            NakedStrategy(result, range, 4);
        }

        [Strategy(StrategyType.HiddenQuards, StrategyArea.Block | StrategyArea.Line)]
        public static void HiddenQuards(StrategyResult result, Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NQ

            HiddenStrategy(result, range, 4);
        }

        [Strategy(StrategyType.PointingPairs, StrategyArea.Block)]
        public static void PointingPairs(StrategyResult result, Range range)
        {
            // https://www.sudokuwiki.org/Intersection_Removal#IR

            int blockIndex = range[0].BlockIndex;
            Table table = range.Table;
            Range emptyCells = range.SelectEmptyCells();

            foreach (int pValue in emptyCells.GetProbableValuesHashSet())
            {
                Range checkRange = emptyCells.Select(x => x.ProbableValues.Contains(pValue));
                Range candidateCells = null;

                HashSet<int> rows = checkRange.GetRowsHashSet();
                if (rows.Count == 1)
                {
                    int r = checkRange[0].RowIndex;
                    candidateCells = table.SelectRow(r).Select(x => x.BlockIndex != blockIndex);
                }
                else
                {
                    HashSet<int> columns = checkRange.GetColumnsHashSet();
                    if (columns.Count == 1)
                    {
                        int c = checkRange[0].ColumnIndex;
                        candidateCells = table.SelectColumn(c).Select(x => x.BlockIndex != blockIndex);
                    }
                }

                if (candidateCells != null)
                {
                    result.Success = RemoveProbableValues(candidateCells, pValue) > 0;
                    return;
                }
            }
        }

        [Strategy(StrategyType.BoxLineReduction, StrategyArea.Block)]
        public static void BoxLineReduction(StrategyResult result, Range range)
        {
            // https://www.sudokuwiki.org/Intersection_Removal#LBR

            int blockIndex = range[0].BlockIndex;
            Table table = range.Table;

            Range emptyBlockCells = range.SelectEmptyCells();
            foreach (int pValue in emptyBlockCells.GetProbableValuesHashSet())
            {
                int hits = 0;
                bool rowOrColumn = false;
                Range candidateCells = null;

                // Проверка строк
                foreach (int r in emptyBlockCells.GetRowsHashSet())
                {
                    // диапазон возможного значения, которое находится в строке рассматриваемого блока
                    Range probableBlockCells = emptyBlockCells.Select(x => x.RowIndex == r && x.ProbableValues.Contains(pValue));
                    if (probableBlockCells.Count > 0)
                    {
                        // ячейки из других блоков, которые могут содержать вероятное значение
                        Range otherCells = table.SelectRow(r).Select(x => x.BlockIndex != blockIndex && x.ContainsAnyValue(pValue));
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
                    Range probableBlockCells = emptyBlockCells.Select(x => x.ColumnIndex == c && x.ProbableValues.Contains(pValue));
                    if (probableBlockCells.Count > 0)
                    {
                        // ячейки из других блоков, которые могут содержать вероятное значение
                        Range otherCells = table.SelectColumn(c).Select(x => x.BlockIndex != blockIndex && x.ContainsAnyValue(pValue));
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
                        removeValueRange = emptyBlockCells.Select(x => x.RowIndex != rowIndex && x.ProbableValues.Contains(pValue));
                    }
                    else
                    {
                        int columnIndex = candidateCells[0].ColumnIndex;
                        removeValueRange = emptyBlockCells.Select(x => x.ColumnIndex != columnIndex && x.ProbableValues.Contains(pValue));
                    }

                    if (removeValueRange.Count > 0)
                    {
                        result.Success = RemoveProbableValues(removeValueRange, pValue) > 0;
                        return;
                    }
                }
            }
        }

        [Strategy(StrategyType.XWing, StrategyArea.Line)]
        public static void XWing(StrategyResult result, Range range)
        {
            XWingStrategy(result, range, 2);
        }

        [Strategy(StrategyType.YWing, StrategyArea.Line)]
        public static void YWing(StrategyResult result, Range range)
        {

        }

        [Strategy(StrategyType.Swordfish, StrategyArea.Line)]
        public static void Swordfish(StrategyResult result, Range range)
        {
            XWingStrategy(result, range, 3);
        }


        private static void NakedStrategy(StrategyResult result, Range range, int maxDepth)
        {
            // если в диапазоне встречаются ячейки с одинаковыми возможными значениями, 
            // соответственно эти значения могут быть только у этих ячеек

            Range emptyCells = range.SelectEmptyCells();
            foreach (Cell cell in emptyCells)
            {
                Range containingCells = emptyCells.Select(x => x != cell && cell.ContainsAllValues(x.ProbableValues));
                if (containingCells.Count > 0)
                {
                    containingCells.Add(cell);
                    if (containingCells.Count == cell.ProbableValues.Count)
                    {
                        if (containingCells.Count <= maxDepth)
                        {
                            Range candidateCells = emptyCells.Select(x => !containingCells.Contains(x));
                            result.Success = RemoveProbableValues(candidateCells, cell.ProbableValues.ToArray()) > 0;
                            return;
                        }
                    }
                }
            }
        }

        private static void HiddenStrategy(StrategyResult result, Range range, int maxDepth)
        {
            Range emptyCells = range.SelectEmptyCells();
            int[] probableValues = emptyCells.GetProbableValuesHashSet().ToArray();

            Range findRange = null;
            int[] findCombination = null;
            for (int combinationLength = 2; combinationLength <= maxDepth; combinationLength++)
            {
                IterateCombinations(probableValues, combinationLength, (combination) =>
                {
                    if (findRange == null)
                    {
                        Range candidateRange = emptyCells.Select(x => x.ContainsAnyValue(combination));
                        if (combinationLength == candidateRange.Count)
                        {
                            findRange = candidateRange;
                            findCombination = combination;
                        }
                    }
                });
            }

            if (findRange != null)
            {
                result.Success = KeepProbableValues(findRange, findCombination) > 0;
            }
        }

        private static void XWingStrategy(StrategyResult result, Range range, int lineLimit)
        {
            Table table = range.Table;
            Range emptyCells = range.SelectEmptyCells();

            if (emptyCells.Count == lineLimit)
            {
                if (emptyCells.IsRow)
                {
                    HashSet<int> checkColumns = emptyCells.GetColumnsHashSet();
                    int rowIndex = emptyCells.GetRowsHashSet().ToArray()[0];

                    foreach (int pValue in emptyCells.GetProbableValuesHashSet())
                    {
                        Range checkRange = table.Select(x => x.RowIndex != rowIndex && x.ContainsAllValues(pValue));

                        HashSet<int> checkRows = new HashSet<int>();
                        checkRows.Add(rowIndex);
                        foreach (int r in checkRange.GetRowsHashSet())
                        {
                            Range checkRowsCells = checkRange.Select(x => x.RowIndex == r && x.ContainsAllValues(pValue));
                            if (checkRowsCells.Count == lineLimit)
                            {
                                checkRowsCells = checkRowsCells.Select(x => checkColumns.Contains(x.ColumnIndex));
                                if (checkRowsCells.Count == lineLimit)
                                {
                                    checkRows.Add(r);
                                }
                            }
                        }

                        if (checkRows.Count == lineLimit)
                        {
                            Range candidateRange = table.Select(x => !checkRows.Contains(x.RowIndex) && checkColumns.Contains(x.ColumnIndex) && x.ContainsAllValues(pValue));
                            result.Success = RemoveProbableValues(candidateRange, pValue) > 0;
                            return;
                        }
                    }
                }
                else
                {
                    HashSet<int> checkRows = emptyCells.GetRowsHashSet();
                    int columnIndex = emptyCells.GetColumnsHashSet().ToArray()[0];

                    foreach (int pValue in emptyCells.GetProbableValuesHashSet())
                    {
                        Range checkRange = table.Select(x => x.ColumnIndex != columnIndex && x.ContainsAllValues(pValue));

                        HashSet<int> checkColumns = new HashSet<int>();
                        checkColumns.Add(columnIndex);
                        foreach (int c in checkRange.GetColumnsHashSet())
                        {
                            Range checkColumnCells = checkRange.Select(x => x.ColumnIndex == c && x.ContainsAllValues(pValue));
                            if (checkColumnCells.Count == lineLimit)
                            {
                                checkColumnCells = checkColumnCells.Select(x => checkRows.Contains(x.RowIndex));
                                if (checkColumnCells.Count == lineLimit)
                                {
                                    checkColumns.Add(c);
                                }
                            }
                        }

                        if (checkColumns.Count == lineLimit)
                        {
                            Range candidateRange = table.Select(x => checkRows.Contains(x.RowIndex) && !checkColumns.Contains(x.ColumnIndex) && x.ContainsAllValues(pValue));
                            result.Success = RemoveProbableValues(candidateRange, pValue) > 0;
                            return;
                        }
                    }
                }
            }
        }


        private static int RemoveProbableValues(Range range, params int[] removedValues)
        {
            int removedCount = 0;

            foreach (Cell cell in range)
            {
                foreach (int pValue in removedValues)
                {
                    if (cell.ProbableValues.Remove(pValue))
                    {
                        removedCount++;
                    }
                }
            }

            return removedCount;
        }

        private static int KeepProbableValues(Range range, params int[] keepValues)
        {
            HashSet<int> keepHashSet = new HashSet<int>(keepValues);

            int removedCount = 0;

            foreach (Cell cell in range)
            {
                foreach (int pValue in cell.ProbableValues.ToArray())
                {
                    if (!keepHashSet.Contains(pValue))
                    {
                        if (cell.ProbableValues.Remove(pValue))
                        {
                            removedCount++;
                        }
                    }
                }
            }

            return removedCount;
        }

        private static void FilterInitialProbableValues(Range range)
        {
            HashSet<int> existsValues = range.GetValuesHashSet();
            Range emptyCells = range.SelectEmptyCells();
            RemoveProbableValues(emptyCells, existsValues.ToArray());
        }

        // Перебор возможных комбинаций неповторяющихся значений из массива 
        private static void IterateCombinations(int[] values, int combinationLength, CombinationDelegate action)
        {
            int[] combination = new int[combinationLength];
            IterateCombinationsInternal(values, combination, 0, -1, action);
        }

        private static void IterateCombinationsInternal(int[] values, int[] combination, int level, int prevPosition, CombinationDelegate action)
        {
            int startPosition = prevPosition + 1;
            int endPosition = values.Length - (combination.Length - level - 1);

            for (int i = startPosition; i < endPosition; i++)
            {
                int value = values[i];
                combination[level] = value;

                if (level < combination.Length - 1)
                {
                    IterateCombinationsInternal(values, combination, level + 1, i, action);
                }
                else
                {
                    int[] combinationCopy = new int[combination.Length];
                    Array.Copy(combination, combinationCopy, combination.Length);
                    action(combinationCopy);
                }
            }
        }

        private delegate void CombinationDelegate(int[] combination);
    }
}
