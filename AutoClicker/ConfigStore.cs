using System;
using System.IO;

namespace AutoClicker
{
    // 配置文件读写:封装"我的文档\AutoClicker.ini"的存取
    // 格式:第一行=秒数,第二行=次数,第三行=缓冲秒数
    // 兼容老格式(单行频率 / 双行秒+次数)
    public class ConfigStore
    {
        public int Seconds { get; set; } = 1;   // 每多少秒
        public int Clicks { get; set; } = 10;   // 点击多少次
        public int Delay { get; set; } = 0;     // 缓冲秒数

        private readonly string cfgFile =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AutoClicker.ini");

        // 读取配置,失败静默降级到默认值
        public void Load()
        {
            try
            {
                if (!File.Exists(cfgFile))
                {
                    return;
                }
                string[] lines = File.ReadAllLines(cfgFile);
                if (lines.Length < 1 || !int.TryParse(lines[0].Trim(), out int first))
                {
                    return;
                }

                if (lines.Length >= 2 && int.TryParse(lines[1].Trim(), out int clk))
                {
                    // 新格式:第一行秒数,第二行次数
                    Seconds = Clamp(first, 1, 60);
                    Clicks = Clamp(clk, 1, 100);
                }
                else
                {
                    // 老单行格式:第一行频率,视为次数,秒数默认 1
                    Clicks = Clamp(first, 1, 100);
                }

                // 第三行:缓冲秒数,缺失就用默认 0
                if (lines.Length >= 3 && int.TryParse(lines[2].Trim(), out int dly))
                {
                    Delay = Clamp(dly, 0, 60);
                }
            }
            catch
            {
                // 静默降级,使用默认值
            }
        }

        // 保存配置,失败静默忽略
        public void Save()
        {
            try
            {
                File.WriteAllText(cfgFile, $"{Seconds}\r\n{Clicks}\r\n{Delay}");
            }
            catch
            {
                // 静默忽略写入错误
            }
        }

        // 把 value 限制在 [min, max] 范围内
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
