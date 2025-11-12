using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Sunny.UI;

namespace rekenmachineProject
{
    public partial class Rekenmachine : UIForm
    {
        // huidige invoer
        private string expr = "";
        // net "=" gedrukt?
        private bool justEvaluated = false;
        // culture decimaal teken
        private readonly string decSep;

        public Rekenmachine()
        {
            InitializeComponent();

            this.Style = UIStyle.Custom;
            this.TitleHeight = 36;
            this.TitleColor = Color.Blue;

            decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            displayLabel.Text = "0";
            WireAllButtons(this);
        }

        // events koppelen op basis van Text
        private void WireAllButtons(Control root)
        {
            foreach (Control c in root.Controls)
            {
                var b = c as UIButton;
                if (b != null)
                {
                    string t = (b.Text ?? "").Trim();

                    if (t.Length == 1 && char.IsDigit(t[0])) b.Click += Digit_Click;
                    else if (t == "," || t == ".") b.Click += Dot_Click;
                    else if (t == "+" || t == "-" || t == "×" || t == "x" || t == "X" || t == "*" || t == "÷" || t == "/")
                    { b.Tag = NormalizeOperator(t); b.Click += Operator_Click; }
                    else if (t == "=") b.Click += Equals_Click;
                    else if (t.Equals("AC", StringComparison.OrdinalIgnoreCase)) b.Click += AC_Click;
                    else if (t.Equals("C", StringComparison.OrdinalIgnoreCase)) b.Click += C_Click;
                    else if (t == "⌫" || t == "←" || t.Equals("Back", StringComparison.OrdinalIgnoreCase)) b.Click += Backspace_Click;
                }
                if (c.HasChildren) WireAllButtons(c);
            }
        }

        // operators normaliseren
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

        // cijfers
        private void Digit_Click(object sender, EventArgs e)
        {
            var d = ((UIButton)sender).Text;
            if (justEvaluated) { expr = ""; justEvaluated = false; }
            if (expr == "0") expr = d; else expr += d;
            UpdateDisplay();
        }

        // decimaal
        private void Dot_Click(object sender, EventArgs e)
        {
            if (justEvaluated) { expr = ""; justEvaluated = false; }
            string lastNumber = GetLastNumberSegment();
            if (lastNumber.IndexOf(decSep, StringComparison.Ordinal) >= 0) return;

            if (expr.Length == 0 || IsLastCharOperator()) expr += "0" + decSep;
            else expr += decSep;

            UpdateDisplay();
        }

        // operatoren
        private void Operator_Click(object sender, EventArgs e)
        {
            string op = (string)(((UIButton)sender).Tag ?? NormalizeOperator(((UIButton)sender).Text.Trim()));
            if (op == null) return;

            justEvaluated = false;

            if (expr.Length == 0)
            {
                if (op == "-") { expr = "-"; UpdateDisplay(); }
                return;
            }

            if (IsLastCharOperator())
                expr = expr.Substring(0, expr.Length - 1) + op;
            else if (expr.EndsWith(decSep)) return;
            else expr += op;

            UpdateDisplay();
        }

        // "="
        private void Equals_Click(object sender, EventArgs e)
        {
            if (expr.Length == 0) return;
            if (IsLastCharOperator() || expr.EndsWith(decSep)) return;

            try
            {
                string result = EvaluateExpression(expr, decSep);
                expr = result;
                justEvaluated = true;
                UpdateDisplay();
            }
            catch
            {
                displayLabel.Text = "Error";
                justEvaluated = true;
            }
        }

        // AC
        private void AC_Click(object sender, EventArgs e)
        {
            expr = "";
            justEvaluated = false;
            UpdateDisplay();
        }

        // C
        private void C_Click(object sender, EventArgs e)
        {
            int cut = LastOperatorIndex();
            expr = cut >= 0 ? expr.Substring(0, cut + 1) : "";
            UpdateDisplay();
        }

        // backspace
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

        // display updaten
        private void UpdateDisplay()
        {
            displayLabel.Text = expr.Length == 0 ? "0" : expr;
        }

        // laatste char operator?
        private bool IsLastCharOperator()
        {
            if (expr.Length == 0) return false;
            char ch = expr[expr.Length - 1];
            return ch == '+' || ch == '-' || ch == '×' || ch == '÷';
        }

        // index van laatste operator
        private int LastOperatorIndex()
        {
            int i1 = expr.LastIndexOf('+');
            int i2 = expr.LastIndexOf('-');
            int i3 = expr.LastIndexOf('×');
            int i4 = expr.LastIndexOf('÷');
            return Math.Max(Math.Max(i1, i2), Math.Max(i3, i4));
        }

        // laatste getal
        private string GetLastNumberSegment()
        {
            int cut = LastOperatorIndex();
            return cut >= 0 ? expr.Substring(cut + 1) : expr;
        }

        // rekenen + locale
        private string EvaluateExpression(string input, string decimalSeparator)
        {
            string s = input.Replace('×', '*').Replace('÷', '/');
            if (decimalSeparator == ",") s = s.Replace(',', '.');

            var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
            object val = dt.Compute(s, null);

            return Convert.ToDouble(val, CultureInfo.InvariantCulture)
                         .ToString("G15", CultureInfo.CurrentCulture);
        }
        private void displayLabel_Click(object sender, EventArgs e) { }
    }
}
