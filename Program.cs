using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

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
        private TabControl tabControl;

        // Tab 1: Generate
        private TextBox txtPatterns;
        private Button btnRun, btnCopyGenerated;
        private ProgressBar progressBar;
        private List<string> lastGeneratedCards = new List<string>();

        // Tab 2: Remove Duplicates
        private TextBox txtFileA, txtFileB, txtSplit;
        private Button btnFileA, btnFileB, btnProcess;
        private Label lblResult;
        private string fileA = "", fileB = "";
        private FlowLayoutPanel panelCopyButtons;
        private List<string> lastOutputFiles = new List<string>();

        // Tab 3: Log
        private TextBox txtLog;

        // Tab 4: Format
        private TextBox txtFormatInput, txtFormatOutput, txtFormatErrors;
        private Button btnFormat, btnCopyResult;
        private Label lblFormatResult;
        private ComboBox cboMask;

        public MainForm()
        {
            this.Text = "CC Tools";
            this.Size = new Size(750, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9F);

            // Load icon
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                    this.Icon = new Icon(icoPath);
            }
            catch { }

            // TabControl
            tabControl = new TabControl()
            {
                Top = 8, Left = 8, Width = 726, Height = 470
            };

            // Tab 1: Generate
            TabPage tabGenerate = new TabPage("Generate");
            SetupGenerateTab(tabGenerate);

            // Tab 2: Remove Duplicates
            TabPage tabRemove = new TabPage("Remove Dup");
            SetupRemoveTab(tabRemove);

            // Tab 3: Format
            TabPage tabFormat = new TabPage("Format");
            SetupFormatTab(tabFormat);

            // Tab 4: Log
            TabPage tabLog = new TabPage("Log");
            SetupLogTab(tabLog);

            tabControl.TabPages.Add(tabGenerate);
            tabControl.TabPages.Add(tabRemove);
            tabControl.TabPages.Add(tabFormat);
            tabControl.TabPages.Add(tabLog);

            this.Controls.Add(tabControl);
        }

        private void SetupGenerateTab(TabPage tab)
        {
            Label lbl = new Label()
            {
                Text = "Nhập patterns (BINxxxx|MM|YYYY|):",
                Top = 10, Left = 10, Width = 300
            };

            txtPatterns = new TextBox()
            {
                Top = 32, Left = 10, Width = 690, Height = 330,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10F),
                Text = "497465880504xxxx|03|2027|\n49740180558xxxxx|05|2028|"
            };

            btnRun = new Button()
            {
                Text = "Generate",
                Top = 370, Left = 10, Width = 420, Height = 35
            };
            btnRun.Click += BtnRun_Click;

            btnCopyGenerated = new Button()
            {
                Text = "📋 Copy",
                Top = 370, Left = 440, Width = 100, Height = 35,
                Enabled = false
            };
            btnCopyGenerated.Click += (s, e) => {
                if (lastGeneratedCards.Count > 0)
                {
                    Clipboard.SetText(string.Join(Environment.NewLine, lastGeneratedCards));
                    MessageBox.Show($"Đã copy {lastGeneratedCards.Count} thẻ vào clipboard!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Button btnCleanup = new Button()
            {
                Text = "🗑️ Dọn dẹp",
                Top = 370, Left = 550, Width = 150, Height = 35,
                BackColor = Color.MistyRose
            };
            btnCleanup.Click += BtnCleanup_Click;

            progressBar = new ProgressBar()
            {
                Top = 410, Left = 10, Width = 690, Height = 22
            };

            tab.Controls.AddRange(new Control[] { lbl, txtPatterns, btnRun, btnCopyGenerated, btnCleanup, progressBar });
        }

        private void SetupRemoveTab(TabPage tab)
        {
            // File A
            Label lblA = new Label() { Text = "File A (bắt buộc):", Top = 20, Left = 10, Width = 110 };
            txtFileA = new TextBox() { Top = 17, Left = 125, Width = 500, ReadOnly = true };
            btnFileA = new Button() { Text = "...", Top = 15, Left = 635, Width = 50, Height = 26 };
            btnFileA.Click += (s, e) => { fileA = PickFile("Chọn file A") ?? ""; txtFileA.Text = fileA; };

            // File B
            Label lblB = new Label() { Text = "File B (tùy chọn):", Top = 60, Left = 10, Width = 110 };
            txtFileB = new TextBox() { Top = 57, Left = 125, Width = 500, ReadOnly = true };
            btnFileB = new Button() { Text = "...", Top = 55, Left = 635, Width = 50, Height = 26 };
            btnFileB.Click += (s, e) => { fileB = PickFile("Chọn file B") ?? ""; txtFileB.Text = fileB; };

            // Split
            Label lblSplit = new Label() { Text = "Chia thành:", Top = 105, Left = 10, Width = 70 };
            txtSplit = new TextBox() { Top = 102, Left = 85, Width = 50, Text = "1" };
            Label lblSplit2 = new Label() { Text = "file", Top = 105, Left = 140, Width = 30 };

            // Process button
            btnProcess = new Button()
            {
                Text = "Xử lý (A - B hoặc chỉ chia file A)",
                Top = 150, Left = 10, Width = 680, Height = 38
            };
            btnProcess.Click += ProcessClick;

            // Result
            lblResult = new Label()
            {
                Text = "",
                Top = 200, Left = 10, Width = 690, Height = 100,
                Font = new Font("Consolas", 9.5F)
            };

            // Panel for copy buttons (dynamic)
            panelCopyButtons = new FlowLayoutPanel()
            {
                Top = 310, Left = 10, Width = 690, Height = 120,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            tab.Controls.AddRange(new Control[] { lblA, txtFileA, btnFileA, lblB, txtFileB, btnFileB, lblSplit, txtSplit, lblSplit2, btnProcess, lblResult, panelCopyButtons });
        }

        private void SetupLogTab(TabPage tab)
        {
            Label lbl = new Label() { Text = "Lịch sử Generate:", Top = 8, Left = 10, Width = 150 };
            
            Button btnClear = new Button()
            {
                Text = "Xóa Log",
                Top = 5, Left = 610, Width = 90
            };
            btnClear.Click += (s, e) => { txtLog.Clear(); };

            txtLog = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Top = 35, Left = 10, Width = 690, Height = 395,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5F),
                WordWrap = false
            };
            
            tab.Controls.AddRange(new Control[] { lbl, btnClear, txtLog });
        }

        private void SetupFormatTab(TabPage tab)
        {
            Label lblInput = new Label() { Text = "Dán dữ liệu lộn xộn vào đây:", Top = 8, Left = 10, Width = 200 };
            
            txtFormatInput = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Top = 30, Left = 10, Width = 340, Height = 200,
                Font = new Font("Consolas", 9.5F),
                WordWrap = false
            };

            Label lblOutput = new Label() { Text = "Kết quả (num|mm|yyyy):", Top = 8, Left = 360, Width = 200 };
            txtFormatOutput = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Top = 30, Left = 360, Width = 340, Height = 200,
                Font = new Font("Consolas", 9.5F),
                ReadOnly = true,
                WordWrap = false
            };

            // Mask option
            Label lblMask = new Label() { Text = "Mask số cuối:", Top = 240, Left = 10, Width = 85, Height = 23 };
            cboMask = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Top = 237, Left = 100, Width = 110, Height = 25
            };
            cboMask.Items.AddRange(new object[] { "Không mask", "4 số (xxxx)", "5 số (xxxxx)", "6 số (xxxxxx)" });
            cboMask.SelectedIndex = 0;

            btnFormat = new Button()
            {
                Text = "Format & Clean",
                Top = 235, Left = 220, Width = 150, Height = 30
            };
            btnFormat.Click += BtnFormat_Click;

            btnCopyResult = new Button()
            {
                Text = "📋 Copy kết quả",
                Top = 235, Left = 380, Width = 150, Height = 30
            };
            btnCopyResult.Click += (s, e) => {
                if (!string.IsNullOrWhiteSpace(txtFormatOutput.Text))
                {
                    Clipboard.SetText(txtFormatOutput.Text);
                    MessageBox.Show($"Đã copy {txtFormatOutput.Lines.Length} dòng vào clipboard!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Không có kết quả để copy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            lblFormatResult = new Label()
            {
                Text = "",
                Top = 272, Left = 10, Width = 690, Height = 20,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // Error log
            Label lblErrors = new Label() { Text = "Chi tiết lỗi (dòng không parse được):", Top = 295, Left = 10, Width = 250 };
            txtFormatErrors = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Top = 318, Left = 10, Width = 690, Height = 110,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                WordWrap = false,
                ForeColor = Color.DarkRed
            };

            tab.Controls.AddRange(new Control[] { lblInput, txtFormatInput, lblOutput, txtFormatOutput, lblMask, cboMask, btnFormat, btnCopyResult, lblFormatResult, lblErrors, txtFormatErrors });
        }

        private void BtnCleanup_Click(object? sender, EventArgs e)
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            string[] patterns = { "cards.txt", "errors.txt", "output*.txt" };
            var files = new List<string>();
            
            foreach (var pattern in patterns)
            {
                try { files.AddRange(Directory.GetFiles(appFolder, pattern)); } catch { }
            }

            if (files.Count == 0)
            {
                MessageBox.Show("Không có file nào để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Show confirmation with file list
            string fileList = string.Join("\n", files.Select(f => $"• {Path.GetFileName(f)}"));
            var result = MessageBox.Show(
                $"Tìm thấy {files.Count} file:\n\n{fileList}\n\nXóa tất cả?", 
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                int deleted = 0;
                foreach (var f in files)
                {
                    try { File.Delete(f); deleted++; } catch { }
                }
                
                // Clear related UI
                lastGeneratedCards.Clear();
                btnCopyGenerated.Enabled = false;
                lastOutputFiles.Clear();
                panelCopyButtons.Controls.Clear();
                
                MessageBox.Show($"Đã xóa {deleted} file!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnFormat_Click(object? sender, EventArgs e)
        {
            string input = txtFormatInput.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Vui lòng dán dữ liệu cần format!");
                return;
            }

            // Get mask option: 0=none, 1=4 digits, 2=5 digits, 3=6 digits
            int maskDigits = cboMask.SelectedIndex switch
            {
                1 => 4,
                2 => 5,
                3 => 6,
                _ => 0
            };

            string[] lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> results = new List<string>();
            List<string> errors = new List<string>();
            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                string cleaned = CleanAndFormatLine(line);
                if (!string.IsNullOrEmpty(cleaned))
                {
                    // Apply mask if selected
                    if (maskDigits > 0)
                    {
                        cleaned = ApplyCardMask(cleaned, maskDigits);
                    }
                    results.Add(cleaned);
                    successCount++;
                }
                else
                {
                    failCount++;
                    string reason = GetFormatErrorReason(line);
                    errors.Add($"Dòng {i + 1}: {reason}");
                    errors.Add($"   └─ \"{(line.Length > 60 ? line.Substring(0, 60) + "..." : line)}\"");
                }
            }

            txtFormatOutput.Text = string.Join(Environment.NewLine, results);
            txtFormatErrors.Text = errors.Count > 0 ? string.Join(Environment.NewLine, errors) : "Không có lỗi!";
            lblFormatResult.Text = $"✅ Thành công: {successCount}  |  ❌ Thất bại: {failCount}  |  Tổng: {lines.Length} dòng";
            lblFormatResult.ForeColor = failCount > 0 ? Color.OrangeRed : Color.Green;
        }

        private string CleanAndFormatLine(string line)
        {
            // Remove all whitespace
            line = line.Replace(" ", "");

            // Extract card number (15-16 digits)
            Match cardMatch = Regex.Match(line, @"(\d{15,16})");
            if (!cardMatch.Success) return "";
            string cardNum = cardMatch.Groups[1].Value;

            // Remove card number from line to parse rest
            string rest = line.Replace(cardNum, "|");

            // Try to extract expiry and CVV
            string mm = "";
            string yyyy = "";
            string cvv = "";

            // Pattern 1: MM/YY or MM/YYYY (e.g., 12/26 or 12/2026)
            Match expMatch = Regex.Match(rest, @"(\d{1,2})/(\d{2,4})");
            if (expMatch.Success)
            {
                mm = expMatch.Groups[1].Value.PadLeft(2, '0');
                string yearPart = expMatch.Groups[2].Value;
                if (yearPart.Length == 2)
                    yyyy = "20" + yearPart;
                else
                    yyyy = yearPart;
                
                // Remove matched part to find CVV
                rest = rest.Replace(expMatch.Value, "|");
            }
            else
            {
                // Pattern 2: Separated by | like: |MM|YY| or |MM|YYYY|
                // Find all number groups
                MatchCollection nums = Regex.Matches(rest, @"\d+");
                List<string> numList = new List<string>();
                foreach (Match m in nums)
                    numList.Add(m.Value);

                if (numList.Count >= 2)
                {
                    // First should be month (1-12)
                    string possibleMM = numList[0];
                    if (possibleMM.Length <= 2 && int.TryParse(possibleMM, out int mmVal) && mmVal >= 1 && mmVal <= 12)
                    {
                        mm = possibleMM.PadLeft(2, '0');
                        
                        // Second should be year
                        string possibleYY = numList[1];
                        if (possibleYY.Length == 2)
                            yyyy = "20" + possibleYY;
                        else if (possibleYY.Length == 4)
                            yyyy = possibleYY;
                        else if (possibleYY.Length == 1) // Single digit year like "7" for 2027
                            yyyy = "202" + possibleYY;
                        
                        // Third (if exists) is CVV
                        if (numList.Count >= 3)
                            cvv = numList[2];
                    }
                }
            }

            // Validate we have required fields
            if (string.IsNullOrEmpty(mm) || string.IsNullOrEmpty(yyyy))
                return "";

            // Validate month
            if (!int.TryParse(mm, out int month) || month < 1 || month > 12)
                return "";

            // Validate year (2020-2040 range)
            if (!int.TryParse(yyyy, out int year) || year < 2020 || year > 2040)
                return "";

            return $"{cardNum}|{mm}|{yyyy}";
        }

        private string GetFormatErrorReason(string line)
        {
            // Check for card number
            Match cardMatch = Regex.Match(line.Replace(" ", ""), @"(\d{15,16})");
            if (!cardMatch.Success)
            {
                // Count digits
                int digitCount = line.Count(char.IsDigit);
                if (digitCount < 15)
                    return $"Số thẻ quá ngắn (chỉ có {digitCount} chữ số, cần 15-16)";
                else if (digitCount > 16 && !Regex.IsMatch(line, @"\d{15,16}"))
                    return "Không tìm thấy số thẻ 15-16 chữ số liên tiếp";
                return "Không tìm thấy số thẻ hợp lệ";
            }

            string cardNum = cardMatch.Groups[1].Value;
            string rest = line.Replace(" ", "").Replace(cardNum, "|");

            // Check expiry
            Match expMatch = Regex.Match(rest, @"(\d{1,2})/(\d{2,4})");
            if (expMatch.Success)
            {
                string mm = expMatch.Groups[1].Value;
                string yy = expMatch.Groups[2].Value;
                if (int.TryParse(mm, out int month) && (month < 1 || month > 12))
                    return $"Tháng không hợp lệ: {mm} (phải từ 01-12)";
                if (yy.Length == 4 && int.TryParse(yy, out int year) && (year < 2020 || year > 2040))
                    return $"Năm ngoài phạm vi: {yy} (phải từ 2020-2040)";
                if (yy.Length == 2)
                {
                    int fullYear = 2000 + int.Parse(yy);
                    if (fullYear < 2020 || fullYear > 2040)
                        return $"Năm ngoài phạm vi: 20{yy} (phải từ 2020-2040)";
                }
            }

            // Check if has any date-like pattern
            if (!Regex.IsMatch(rest, @"\d{1,2}[/|]\d{2,4}"))
            {
                var nums = Regex.Matches(rest, @"\d+").Cast<Match>().Select(m => m.Value).ToList();
                if (nums.Count < 2)
                    return "Không tìm thấy ngày hết hạn (cần MM/YY hoặc MM|YYYY)";
                
                // Check if first num could be month
                if (nums.Count >= 1)
                {
                    if (!int.TryParse(nums[0], out int m) || m < 1 || m > 12)
                        return $"Tháng không hợp lệ: {nums[0]} (phải từ 01-12)";
                }
                if (nums.Count >= 2)
                {
                    string yearStr = nums[1];
                    if (yearStr.Length == 1)
                        return $"Năm chỉ có 1 chữ số: {yearStr} (định dạng không rõ ràng)";
                }
            }

            return "Không xác định được định dạng ngày hết hạn";
        }

        private string ApplyCardMask(string formattedLine, int maskDigits)
        {
            // Format: cardNum|mm|yyyy
            string[] parts = formattedLine.Split('|');
            if (parts.Length < 1) return formattedLine;

            string cardNum = parts[0];
            if (cardNum.Length < maskDigits) return formattedLine;

            // Replace last N digits with 'x'
            string masked = cardNum.Substring(0, cardNum.Length - maskDigits) + new string('x', maskDigits);
            parts[0] = masked;

            return string.Join("|", parts);
        }

        private string? PickFile(string title)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = title;
                dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK) return dlg.FileName;
            }
            return null;
        }

        private void ProcessClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fileA))
            {
                MessageBox.Show("Vui lòng chọn file A!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Read file A and count original lines
            string[] linesA = File.ReadAllLines(fileA);
            int originalCount = linesA.Length;
            
            // Use HashSet to automatically remove duplicates within file A
            var setA = new HashSet<string>(linesA);
            int afterDedupA = setA.Count;
            int dupInA = originalCount - afterDedupA;
            
            // If file B is selected, remove lines that exist in B
            int removedByB = 0;
            if (!string.IsNullOrEmpty(fileB) && File.Exists(fileB))
            {
                var setB = new HashSet<string>(File.ReadAllLines(fileB));
                int beforeExcept = setA.Count;
                setA.ExceptWith(setB);
                removedByB = beforeExcept - setA.Count;
            }
            
            var result = setA.ToList();

            int splitCount = 1;
            int.TryParse(txtSplit.Text, out splitCount);
            if (splitCount < 1) splitCount = 1;

            string folder = Path.GetDirectoryName(fileA) ?? "";
            string mode = string.IsNullOrEmpty(fileB) ? "Chia file" : "A - B";

            // Build stats string
            string stats = $"📊 File A: {originalCount:N0} dòng";
            if (dupInA > 0) stats += $" (loại {dupInA:N0} trùng)";
            if (removedByB > 0) stats += $"\n📊 Loại bởi B: {removedByB:N0} dòng";
            stats += $"\n📊 Kết quả: {result.Count:N0} dòng";

            // Clear previous copy buttons
            panelCopyButtons.Controls.Clear();
            lastOutputFiles.Clear();

            if (splitCount == 1)
            {
                string output = Path.Combine(folder, "output.txt");
                File.WriteAllLines(output, result);
                lblResult.Text = $"✅ [{mode}]\n{stats}\n\n💾 Đã lưu vào: {Path.GetFileName(output)}";
                
                // Add copy button
                lastOutputFiles.Add(output);
                AddCopyButton("📋 Copy output.txt", output);
            }
            else
            {
                int perFile = (result.Count + splitCount - 1) / splitCount;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < splitCount; i++)
                {
                    var chunk = result.Skip(i * perFile).Take(perFile).ToList();
                    if (chunk.Count > 0)
                    {
                        string output = Path.Combine(folder, $"output_{i + 1}.txt");
                        File.WriteAllLines(output, chunk);
                        sb.AppendLine($"  File {i + 1}: {chunk.Count:N0} dòng");
                        
                        // Add copy button for each file
                        lastOutputFiles.Add(output);
                        AddCopyButton($"📋 Copy file {i + 1} ({chunk.Count:N0})", output);
                    }
                }
                lblResult.Text = $"✅ [{mode}]\n{stats}\n\n💾 Đã chia thành {splitCount} file:\n{sb}";
            }
        }

        private void AddCopyButton(string text, string filePath)
        {
            Button btn = new Button()
            {
                Text = text,
                Width = 165,
                Height = 32,
                Margin = new Padding(3),
                Tag = filePath
            };
            btn.Click += (s, e) =>
            {
                string path = (string)((Button)s!).Tag!;
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    int lineCount = File.ReadAllLines(path).Length;
                    Clipboard.SetText(content);
                    MessageBox.Show($"Đã copy {lineCount:N0} dòng từ\n{Path.GetFileName(path)}\nvào clipboard!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("File không tồn tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            panelCopyButtons.Controls.Add(btn);
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
                    Invoke(() => MessageBox.Show("Lỗi: " + ex.Message));
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
            List<string> processedBins = new List<string>();
            int parseErrorCount = 0;

            int totalPatterns = lines.Length;
            int processedPatterns = 0;

            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                processedPatterns++;
                Invoke(() => { progressBar.Value = (processedPatterns * 30) / totalPatterns; });

                string pattern = line.Trim();
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: dòng trống.");
                    continue;
                }

                string cardPattern;
                string mm = "01";
                string yyyy = "2028";

                string[] parts = pattern.Split('|');
                if (parts.Length < 3)
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: '{pattern}' - Thiếu MM|YYYY.");
                    continue;
                }

                cardPattern = parts[0].Trim();
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    mm = parts[1].Trim().PadLeft(2, '0');
                if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
                    yyyy = parts[2].Trim();

                // Validate MM
                if (mm.Length != 2 || !int.TryParse(mm, out int mmVal) || mmVal < 1 || mmVal > 12)
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: MM không hợp lệ: '{mm}'.");
                    continue;
                }

                // Validate YYYY
                if (yyyy.Length != 4 || !int.TryParse(yyyy, out _))
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: YYYY không hợp lệ: '{yyyy}'.");
                    continue;
                }

                // Validate pattern chars
                bool patternOk = true;
                foreach (char c in cardPattern)
                {
                    if (!((c >= '0' && c <= '9') || c == 'x' || c == 'X'))
                    {
                        patternOk = false;
                        break;
                    }
                }
                if (!patternOk)
                {
                    parseErrorCount++;
                    errorDetails.Add($"Dòng {li + 1}: Chỉ cho phép số và 'x'.");
                    continue;
                }

                // Find placeholders
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
                    errorDetails.Add($"Dòng {li + 1}: Không có 'x' placeholder.");
                    continue;
                }

                // Store BIN info
                string binPrefix = cardPattern.ToUpper().Split('X')[0];
                long combinations = 1;
                foreach (int len in placeholderLengths)
                    combinations *= (long)Math.Pow(10, len);
                processedBins.Add($"{binPrefix}... ({combinations:N0})");

                GenerateForPattern(cardPattern, mm, yyyy, placeholderLengths, allResults);
            }

            // Shuffle
            Invoke(() => { progressBar.Value = 70; });
            List<string> finalResults = new List<string>(allResults);
            ShuffleList(finalResults);

            // Luhn validation
            Invoke(() => { progressBar.Value = 80; });
            List<string> validResults = new List<string>();
            int invalidCount = 0;
            foreach (var line in finalResults)
            {
                var parts = line.Split('|');
                var num = parts.Length > 0 ? parts[0] : "";
                if (!string.IsNullOrEmpty(num) && IsLuhnValid(num))
                    validResults.Add(line);
                else
                    invalidCount++;
            }

            // Write output
            Invoke(() => { progressBar.Value = 90; });
            using (StreamWriter sw = new StreamWriter("cards.txt", false, Encoding.UTF8))
            {
                foreach (var card in validResults)
                {
                    if (!string.IsNullOrWhiteSpace(card))
                        sw.WriteLine(card);
                }
            }

            // Write errors
            using (StreamWriter err = new StreamWriter("errors.txt", false, Encoding.UTF8))
            {
                err.WriteLine($"==== Báo cáo lỗi ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ====");
                err.WriteLine($"Patterns: {totalPatterns}, Hợp lệ: {validResults.Count}, Loại Luhn: {invalidCount}, Lỗi: {parseErrorCount}");
                if (errorDetails.Count > 0)
                {
                    err.WriteLine("\n-- Chi tiết lỗi --");
                    foreach (var e in errorDetails)
                        err.WriteLine(e);
                }
            }

            // Show result
            Invoke(() =>
            {
                progressBar.Value = 100;

                // Save results for copy
                lastGeneratedCards = new List<string>(validResults);
                btnCopyGenerated.Enabled = validResults.Count > 0;

                // Log
                string binsInfo = processedBins.Count > 0 ? string.Join(", ", processedBins) : "N/A";
                string logEntry = $"[{DateTime.Now:HH:mm:ss}] BIN: {binsInfo} | Kết quả: {validResults.Count:N0} hợp lệ";
                if (txtLog.Text.Length > 0)
                    txtLog.AppendText(Environment.NewLine + logEntry);
                else
                    txtLog.AppendText(logEntry);

                string msg = $"Hoàn tất!\n\nHợp lệ: {validResults.Count:N0}\nLoại Luhn: {invalidCount:N0}\nLỗi: {parseErrorCount:N0}";
                MessageBox.Show(msg, "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private void GenerateForPattern(string pattern, string mm, string yyyy, List<int> placeholderLengths, HashSet<string> results)
        {
            long total = 1;
            foreach (int len in placeholderLengths)
                total *= (long)Math.Pow(10, len);

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

                // Card length rules
                int targetLen = 16;
                if (currentPattern.StartsWith("30") || currentPattern.StartsWith("36") || currentPattern.StartsWith("38"))
                    targetLen = 14;
                else if (currentPattern.StartsWith("3"))
                    targetLen = 15;

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
            foreach (char c in fullNumber)
                if (c < '0' || c > '9') return false;
            int last = fullNumber[fullNumber.Length - 1] - '0';
            string prefix = fullNumber.Substring(0, fullNumber.Length - 1);
            return GetLuhnCheckDigit(prefix) == last;
        }
    }
}
