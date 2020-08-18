
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
            foreach (int pVal in emptyCells.GetProbableValuesHashSet())
            {
                Range cells = emptyCells.Select(x => x.ProbableValues.Contains(pVal));
                if (cells.Count == 1)
                {
                    Cell cell = cells[0];
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
            Range allCells = range.Table.Cells;

            Range emptyCells = range.SelectEmptyCells();

            foreach (int pVal in emptyCells.GetProbableValuesHashSet())
            {
                Range valueRange = emptyCells.Select(x => x.ProbableValues.Contains(pVal));
                Range candidateCells = null;

                HashSet<int> rows = valueRange.GetRowsHashSet();
                if (rows.Count == 1)
                {
                    int r = valueRange[0].RowIndex;
                    candidateCells = allCells.Select(x => x.BlockIndex != blockIndex && x.RowIndex == r);
                }
                else
                {
                    HashSet<int> columns = valueRange.GetColumnsHashSet();
                    if (columns.Count == 1)
                    {
                        int c = valueRange[0].ColumnIndex;
                        candidateCells = allCells.Select(x => x.BlockIndex != blockIndex && x.ColumnIndex == c);
                    }
                }

                if (candidateCells != null)
                {
                    int removeCount = 0;
                    foreach (Cell cell in candidateCells)
                    {
                        if (cell.ProbableValues.Remove(pVal))
                        {
                            removeCount++;
                        }
                    }

                    return removeCount > 0;
                }
            }

            return false;
        }

        //[Strategy(StrategyType.BoxLineReduction, StrategyArea.Block)]
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
                        int removeCount = 0;
                        foreach (Cell cell in removeValueRange)
                        {
                            if (cell.ProbableValues.Remove(v))
                            {
                                removeCount++;
                            }
                        }

                        return removeCount > 0;
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

        private static bool HiddenStrategy(Range range, int maxDepth)
        {
            range = range.Table.SelectBlock(4);
            //range = range.Table.SelectRow(3);

            // ИМЕННО ТАКАЯ КОМБИНАЦИЯ БОЛЬШЕ НИГДЕ НЕ ВСТРЕЧАЕТСЯ
            // КОЛИЧЕСТВО ЦИФР В КОМБИНАЦИИ ДОЛЖНО СОВПАДАТЬ С КОЛ-ВОМ ЯЧЕЕК, В КОТОРЫХ ЭТА КОМБИНАЦИЯ ВСТРЕЧАЕТСЯ
            // Одна из ячеек должна быть однозначно полностью заполненной нужной комбинацией

            Range emptyCells = range.SelectEmptyCells();

            Dictionary<int, Range> valueDict = new Dictionary<int, Range>();
            foreach (var pVal in emptyCells.GetProbableValuesHashSet())
            {
                var valueRange = emptyCells.Select(x => x.ProbableValues.Contains(pVal));
                valueDict.Add(pVal, valueRange);
            }

            var valueDictList = valueDict.ToList();
            for (int i = 0; i < valueDictList.Count; i++)
            {
                var item = valueDictList[i];
                int valueCount = item.Value.Count;

                var aaa = valueDictList.FindAll(x => x.Value.Count > valueCount);

                if (valueCount > aaa.Count)
                {
                    valueDictList.RemoveAt(i);
                    i--;
                }
            }




            { }







            //foreach (Cell cell in emptyCells)
            //{
            //    Range containingCells = emptyCells.Select(x => cell.AnyProbableIsContaining(x));
            //    if (containingCells.Count > 0)
            //    {
            //        int maxCount = 0;
            //        int totalCount = 0;
            //        Dictionary<int, Range> valueDict111 = new Dictionary<int, Range>();
            //        foreach (int pVal in cell.ProbableValues)
            //        {
            //            Range valueRange = containingCells.Select(x => x.ProbableValues.Contains(pVal));
            //            if (valueRange.Count < containingCells.Count)
            //            {
            //                valueDict111.Add(pVal, valueRange);
            //                maxCount = Math.Max(maxCount, valueRange.Count);
            //                totalCount += valueRange.Count;
            //            }
            //        }

            //        if (maxCount <= valueDict111.Count)
            //        { 

            //        }



            //        { }

            //        //containingCells.Add(cell);





            //        //if (containingCells.Count == cell.ProbableValues.Count)
            //        //{
            //        //    if (containingCells.Count <= maxDepth)
            //        //    {
            //        //        int clearCount = 0;

            //        //        Range filteredCells = emptyCells.Select(x => !containingCells.Contains(x));
            //        //        foreach (Cell filteredCell in filteredCells)
            //        //        {
            //        //            foreach (int pValue in cell.ProbableValues)
            //        //            {
            //        //                if (filteredCell.ProbableValues.Remove(pValue))
            //        //                {
            //        //                    clearCount++;
            //        //                }
            //        //            }
            //        //        }

            //        //        return clearCount > 0;
            //        //    }
            //        //}
            //    }
            //}

            return false;












            //Range emptyCells = range.SelectEmptyCells();







            //Dictionary<int, Range> countDict = new Dictionary<int, Range>();
            //foreach (var dVal in valueDict)
            //{
            //    var rnggg = dVal.Value;

            //    if (countDict.ContainsKey(rnggg.Count))
            //    {
            //        var existsRange = countDict[rnggg.Count];
            //        existsRange = existsRange.GetUnionRange(rnggg);
            //        countDict[rnggg.Count] = existsRange;

            //    }
            //    else
            //    {
            //        countDict.Add(rnggg.Count, rnggg);
            //    }
            //}


            //var pValues = emptyCells.GetProbableValuesHashSet();
            //foreach (var pVal in pValues)
            //{
            //    var valueRange = emptyCells.Select(x => x.ProbableValues.Contains(pVal));
            //    if (valueRange.Count > 2)
            //    {
            //        var joint = valueRange.GetJointProbableValuesHashSet();
            //        if (valueRange.Count == joint.Count)
            //        {

            //        }
            //    }
            //}




        }




    }
}
