using MyLibrary.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SudokuSolver
{
    public partial class Form1 : Form
    {
        private readonly Sudoku sudoku = new Sudoku(3);

        private int currentNumber;

        public Form1()
        {
            InitializeComponent();

            ControlExtension.SetDoubleBuffer(this, true);
            for (int i = 1; i <= 81; i++)
            {
                Control richTextBox = tableLayoutPanel1.Controls["richTextBox" + i];
                ControlExtension.SetDoubleBuffer(richTextBox, true);
            }

            LoadStrategyList();

            LoadFileList();

            if (fileList.Items.Count > 0)
            {
                fileList.SelectedIndex = 0;
            }
        }


        private void LoadFileList()
        {
            fileList.Items.Clear();

            string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.txt");
            foreach (string file in files)
            {
                Item<string, string> fileItem = new Item<string, string>();
                fileItem.Key = Path.GetFileNameWithoutExtension(file);
                fileItem.Value = file;
                fileList.Items.Add(fileItem);
            }
        }

        public void LoadStrategyList()
        {
            StrategyInfo[] strategies = StrategyHelper.GetStrategies();

            strategyList.Items.Clear();

            foreach (StrategyInfo strategy in strategies)
            {
                Item<string, StrategyType> item = new Item<string, StrategyType>();
                item.Key = strategy.ToString();
                item.Value = strategy.StrategyType;

                strategyList.Items.Add(item);
            }
        }

        private void OpenFile(string path)
        {
            string[] fileData = File.ReadAllLines(path);
            if (fileData.Length > 2)
            {
                sudoku.LoadFromExcel(fileData);
            }
            else
            {
                sudoku.LoadFromSequence(fileData[0]);
            }

            stageNumber.Minimum = 1;
            stageNumber.Maximum = sudoku.Stages.Count;

            OpenStage((int)stageNumber.Maximum);
        }

        private void OpenStage(int number)
        {
            if (stageNumber.Minimum <= number && number <= stageNumber.Maximum)
            {
                currentNumber = number;
                stageNumber.Value = number;
                infoLabel.Text = $"{stageNumber.Value} / {stageNumber.Maximum}";
                Stage stage = sudoku.Stages[number - 1];
                ShowTable(stage, true);



                StrategyResult nextResult = null;
                if (currentNumber < sudoku.Stages.Count)
                {
                    nextResult = sudoku.Stages[currentNumber].Result;
                }

                ShowStrategy(nextResult);
            }
        }

        private void ShowTable(Stage stage, bool showResult)
        {
            Table table = stage.Table;

            int labelCounter = 1;
            for (int r = 0; r < table.Length; r++)
            {
                for (int c = 0; c < table.Length; c++)
                {
                    Cell cell = table[r, c];

                    RichTextBox control = (RichTextBox)tableLayoutPanel1.Controls["richTextBox" + labelCounter];
                    labelCounter++;

                    SetCell(control, cell, showResult);
                }
            }
        }

        private void ShowStrategy(StrategyResult result)
        {
            strategyList.SelectedIndex = -1;
            if (result != null)
            {
                for (int i = 0; i < strategyList.Items.Count; i++)
                {
                    Item<string, StrategyType> item = (Item<string, StrategyType>)strategyList.Items[i];
                    if (item.Value == result.StrategyType)
                    {
                        strategyList.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        private void SetCell(RichTextBox label, Cell cell, bool showResult)
        {
            label.Clear();
            label.BackColor = Color.White;

            if (cell.Value == null)
            {
                label.Font = new Font("Consolas", 9);
                label.ForeColor = Color.Gray;

                if (cell.ProbableValues.Count == 0)
                {
                    label.Text = "";
                }
                else
                {
                    label.Text = GetProbableValues(cell);
                }
            }
            else
            {
                label.Font = new Font("Consolas", 20);
                if (cell.IsDefault)
                {
                    label.ForeColor = Color.Black;
                }
                else
                {
                    label.ForeColor = Color.DarkSlateBlue;
                }

                label.Text = cell.Value.Value.ToString();
                label.Select(0, 1);
                label.SelectionAlignment = HorizontalAlignment.Center;
            }

            if (showResult && currentNumber < sudoku.Stages.Count)
            {
                StrategyResult nextResult = sudoku.Stages[currentNumber].Result; // currentNumber = index - 1

                Cell relationCell = nextResult.GetRelationCell(cell.RowIndex, cell.ColumnIndex);
                Cell affectedCell = nextResult.GetAffectedCell(cell.RowIndex, cell.ColumnIndex);
                Cell relationValueCell = nextResult.GetRelationValueCell(cell.RowIndex, cell.ColumnIndex);
                Cell removedValueCell = nextResult.GetRemovedValueCell(cell.RowIndex, cell.ColumnIndex);

                if (relationCell != null)
                {
                    label.BackColor = Color.Green;
                }
                else if (affectedCell != null)
                {
                    label.BackColor = Color.Yellow;
                }
                else if (relationValueCell != null)
                {
                    foreach (var value in relationValueCell.ProbableValues)
                    {
                        SelectProbableValue(label, value);
                        label.SelectionColor = Color.Black;
                        label.SelectionBackColor = Color.LightGreen ;
                    }
                }
                else if (removedValueCell != null)
                {
                    foreach (var value in removedValueCell.ProbableValues)
                    {
                        SelectProbableValue(label, value);
                        label.SelectionColor = Color.Black;
                        label.SelectionBackColor = Color.Pink;
                    }
                }
            }
        }

        private void SelectProbableValue(RichTextBox richTextBox, int value)
        {
            switch (value)
            {
                case 1: richTextBox.Select(0, 1); break;
                case 2: richTextBox.Select(2, 1); break;
                case 3: richTextBox.Select(4, 1); break;
                case 4: richTextBox.Select(6, 1); break;
                case 5: richTextBox.Select(8, 1); break;
                case 6: richTextBox.Select(10, 1); break;
                case 7: richTextBox.Select(12, 1); break;
                case 8: richTextBox.Select(14, 1); break;
                case 9: richTextBox.Select(16, 1); break;
            }
        }

        private string GetProbableValues(Cell cell)
        {
            StringBuilder str = new StringBuilder();

            for (int i = 1; i < 10; i++)
            {
                if (cell.ProbableValues.Contains(i))
                {
                    str.Append(i);
                }
                else
                {
                    str.Append(" ");
                }

                if (i != 9)
                {
                    if (i == 3 || i == 6)
                    {
                        str.Append("\r\n");
                    }
                    else
                    {
                        str.Append(" ");
                    }
                }
            }

            return str.ToString();
        }

        private static string GetExcelString(Table table)
        {
            StringBuilder str = new StringBuilder();

            for (int row = 0; row < table.Length; row++)
            {
                for (int column = 0; column < table.Length; column++)
                {
                    Cell cell = table[row, column];

                    if (cell.Value == null)
                    {
                        str.Append('[');
                        foreach (int pval in cell.ProbableValues)
                        {
                            str.Append(pval);
                        }
                        str.Append(']');
                    }
                    else
                    {
                        str.Append(cell.Value.Value);
                    }
                    str.Append('\t');
                }
                str.AppendLine();
            }

            return str.ToString();
        }

        private static string GetSeqString(Table table)
        {
            StringBuilder str = new StringBuilder();

            for (int row = 0; row < table.Length; row++)
            {
                for (int column = 0; column < table.Length; column++)
                {
                    Cell cell = table[row, column];

                    if (cell.Value == null)
                    {
                        str.Append("0");
                    }
                    else
                    {
                        str.Append(cell.Value.Value);
                    }
                }
            }

            return str.ToString();
        }


        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
            int x = tableLayoutPanel1.Width;
            int y = tableLayoutPanel1.Height;

            int x1 = x / 3;
            int x2 = x1 * 2 - 3;

            int y1 = y / 3;
            int y2 = y1 * 2 - 3;

            Pen pen = new Pen(Brushes.Gray, 3);
            e.Graphics.DrawLine(pen, x1, 0, x1, y);
            e.Graphics.DrawLine(pen, x2, 0, x2, y);

            e.Graphics.DrawLine(pen, 0, y1, x, y1);
            e.Graphics.DrawLine(pen, 0, y2, x, y2);
        }

        private void prevBtn_Click(object sender, System.EventArgs e)
        {
            OpenStage((int)(stageNumber.Value - 1));
        }

        private void nextBtn_Click(object sender, System.EventArgs e)
        {
            OpenStage((int)(stageNumber.Value + 1));
        }

        private void openBtn_Click(object sender, System.EventArgs e)
        {
            OpenStage((int)stageNumber.Value);
        }

        private void copyBtn_Click(object sender, System.EventArgs e)
        {
            Stage stage = sudoku.Stages[currentNumber - 1];
            string excelString = GetExcelString(stage.Table);

            Clipboard.SetText(excelString);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                e.SuppressKeyPress = e.Handled = true;
                prevBtn_Click(sender, e);
            }
            else if (e.KeyCode == Keys.Right)
            {
                e.SuppressKeyPress = e.Handled = true;
                nextBtn_Click(sender, e);
            }
        }

        private void copySeqBtn_Click(object sender, System.EventArgs e)
        {
            Stage stage = sudoku.Stages[currentNumber - 1];
            string seqString = GetSeqString(stage.Table);

            Clipboard.SetText(seqString);
        }

        private void fileList_SelectedIndexChanged(object sender, EventArgs e)
        {
            Item<string, string> fileItem = (Item<string, string>)fileList?.SelectedItem;
            if (fileItem != null)
            {
                OpenFile(fileItem.Value);
            }
        }
    }

    internal class Item<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public override string ToString()
        {
            return Key.ToString();
        }
    }
}
