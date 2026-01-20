using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;

namespace ToolLuhnCore
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private TextBox txtPatterns;
        private Button btnRun;
        private ProgressBar progressBar;

        public MainForm()
        {
            this.Text = "CC Generator";
            this.Size = new Size(480, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Patterns input - full width, dark theme, no scrollbar
            txtPatterns = new TextBox() { 
                Top = 16, Left = 16, Width = 448, Height = 160, 
                Font = new Font("Consolas", 10F), 
                Multiline = true, 
                ScrollBars = ScrollBars.None,
                Text = "497465880504xxxx|03|2027|\n49740180558xxxxx|05|2028|",
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            btnRun = new Button() { 
                Text = "Generate", 
                Top = 188, Left = 16, Width = 448, Height = 36, 
                BackColor = Color.FromArgb(0, 150, 136), 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 121, 107);
            btnRun.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 77, 64);
            btnRun.Click += BtnRun_Click;

            progressBar = new ProgressBar() { 
                Top = 232, Left = 16, Width = 448, Height = 14,
                Style = ProgressBarStyle.Continuous
            };

            this.Controls.AddRange(new Control[] { txtPatterns, btnRun, progressBar });

            // Load icon
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                    this.Icon = new Icon(icoPath);
            }
            catch { }
        }

        private async void BtnRun_Click(object? sender, EventArgs e)
        {
            string patternsText = txtPatterns.Text.Trim();
            if (string.IsNullOrWhiteSpace(patternsText))
            {
                MessageBox.Show("Vui lòng nhập ít nhất 1 pattern!");
                return;
            }

            btnRun.Enabled = false;
            progressBar.Value = 0;

            await Task.Run(() =>
            {
                try
                {
                    GenerateBulkProcess(patternsText);
                }
                catch (Exception ex)
                {
                    Invoke((MethodInvoker)(() => MessageBox.Show("Lỗi: " + ex.Message)));
                }
            });

            btnRun.Enabled = true;
            progressBar.Value = 100;
        }

        private void GenerateBulkProcess(string patternsText)
        {
            string[] lines = patternsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> allResults = new HashSet<string>();
            List<string> errorDetails = new List<string>();
            int parseErrorCount = 0;
            
            int totalPatterns = lines.Length;
            int processedPatterns = 0;

            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                processedPatterns++;
                Invoke((MethodInvoker)(() => 
                {
                    progressBar.Value = (processedPatterns * 30) / totalPatterns;
                }));

                string pattern = line.Trim();
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: dòng trống, bỏ qua.");
                    continue;
                }

                // Parse pattern: "BINxxxx|MM|YYYY|" or "BINxxxxx|MM|YYYY|"
                string cardPattern;
                string mm = "01";
                string yyyy = "2028";

                // Split by pipe
                string[] parts = pattern.Split('|');
                if (parts.Length < 3)
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: '{pattern}' - Thiếu tham số MM|YYYY.");
                    continue;
                }

                cardPattern = parts[0].Trim();
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1])) 
                    mm = parts[1].Trim().PadLeft(2, '0');
                if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2])) 
                    yyyy = parts[2].Trim();

                // Validate MM and YYYY
                if (mm.Length != 2 || !(mm[0] >= '0' && mm[0] <= '9') || !(mm[1] >= '0' && mm[1] <= '9'))
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: '{pattern}' - MM không hợp lệ: '{mm}'.");
                    continue;
                }
                int mmVal = int.Parse(mm);
                if (mmVal < 1 || mmVal > 12)
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: '{pattern}' - Tháng ngoài phạm vi (01-12): '{mm}'.");
                    continue;
                }

                if (yyyy.Length != 4 || !int.TryParse(yyyy, out _))
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: '{pattern}' - YYYY không hợp lệ: '{yyyy}'.");
                    continue;
                }

                // Validate cardPattern chars (digits or x/X)
                bool patternCharsOk = true;
                for (int ci = 0; ci < cardPattern.Length; ci++)
                {
                    char c = cardPattern[ci];
                    if (!((c >= '0' && c <= '9') || c == 'x' || c == 'X'))
                    {
                        patternCharsOk = false;
                        break;
                    }
                }
                if (!patternCharsOk)
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: '{pattern}' - Chỉ cho phép số và 'x' trong phần BIN.");
                    continue;
                }

                // Find placeholders (xxxxx or xxxx)
                List<int> placeholderLengths = new List<int>();
                string tempPattern = cardPattern.ToLower();
                int idx = 0;
                
                while (idx < tempPattern.Length)
                {
                    if (tempPattern[idx] == 'x')
                    {
                        int xCount = 0;
                        while (idx < tempPattern.Length && tempPattern[idx] == 'x')
                        {
                            xCount++;
                            idx++;
                        }
                        placeholderLengths.Add(xCount);
                    }
                    else
                    {
                        idx++;
                    }
                }

                if (placeholderLengths.Count == 0)
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: '{pattern}' - Không có placeholder 'x', bỏ qua.");
                    continue;
                }

                // Calculate total combinations
                long combinations = 1;
                foreach (int len in placeholderLengths)
                {
                    combinations *= (long)Math.Pow(10, len);
                }
                
                GenerateForPattern(cardPattern, mm, yyyy, placeholderLengths, allResults);
            }

            // Convert to list for shuffling
            Invoke((MethodInvoker)(() => 
            {
                progressBar.Value = 70;
            }));

            List<string> finalResults = new List<string>(allResults);

            ShuffleList(finalResults);

            // Validate with Luhn and filter invalid if any
            Invoke((MethodInvoker)(() => 
            {
                progressBar.Value = 80;
            }));

            List<string> validResults = new List<string>(finalResults.Count);
            int invalidCount = 0;
            foreach (var line in finalResults)
            {
                var parts = line.Split('|');
                var num = parts.Length > 0 ? parts[0] : string.Empty;
                if (!string.IsNullOrEmpty(num) && IsLuhnValid(num))
                {
                    validResults.Add(line);
                }
                else
                {
                    invalidCount++;
                }
            }

            // Write to file
            Invoke((MethodInvoker)(() => 
            {
                progressBar.Value = 90;
            }));

            using (StreamWriter sw = new StreamWriter("cards.txt", false, Encoding.UTF8, 262144))
            {
                foreach (var card in validResults)
                {
                    if (!string.IsNullOrWhiteSpace(card))
                        sw.WriteLine(card);
                }
            }

            // Write detailed errors
            Invoke((MethodInvoker)(() => 
            {
                progressBar.Value = 95;
            }));

            using (StreamWriter err = new StreamWriter("errors.txt", false, Encoding.UTF8, 131072))
            {
                err.WriteLine($"==== Báo cáo lỗi chi tiết ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ====");
                err.WriteLine($"Tổng patterns: {totalPatterns}");
                err.WriteLine($"Hợp lệ ghi ra: {validResults.Count}");
                err.WriteLine($"Bị loại do Luhn: {invalidCount}");
                err.WriteLine($"Lỗi parse/định dạng: {parseErrorCount}");
                err.WriteLine();
                if (parseErrorCount > 0)
                {
                    err.WriteLine("-- Lỗi parse/định dạng --");
                    foreach (var e in errorDetails)
                    {
                        err.WriteLine(e);
                    }
                    err.WriteLine();
                }
                if (invalidCount > 0)
                {
                    err.WriteLine("-- Các dòng bị loại do không đạt Luhn --");
                    foreach (var line in finalResults)
                    {
                        var parts = line.Split('|');
                        var num = parts.Length > 0 ? parts[0] : string.Empty;
                        if (string.IsNullOrEmpty(num) || !IsLuhnValid(num))
                        {
                            err.WriteLine(line);
                        }
                    }
                }
            }

            // Build a concise sample of errors for popup
            string sampleErrors = string.Empty;
            if (errorDetails.Count > 0)
            {
                int maxSamples = errorDetails.Count < 5 ? errorDetails.Count : 5;
                for (int si = 0; si < maxSamples; si++)
                {
                    sampleErrors += "- " + errorDetails[si] + "\n";
                }
            }

            Invoke((MethodInvoker)(() =>
            {
                progressBar.Value = 100;

                string msg = $"Xong!\nHợp lệ: {validResults.Count:N0}\nBị loại Luhn: {invalidCount:N0}\nLỗi định dạng: {parseErrorCount:N0}";
                if (parseErrorCount > 0)
                {
                    msg += "\n\nVí dụ lỗi (tối đa 5):\n" + sampleErrors.TrimEnd();
                }

                MessageBox.Show(msg, "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));
        }

        private void GenerateForPattern(string pattern, string mm, string yyyy, List<int> placeholderLengths, HashSet<string> results)
        {
            long total = 1;
            foreach (int len in placeholderLengths)
            {
                total *= (long)Math.Pow(10, len);
            }

            for (long i = 0; i < total; i++)
            {
                string currentPattern = pattern;
                long remaining = i;

                for (int p = 0; p < placeholderLengths.Count; p++)
                {
                    int placeholderLen = placeholderLengths[p];
                    long maxVal = (long)Math.Pow(10, placeholderLen);
                    long digitValue = remaining % maxVal;
                    remaining /= maxVal;
                    
                    string placeholder = new string('x', placeholderLen);
                    int xIndex = currentPattern.ToLower().IndexOf(placeholder);
                    if (xIndex >= 0)
                    {
                        string formatStr = new string('0', placeholderLen);
                        currentPattern = currentPattern.Substring(0, xIndex) + 
                                       digitValue.ToString(formatStr) + 
                                       currentPattern.Substring(xIndex + placeholderLen);
                    }
                }

                int targetLen = 16;
                // Adjust length rules:
                // - Cards starting with '30', '36', '38' -> 14 digits (Diners Club)
                // - Any other starting with '3' -> 15 digits (Amex and similar per user's requirement)
                // - Otherwise -> 16 digits
                if (currentPattern.StartsWith("30") || currentPattern.StartsWith("36") || currentPattern.StartsWith("38"))
                {
                    targetLen = 14;
                }
                else if (currentPattern.StartsWith("3"))
                {
                    targetLen = 15;
                }

                if (currentPattern.Length == targetLen - 1)
                {
                    int checkDigit = GetLuhnCheckDigit(currentPattern);
                    results.Add($"{currentPattern}{checkDigit}|{mm}|{yyyy}|000");
                }
                else if (currentPattern.Length == targetLen)
                {
                    string withoutLast = currentPattern.Substring(0, targetLen - 1);
                    int checkDigit = GetLuhnCheckDigit(withoutLast);
                    results.Add($"{withoutLast}{checkDigit}|{mm}|{yyyy}|000");
                }
            }
        }

        private void ShuffleList(List<string> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private int GetLuhnCheckDigit(string number)
        {
            int sum = 0;
            bool doubleDigit = true;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int digit = number[i] - '0';
                if (doubleDigit)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                doubleDigit = !doubleDigit;
            }
            return (10 - (sum % 10)) % 10;
        }

        private bool IsLuhnValid(string fullNumber)
        {
            if (string.IsNullOrEmpty(fullNumber) || fullNumber.Length < 2) return false;
            for (int i = 0; i < fullNumber.Length; i++)
            {
                char c = fullNumber[i];
                if (c < '0' || c > '9') return false;
            }
            int last = fullNumber[fullNumber.Length - 1] - '0';
            string prefix = fullNumber.Substring(0, fullNumber.Length - 1);
            return GetLuhnCheckDigit(prefix) == last;
        }
    }
}