using System.Text;

namespace CoreDemo6
{
    class Program
    {
        static void Main(string[] args)
        {
            var dateTime = DateTime.Now;
            var dateTimeStr1 = "2026-03-05T16:25:39.85000013       ";
            var dateTimeStr2 = "20260305000002938701";
            var timeStr1 = AdjustTimeByTemplate(dateTimeStr1, dateTime);
            var timeStr2 = AdjustTimeByTemplate(dateTimeStr2, dateTime);
            Console.WriteLine($"[{dateTimeStr1}]\r\n[{timeStr1}]");
            Console.WriteLine($"[{dateTimeStr2}]\r\n[{timeStr2}]");

            // 注册编码提供程序，支持GB2312等编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            try
            {
                // 基准日期：2026-01-04 23:59:59
                DateTime baseDate = new DateTime(2026, 1, 4, 23, 59, 59);
                DateTime currentDate = DateTime.Now;

                // 计算时间差（总天数）
                TimeSpan difference = currentDate - baseDate;
                int totalDays = (int)Math.Abs(difference.TotalDays);

                // 判断单周还是双周
                // 第一周：0-6天，第二周：7-13天，依此类推
                int weekNumber = (totalDays / 7) + 1;
                bool isOddWeek = (weekNumber % 2) == 1;

                // 根据单双周确定执行任务
                string task = isOddWeek ? "大理简单兵圣" : "大理困难天波府";
                string scheduleLine = "22:25-23:40  执行  " + task;

                Console.WriteLine($"当前日期: {currentDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"基准日期: {baseDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"相隔天数: {totalDays} 天");
                Console.WriteLine($"第 {weekNumber} 周");
                Console.WriteLine($"单双周: {(isOddWeek ? "单周" : "双周")}");
                Console.WriteLine($"执行任务: {task}");
                Console.WriteLine();

                // 更新文件
                UpdateFile("C:\\天龙小蜜\\自定义\\周六.txt", scheduleLine);
                UpdateFile("C:\\天龙小蜜\\自定义\\周天.txt", scheduleLine);

                Console.WriteLine("文件更新完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static void UpdateFile(string filePath, string newLastLine)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"警告: 文件不存在 - {filePath}");
                return;
            }

            try
            {
                // 使用GB2312编码读取文件
                Encoding encoding = Encoding.GetEncoding(936);
                string[] lines = File.ReadAllLines(filePath, encoding);

                if (lines.Length > 0)
                {
                    // 替换最后一行
                    lines[lines.Length - 1] = newLastLine;

                    // 使用GB2312编码写回文件
                    File.WriteAllLines(filePath, lines, encoding);

                    Console.WriteLine($"已更新文件: {Path.GetFileName(filePath)}");
                    Console.WriteLine($"  新最后一行: {newLastLine}");
                }
                else
                {
                    Console.WriteLine($"警告: 文件为空 - {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新文件失败 {filePath}: {ex.Message}");
                throw;
            }
        }

        public static string AdjustTimeByTemplate(string timeStr, DateTime baseTime)
        {
            if (string.IsNullOrEmpty(timeStr))
                throw new ArgumentException("模板不能为空", nameof(timeStr));

            // 👇 先减 10 分钟
            DateTime adjustedTime = baseTime.AddMinutes(-10);

            string trimmed = timeStr.TrimEnd();
            int trailingSpaces = timeStr.Length - trimmed.Length;

            // 判断是否为紧凑20位格式
            if (trimmed.Length == 20 && IsAllDigits(trimmed))
            {
                return FormatCompact(adjustedTime);
            }

            // 否则为 ISO 格式：需要提取原始小数位数
            int originalFractionDigits = 7; // 默认7位

            int dotIndex = trimmed.IndexOf('.');
            if (dotIndex >= 0)
            {
                string fractionPart = trimmed.Substring(dotIndex + 1);
                // 只取数字部分（忽略可能的空格或非法字符）
                var digitCount = 0;
                foreach (char c in fractionPart)
                {
                    if (char.IsDigit(c)) digitCount++;
                    else break; // 遇到非数字停止（如空格）
                }
                if (digitCount > 0)
                    originalFractionDigits = digitCount;
            }

            // 格式化新时间的小数部分
            long ticksInSecond = adjustedTime.Ticks % 10_000_000L; // 0~9,999,999 (7位)
            string fractionStr = ticksInSecond.ToString("D7"); // 补零到7位

            // 按原始位数调整
            string finalFraction;
            if (originalFractionDigits <= 7)
            {
                finalFraction = fractionStr.Substring(0, originalFractionDigits);
            }
            else // originalFractionDigits == 8 (or more)
            {
                // 8位：7位标准 + 补一个'0'（因第8位无法从DateTime获取）
                finalFraction = fractionStr + "0";
                // 如果需要更复杂的逻辑（如保留原始最后一位），需额外参数
            }

            string result = $"{adjustedTime:yyyy-MM-ddTHH:mm:ss}.{finalFraction}";
            return result.PadRight(result.Length + trailingSpaces);
        }

        private static bool IsAllDigits(string s)
        {
            foreach (char c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        private static string FormatCompact(DateTime dt)
        {
            string date = dt.ToString("yyyyMMddHHmmss");
            int microseconds = (int)(dt.Ticks % 10_000_000L) / 10;
            return date + microseconds.ToString("D6");
        }
    }
}
