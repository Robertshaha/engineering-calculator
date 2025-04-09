using System;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EngineeringCalculator
{
    public partial class MainWindow : Window
    {
        private bool _isNewCalculation = true;
        private bool _isError = false;
        private bool _lastInputWasOperator = false;

        public MainWindow()
        {
            InitializeComponent();
            ClearAll();
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isError) ClearAll();

            var button = (Button)sender;
            var number = button.Content.ToString();

            if (_isNewCalculation || ResultTextBox.Text == "0")
            {
                ResultTextBox.Text = number;
                _isNewCalculation = false;
            }
            else
            {
                if (!(number == "0" && ResultTextBox.Text == "0"))
                {
                    ResultTextBox.Text += number;
                }
            }

            _lastInputWasOperator = false;
            UpdateHistory();
        }

        private void OperatorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isError) return;

            var button = (Button)sender;
            var op = button.Content.ToString();

            if (_lastInputWasOperator && op != "(")
            {
                ResultTextBox.Text = ResultTextBox.Text.Substring(0, ResultTextBox.Text.Length - 1) + op;
            }
            else
            {
                if (op == "(" && !_isNewCalculation && !IsLastCharacterOperator(ResultTextBox.Text))
                {
                    op = "×" + op;
                }
                ResultTextBox.Text += op;
            }

            _lastInputWasOperator = op != ")";
            _isNewCalculation = false;
            UpdateHistory();
        }

        private bool IsLastCharacterOperator(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            var lastChar = text[text.Length - 1];
            return "+-×÷^(".Contains(lastChar.ToString());
        }

        private void FunctionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isError) return;

            var button = (Button)sender;
            var function = button.Content.ToString();

            switch (function)
            {
                case "sin":
                case "cos":
                case "tan":
                    ResultTextBox.Text += function + "(";
                    break;
                case "x²":
                    ResultTextBox.Text += "^2";
                    break;
                case "√x":
                    ResultTextBox.Text += "√(";
                    break;
                case "x^y":
                    ResultTextBox.Text += "^";
                    break;
                case "10^x":
                    ResultTextBox.Text += "10^";
                    break;
                case "log":
                    ResultTextBox.Text += "log(";
                    break;
                case "ln":
                    ResultTextBox.Text += "ln(";
                    break;
                case "n!":
                    CalculateFactorial();
                    break;
                case "|x|":
                    ResultTextBox.Text = Math.Abs(ParseCurrentNumber()).ToString(CultureInfo.CurrentCulture);
                    break;
                case "1/x":
                    CalculateInverse();
                    break;
            }

            _lastInputWasOperator = false;
            UpdateHistory();
        }

        private void CalculateInverse()
        {
            if (double.TryParse(ResultTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var num))
            {
                if (num == 0)
                {
                    ShowError("Деление на ноль");
                    return;
                }
                ResultTextBox.Text = (1.0 / num).ToString(CultureInfo.CurrentCulture);
            }
        }

        private void CalculateFactorial()
        {
            if (double.TryParse(ResultTextBox.Text, out var num))
            {
                try
                {
                    ResultTextBox.Text = Factorial(num).ToString(CultureInfo.CurrentCulture);
                }
                catch (ArgumentException ex)
                {
                    ShowError(ex.Message);
                }
            }
        }

        private double Factorial(double n)
        {
            if (n < 0) throw new ArgumentException("Факториал отрицательного числа не определен");
            if (n % 1 != 0) throw new ArgumentException("Факториал только для целых чисел");
            if (n > 170) return double.PositiveInfinity;
            
            double result = 1;
            for (int i = 2; i <= n; i++)
            {
                result *= i;
            }
            return result;
        }

        private void ConstantButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isError) return;

            var button = (Button)sender;
            var constant = button.Content.ToString();

            if (_isNewCalculation)
            {
                ResultTextBox.Text = constant == "π" ? Math.PI.ToString(CultureInfo.CurrentCulture) : Math.E.ToString(CultureInfo.CurrentCulture);
                _isNewCalculation = false;
            }
            else
            {
                ResultTextBox.Text += constant == "π" ? Math.PI.ToString(CultureInfo.CurrentCulture) : Math.E.ToString(CultureInfo.CurrentCulture);
            }

            UpdateHistory();
        }

        private void CommaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isError) return;

            if (_isNewCalculation)
            {
                ResultTextBox.Text = "0,";
                _isNewCalculation = false;
            }
            else if (!ResultTextBox.Text.Contains(","))
            {
                ResultTextBox.Text += ",";
            }

            UpdateHistory();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isError || string.IsNullOrEmpty(ResultTextBox.Text)) return;

            ResultTextBox.Text = ResultTextBox.Text.Substring(0, ResultTextBox.Text.Length - 1);

            if (ResultTextBox.Text.Length == 0)
            {
                ResultTextBox.Text = "0";
                _isNewCalculation = true;
            }

            UpdateHistory();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
        }

        private void EqualsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isError || string.IsNullOrEmpty(ResultTextBox.Text)) return;

            try
            {
                var expression = PrepareExpression(ResultTextBox.Text);
                var result = EvaluateExpression(expression);
                
                HistoryTextBox.Text = ResultTextBox.Text + " =";
                ResultTextBox.Text = result.ToString(CultureInfo.CurrentCulture);
                _isNewCalculation = true;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private string PrepareExpression(string input)
        {
            var expression = input
                .Replace("×", "*")
                .Replace("÷", "/")
                .Replace(",", ".")
                .Replace("π", Math.PI.ToString(CultureInfo.InvariantCulture))
                .Replace("e", Math.E.ToString(CultureInfo.InvariantCulture));
                
            expression = Regex.Replace(expression, @"(sin|cos|tan)\(", 
                m => $"Math.{m.Groups[1].Value}(Math.PI/180*");

            expression = expression
                .Replace("√(", "Math.Sqrt(")
                .Replace("log(", "Math.Log10(")
                .Replace("ln(", "Math.Log(")
                .Replace("abs(", "Math.Abs(");

            if (CountCharacter(expression, '(') != CountCharacter(expression, ')'))
            {
                throw new ArgumentException("Несбалансированные скобки");
            }

            return expression;
        }

        private static int CountCharacter(string str, char ch)
        {
            return str.Count(c => c == ch);
        }

        private double EvaluateExpression(string expression)
        {
            try
            {
                var result = new DataTable().Compute(expression, null);
                return Convert.ToDouble(result);
            }
            catch (SyntaxErrorException)
            {
                throw new ArgumentException("Синтаксическая ошибка");
            }
            catch (EvaluateException)
            {
                throw new ArgumentException("Ошибка вычисления");
            }
            catch (OverflowException)
            {
                throw new ArgumentException("Переполнение");
            }
        }

        private double ParseCurrentNumber()
        {
            if (double.TryParse(ResultTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var num))
            {
                return num;
            }
            throw new ArgumentException("Некорректное число");
        }

        private void ShowError(string message)
        {
            HistoryTextBox.Text = "Ошибка";
            ResultTextBox.Text = message;
            _isError = true;
        }

        private void UpdateHistory()
        {
            HistoryTextBox.Text = ResultTextBox.Text;
        }

        private void ClearAll()
        {
            ResultTextBox.Text = "0";
            HistoryTextBox.Text = "";
            _isNewCalculation = true;
            _isError = false;
        }
    }
}
