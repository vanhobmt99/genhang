using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ToolLuhnCore
{
    public static class CoreLogic
    {
        // --- Luhn & CC Logic ---

        public static int GetLuhnCheckDigit(string number)
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

        public static bool IsLuhnValid(string fullNumber)
        {
            if (string.IsNullOrEmpty(fullNumber) || fullNumber.Length < 2) return false;
            foreach (char c in fullNumber)
                if (c < '0' || c > '9') return false;
            int last = fullNumber[fullNumber.Length - 1] - '0';
            string prefix = fullNumber.Substring(0, fullNumber.Length - 1);
            return GetLuhnCheckDigit(prefix) == last;
        }

        public static void ShuffleList<T>(List<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void GenerateForPattern(string pattern, string mm, string yyyy, List<int> placeholderLengths, HashSet<string> results)
        {
            long total = 1;
            foreach (int len in placeholderLengths)
                total *= (long)Math.Pow(10, len);

            // Limit to reasonable amount per pattern to avoid freeze if x count is high
            // For this tool, let's max out at 100,000 per pattern for safety? 
            // Or just let it run. The original code let it run.
            
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

        // --- Formatting Logic ---

        public static string ApplyCardMask(string formattedLine, int maskDigits)
        {
            string[] parts = formattedLine.Split('|');
            if (parts.Length < 1) return formattedLine;

            string cardNum = parts[0];
            if (cardNum.Length < maskDigits) return formattedLine;

            string masked = cardNum.Substring(0, cardNum.Length - maskDigits) + new string('x', maskDigits);
            parts[0] = masked;

            return string.Join("|", parts);
        }

        public static string CleanAndFormatLine(string line)
        {
            line = line.Replace(" ", "");
            Match cardMatch = Regex.Match(line, @"(\d{15,16})");
            if (!cardMatch.Success) return "";
            string cardNum = cardMatch.Groups[1].Value;

            string rest = line.Replace(cardNum, "|");
            string mm = "";
            string yyyy = "";
            
            // Pattern 1: MM/YY or MM/YYYY
            Match expMatch = Regex.Match(rest, @"(\d{1,2})/(\d{2,4})");
            if (expMatch.Success)
            {
                mm = expMatch.Groups[1].Value.PadLeft(2, '0');
                string yearPart = expMatch.Groups[2].Value;
                if (yearPart.Length == 2) yyyy = "20" + yearPart;
                else yyyy = yearPart;
            }
            else
            {
                MatchCollection nums = Regex.Matches(rest, @"\d+");
                List<string> numList = new List<string>();
                foreach (Match m in nums) numList.Add(m.Value);

                if (numList.Count >= 2)
                {
                    string possibleMM = numList[0];
                    if (possibleMM.Length <= 2 && int.TryParse(possibleMM, out int mmVal) && mmVal >= 1 && mmVal <= 12)
                    {
                        mm = possibleMM.PadLeft(2, '0');
                        string possibleYY = numList[1];
                        if (possibleYY.Length == 2) yyyy = "20" + possibleYY;
                        else if (possibleYY.Length == 4) yyyy = possibleYY;
                        else if (possibleYY.Length == 1) yyyy = "202" + possibleYY;
                    }
                }
            }

            if (string.IsNullOrEmpty(mm) || string.IsNullOrEmpty(yyyy)) return "";
            if (!int.TryParse(mm, out int month) || month < 1 || month > 12) return "";
            if (!int.TryParse(yyyy, out int year) || year < 2020 || year > 2040) return "";

            return $"{cardNum}|{mm}|{yyyy}";
        }

        public static string GetFormatErrorReason(string line)
        {
            Match cardMatch = Regex.Match(line.Replace(" ", ""), @"(\d{15,16})");
            if (!cardMatch.Success)
            {
                int digitCount = line.Count(char.IsDigit);
                if (digitCount < 15) return $"Số thẻ quá ngắn ({digitCount} số, cần 15-16)";
                return "Không tìm thấy số thẻ hợp lệ";
            }

            string cardNum = cardMatch.Groups[1].Value;
            string rest = line.Replace(" ", "").Replace(cardNum, "|");

            if (!Regex.IsMatch(rest, @"\d{1,2}[/|]\d{2,4}"))
            {
                var nums = Regex.Matches(rest, @"\d+");
                if (nums.Count < 2) return "Không tìm thấy ngày hết hạn (MM/YY)";
            }

            return "Không xác định được định dạng ngày hết hạn";
        }
    }
}
