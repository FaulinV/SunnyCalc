using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Sunny.UI; // voor de mooie knoppies en dingentjes

namespace rekenmachineProject
{
    public partial class Rekenmachine : UIForm
    {
        private string expr = "";
        private bool justEvaluated = false;
        private readonly string decSep;

        public Rekenmachine()
        {
            InitializeComponent();

            this.Style = UIStyle.Custom;
            this.TitleHeight = 36;
            this.TitleColor = Color.Blue;

            decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            lblDisplay.Text = "0";

            WireAllButtons(this);
        }

        private void WireAllButtons(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is UIButton b)
                {
                    string t = (b.Text ?? "").Trim();

                    if (t.Length == 1 && char.IsDigit(t[0])) b.Click += Digit_Click;
                    else if (t == "," || t == ".") b.Click += Dot_Click;
                    else if (t == "+" || t == "-" || t == "×" || t == "x" || t == "*" || t == "÷" || t == "/")
                    { b.Tag = NormalizeOperator(t); b.Click += Operator_Click; }
                    else if (t == "=") b.Click += Equals_Click;
                    else if (t.Equals("AC", StringComparison.OrdinalIgnoreCase)) b.Click += AC_Click;
                    else if (t.Equals("C", StringComparison.OrdinalIgnoreCase)) b.Click += C_Click;
                    else if (t == "⌫" || t == "←" || t.Equals("Back", StringComparison.OrdinalIgnoreCase)) b.Click += Backspace_Click;
                }
                if (c.HasChildren) WireAllButtons(c);
            }
        }

        private string NormalizeOperator(string t)
        {
            switch (t)
            {
                case "+": return "+";
                case "-": return "-";
                case "×":
                case "x":
                case "X":
                case "*": return "×";
                case "÷":
                case "/": return "÷";
                default: return null;
            }
        }

        private void Digit_Click(object sender, EventArgs e)
        {
            var d = ((UIButton)sender).Text;

            if (justEvaluated) { expr = ""; justEvaluated = false; }

            if (expr == "0") expr = d;
            else expr += d;

            UpdateDisplay();
        }

        private void Dot_Click(object sender, EventArgs e)
        {
            if (justEvaluated) { expr = ""; justEvaluated = false; }

            string lastNumber = GetLastNumberSegment();
            if (lastNumber.Contains(decSep)) return;

            if (expr.Length == 0 || IsLastCharOperator())
                expr += "0" + decSep;
            else
                expr += decSep;

            UpdateDisplay();
        }

        private void Operator_Click(object sender, EventArgs e)
        {
            var op = (string)(((UIButton)sender).Tag ?? NormalizeOperator(((UIButton)sender).Text.Trim()));
            if (op == null) return;

            justEvaluated = false;

            if (expr.Length == 0)
            {
                if (op == "-") { expr = "-"; UpdateDisplay(); }
                return;
            }

            if (IsLastCharOperator())
            {
                expr = expr.Substring(0, expr.Length - 1) + op;
            }
            else if (expr.EndsWith(decSep))
            {
                return;
            }
            else
            {
                expr += op;
            }

            UpdateDisplay();
        }

        private void Equals_Click(object sender, EventArgs e)
        {
            if (expr.Length == 0) return;
            if (IsLastCharOperator() || expr.EndsWith(decSep)) return;

            var ok = TryEvaluate(expr, out string result);
            if (ok)
            {
                expr = result;
                justEvaluated = true;
                UpdateDisplay();
            }
            else
            {
                lblDisplay.Text = "Error";
                justEvaluated = true;
            }
        }

        private void AC_Click(object sender, EventArgs e)
        {
            expr = "";
            justEvaluated = false;
            UpdateDisplay();
        }

        private void C_Click(object sender, EventArgs e)
        {
            int cut = LastOperatorIndex();
            expr = cut >= 0 ? expr.Substring(0, cut + 1) : "";
            UpdateDisplay();
        }

        private void Backspace_Click(object sender, EventArgs e)
        {
            if (expr.Length == 0 || justEvaluated)
            {
                expr = "";
                UpdateDisplay();
                justEvaluated = false;
                return;
            }
            expr = expr.Substring(0, expr.Length - 1);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            lblDisplay.Text = expr.Length == 0 ? "0" : expr;
        }

        private bool IsLastCharOperator()
        {
            if (expr.Length == 0) return false;
            char ch = expr[expr.Length - 1];
            return ch == '+' || ch == '-' || ch == '×' || ch == '÷';
        }

        private int LastOperatorIndex()
        {
            int i1 = expr.LastIndexOf('+');
            int i2 = expr.LastIndexOf('-');
            int i3 = expr.LastIndexOf('×');
            int i4 = expr.LastIndexOf('÷');
            return Math.Max(Math.Max(i1, i2), Math.Max(i3, i4));
        }

        private string GetLastNumberSegment()
        {
            int cut = LastOperatorIndex();
            return cut >= 0 ? expr.Substring(cut + 1) : expr;
        }

        private bool TryEvaluate(string input, out string result)
        {
            try
            {
                string s = input.Replace('×', '*').Replace('÷', '/');
                if (decSep == ",") s = s.Replace(',', '.');

                var dt = new DataTable();
                dt.Locale = CultureInfo.InvariantCulture;
                object val = dt.Compute(s, null);

                string text = Convert.ToDouble(val, CultureInfo.InvariantCulture)
                                  .ToString("G15", CultureInfo.CurrentCulture);

                result = text;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
