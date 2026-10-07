using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClicker
{
    // 鼠标连点器核心:封装点击模拟 + 定时器 + 缓冲倒计时,与 UI 解耦
    // 调用方设置 IntervalMs / DelaySeconds 后,Start() 即可;通过事件接收状态变化
    public class Clicker : IDisposable
    {
        // === Win32 API ===
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02; // 左键按下
        private const uint MOUSEEVENTF_LEFTUP = 0x04;   // 左键抬起

        // 间隔毫秒数,每秒 N 次就是 1000/N
        public int IntervalMs { get; set; } = 100;
        // 缓冲秒数,启动后等待这么久再开始连点,0 表示立即
        public int DelaySeconds { get; set; } = 0;
        // 是否正在运行(包括缓冲期间)
        public bool IsRunning => clickTimer.Enabled || delayTimer.Enabled;

        // === 事件,供 UI 订阅 ===
        public event EventHandler StateChanged;    // 启动/停止变化
        public event Action<int> DelayCountdown;  // 缓冲倒计时,参数是剩余秒数
        public event EventHandler Started;         // 缓冲结束,正式开始连点

        private readonly Timer clickTimer;          // 触发点击的定时器
        private readonly Timer delayTimer;          // 缓冲倒计时的定时器,每秒一次
        private int delayRemaining;                 // 缓冲剩余秒数

        public Clicker()
        {
            clickTimer = new Timer();
            clickTimer.Tick += (sender, e) => DoLeftClick();

            delayTimer = new Timer { Interval = 1000 };
            delayTimer.Tick += (sender, e) =>
            {
                delayRemaining--;
                if (delayRemaining <= 0)
                {
                    // 倒计时结束,启动连点
                    delayTimer.Stop();
                    clickTimer.Interval = IntervalMs;
                    clickTimer.Start();
                    Started?.Invoke(this, EventArgs.Empty);
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    DelayCountdown?.Invoke(delayRemaining);
                }
            };
        }

        // 启动:如果设了缓冲秒数,先倒计时;否则立即开始
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            if (DelaySeconds > 0)
            {
                delayRemaining = DelaySeconds;
                delayTimer.Start();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                clickTimer.Interval = IntervalMs;
                clickTimer.Start();
                Started?.Invoke(this, EventArgs.Empty);
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // 停止连点或取消缓冲
        public void Stop()
        {
            clickTimer.Stop();
            delayTimer.Stop();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        // 触发一次左键点击(按下 + 抬起 = 完整的一次单击)
        private void DoLeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        public void Dispose()
        {
            clickTimer.Stop();
            delayTimer.Stop();
        }
    }
}
