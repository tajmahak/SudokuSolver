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

            sudoku.Load(File.ReadAllLines("test1.txt"));

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
                ShowStage(stage);
            }
        }

        private void ShowStage(Stage stage)
        {
            stageTypeLbl.Text = stage.StrategyType.ToString();

            Table table = stage.Table;

            int labelCounter = 1;
            for (int r = 0; r < table.Length; r++)
            {
                for (int c = 0; c < table.Length; c++)
                {
                    Cell cell = table[r, c];

                    Label label = (Label)tableLayoutPanel1.Controls["label" + labelCounter];
                    labelCounter++;

                    SetLabel(label, cell);
                }
            }
        }

        private void SetLabel(Label label, Cell cell)
        {
            if (cell.Value == null)
            {
                label.ForeColor = Color.Gray;
                label.Font = new Font(label.Font.FontFamily, 9);

                if (cell.ProbableValues.Count == 0)
                {
                    label.Text = "";
                }
                else
                {
                    StringBuilder str = new StringBuilder();
                    int index = 0;
                    foreach (int pVal in cell.ProbableValues)
                    {
                        if (index > 0)
                        {
                            str.Append(' ');
                        }
                        str.Append(pVal);
                        index++;
                    }

                    label.Text = str.ToString();
                }
            }
            else
            {
                if (cell.IsDefault)
                {
                    label.ForeColor = Color.Black;
                }
                else
                {
                    label.ForeColor = Color.DarkSlateBlue;
                }
                label.Font = new Font(label.Font.FontFamily, 20);
                label.Text = cell.Value.Value.ToString();
            }
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
    }
}
