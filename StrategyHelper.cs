
using System;
using System.Collections.Generic;
using System.Linq;
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
                int value = cell.ProbableValues.ToArray()[0];
                cell.Value = value;
                cell.ProbableValues.Clear();

                Range block = range.Select(x => x != cell && x.BlockIndex == cell.BlockIndex);
                RemoveProbableValues(block, value);

                Range row = range.Select(x => x != cell && x.RowIndex == cell.RowIndex);
                RemoveProbableValues(row, value);

                Range column = range.Select(x => x != cell && x.ColumnIndex == cell.ColumnIndex);
                RemoveProbableValues(column, value);

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
            foreach (int pVal in emptyCells.GetProbableValuesHashSet())
            {
                Range candidateRange = emptyCells.Select(x => x.ProbableValues.Contains(pVal));
                if (candidateRange.Count == 1)
                {
                    Cell cell = candidateRange[0];
                    cell.ProbableValues.Clear();
                    cell.ProbableValues.Add(pVal);
                    return true;
                }
            }

            return false;
        }

        [Strategy(StrategyType.NakedPairsTriples, StrategyArea.Block | StrategyArea.Line)]
        private static bool NakedPairsTriples(Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NP

            return NakedStrategy(range, 3);
        }

        [Strategy(StrategyType.HiddenPairsTriples, StrategyArea.Block | StrategyArea.Line)]
        private static bool HiddenPairsTriples(Range range)
        {
            // https://www.sudokuwiki.org/Hidden_Candidates#HP

            return HiddenStrategy(range, 3);
        }

        [Strategy(StrategyType.NakedQuards, StrategyArea.Block | StrategyArea.Line)]
        private static bool NakedQuards(Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NQ

            return NakedStrategy(range, 4);
        }

        [Strategy(StrategyType.HiddenQuards, StrategyArea.Block | StrategyArea.Line)]
        private static bool HiddenQuards(Range range)
        {
            // https://www.sudokuwiki.org/Naked_Candidates#NQ

            return HiddenStrategy(range, 4);
        }

        [Strategy(StrategyType.PointingPairs, StrategyArea.Block)]
        private static bool PointingPairs(Range range)
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
                    return RemoveProbableValues(candidateCells, pValue) > 0;
                }
            }

            return false;
        }

        [Strategy(StrategyType.BoxLineReduction, StrategyArea.Block)]
        private static bool BoxLineReduction(Range range)
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
                        return RemoveProbableValues(removeValueRange, pValue) > 0;
                    }
                }
            }

            return false;
        }

        [Strategy(StrategyType.XWing, StrategyArea.Line)]
        private static bool XWing(Range range)
        {
            Table table = range.Table;
            Range emptyCells = range.SelectEmptyCells();

            if (emptyCells.Count == 2)
            {
                HashSet<int> probableValues = emptyCells.GetProbableValuesHashSet();
                if (probableValues.Count == 2)
                {
                    bool isRowOrColumn = emptyCells.GetRowsHashSet().Count == 1;
                    Cell cell1 = emptyCells[0];
                    Cell cell2 = emptyCells[1];

                    Range candidateRange = null;

                    foreach (int pValue in probableValues)
                    {
                        if (isRowOrColumn)
                        {
                            for (int r = 0; r < table.Length; r++)
                            {
                                if (r != cell1.RowIndex)
                                {
                                    Range row = table.SelectRow(r).Select(x => x.ProbableValues.Contains(pValue));
                                    if (row.Count == 2)
                                    {
                                        Cell otherCell1 = row[0];
                                        Cell otherCell2 = row[1];
                                        if (cell1.ColumnIndex == otherCell1.ColumnIndex && cell2.ColumnIndex == otherCell2.ColumnIndex)
                                        {
                                            candidateRange = table.Select(x =>
                                                x != cell1 && x != cell2 && x != otherCell1 && x != otherCell2 &&
                                                (x.ColumnIndex == cell1.ColumnIndex || x.ColumnIndex == cell2.ColumnIndex) &&
                                                x.ProbableValues.Contains(pValue));
                                        }
                                    }



                                }
                            }


                        }
                        else
                        {


                        }
                    }






                }
            }

            return false;
        }




        private static bool NakedStrategy(Range range, int maxDepth)
        {
            // если в диапазоне встречаются ячейки с одинаковыми возможными значениями, 
            // соответственно эти значения могут быть только у этих ячеек

            Range emptyCells = range.SelectEmptyCells();
            foreach (Cell cell in emptyCells)
            {
                Range containingCells = emptyCells.Select(x => x != cell && cell.ProbableIsContaining(x));
                if (containingCells.Count > 0)
                {
                    containingCells.Add(cell);
                    if (containingCells.Count == cell.ProbableValues.Count)
                    {
                        if (containingCells.Count <= maxDepth)
                        {
                            Range filteredCells = emptyCells.Select(x => !containingCells.Contains(x));
                            return RemoveProbableValues(filteredCells, cell.ProbableValues.ToArray()) > 0;
                        }
                    }
                }
            }

            return false;
        }

        private static bool HiddenStrategy(Range range, int maxDepth)
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
                        Range candidateRange = emptyCells.GetJointRange111(combination);
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
                return KeepProbableValues(findRange, findCombination) > 0;
            }

            return false;
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
