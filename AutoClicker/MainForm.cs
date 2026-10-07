using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClicker
{
    // 主窗体:负责 UI 控件和事件编排
    // 业务逻辑(点击 + 定时 + 缓冲)由 Clicker 类承担
    // 配置持久化由 ConfigStore 类承担
    public class MainForm : Form
    {
        // 深色主题颜色
        private readonly Color bg = Color.FromArgb(32, 32, 32);   // 窗体和控件背景
        private readonly Color bgBtn = Color.FromArgb(45, 45, 48); // 按钮稍浅一点
        private readonly Color fg = Color.GhostWhite;            // 文字浅白

        // 业务对象
        private readonly Clicker clicker = new();
        private readonly ConfigStore config = new();

        // 热键相关(WndProc 必须在 Form 里重写,所以热键注册留在窗体)
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 0x233;
        private const Keys HOTKEY_KEY = Keys.F1;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // === 控件 ===
        private Label lblEvery, lblSep, lblTimes, lblBuf1, lblBuf2, lblHotkey, lblStatus;
        private NumericUpDown numSeconds, numClicks, numDelay;
        private Button btnToggle;

        public MainForm()
        {
            InitUI();
            BindClickerEvents();
            LoadConfig();
            RegisterGlobalHotkey();
        }

        // 创建所有控件并摆放到合适位置
        private void InitUI()
        {
            // 窗体本身
            Text = "简洁鼠标连点器";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(320, 220);
            BackColor = bg;
            ForeColor = fg;

            // === 第一行:每 X 秒点击 Y 次 ===
            lblEvery = MakeLabel("每", new Point(20, 22));
            Controls.Add(lblEvery);

            numSeconds = MakeNumeric(1, 60, 1, new Point(40, 18), 50);
            numSeconds.ValueChanged += (sender, e) => UpdateIntervalFromInputs();
            Controls.Add(numSeconds);

            lblSep = MakeLabel("秒点击", new Point(95, 22));
            Controls.Add(lblSep);

            numClicks = MakeNumeric(1, 100, 10, new Point(140, 18), 55);
            numClicks.ValueChanged += (sender, e) => UpdateIntervalFromInputs();
            Controls.Add(numClicks);

            lblTimes = MakeLabel("次", new Point(200, 22));
            Controls.Add(lblTimes);

            btnToggle = new Button
            {
                Text = "开始",
                Location = new Point(230, 16),
                Size = new Size(70, 28),
                BackColor  = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
            };
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.Click += (sender, e) => Toggle();
            Controls.Add(btnToggle);

            // === 第二行:缓冲 X 秒后启动 ===
            lblBuf1 = MakeLabel("缓冲", new Point(20, 62));
            Controls.Add(lblBuf1);

            numDelay = MakeNumeric(0, 60, 0, new Point(60, 58), 50);
            numDelay.ValueChanged += (sender, e) => clicker.DelaySeconds = (int)numDelay.Value;
            Controls.Add(numDelay);

            lblBuf2 = MakeLabel("秒后启动(0 表示立即)", new Point(120, 62));
            Controls.Add(lblBuf2);

            // === 第三行:快捷键说明 ===
            lblHotkey = MakeLabel("快捷键 F1 启停(全局)", new Point(20, 100));
            Controls.Add(lblHotkey);

            // === 第四行:状态显示 ===
            lblStatus = MakeLabel("状态:已停止", new Point(20, 140));
            Controls.Add(lblStatus);
        }

        // 工厂方法:统一深色风格的 Label
        private Label MakeLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Location = location,
                AutoSize = true,
                BackColor = bg,
                ForeColor = fg,
            };
        }

        // 工厂方法:统一深色风格的 NumericUpDown
        private NumericUpDown MakeNumeric(int min, int max, int value, Point location, int width)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Location = location,
                Width = width,
                BackColor = bg,
                ForeColor = fg,
            };
        }

        // 订阅 Clicker 事件:UI 跟随业务状态变化
        private void BindClickerEvents()
        {
            clicker.StateChanged += (sender, e) => UpdateButtonState();
            clicker.DelayCountdown += remaining => lblStatus.Text = $"{remaining} 秒后开始...";
            clicker.Started += (sender, e) => lblStatus.Text = "状态:运行中...";
        }

        // 根据 clicker.IsRunning 更新按钮文字和状态
        private void UpdateButtonState()
        {
            if (clicker.IsRunning)
            {
                btnToggle.Text = "停止";
                // 状态文字由 Started / DelayCountdown 事件单独设置,这里不覆盖
            }
            else
            {
                btnToggle.Text = "开始";
                lblStatus.Text = "状态:已停止";
            }
        }

        // 点击按钮或按 F1:在运行就停止,否则启动
        private void Toggle()
        {
            if (clicker.IsRunning)
            {
                clicker.Stop();
            }
            else
            {
                clicker.Start();
            }
        }

        // 根据输入框的秒数和次数计算并同步到 clicker.IntervalMs
        private void UpdateIntervalFromInputs()
        {
            int seconds = (int)numSeconds.Value;
            int clicks = (int)numClicks.Value;
            if (seconds < 1)
            {
                seconds = 1;
            }
            if (clicks < 1)
            {
                clicks = 1;
            }
            clicker.IntervalMs = (seconds * 1000) / clicks;
        }

        // 注册全局热键 F1,失败则提示
        private void RegisterGlobalHotkey()
        {
            if (!RegisterHotKey(Handle, HOTKEY_ID, 0, (uint)HOTKEY_KEY))
            {
                MessageBox.Show($"热键 {HOTKEY_KEY} 注册失败,可能已被其他程序占用。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 拦截窗体消息,接收全局热键
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && (int)m.WParam == HOTKEY_ID)
            {
                Toggle();
            }
            base.WndProc(ref m);
        }

        // 窗体句柄销毁时反注册热键
        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            base.OnHandleDestroyed(e);
        }

        // 窗体关闭时停止连点并保存配置
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            clicker.Stop();
            SaveConfig();
            base.OnFormClosing(e);
        }

        // 把 config 加载到输入框,再同步到 clicker
        private void LoadConfig()
        {
            config.Load();
            numSeconds.Value = config.Seconds;
            numClicks.Value = config.Clicks;
            numDelay.Value = config.Delay;
            UpdateIntervalFromInputs();
            clicker.DelaySeconds = config.Delay;
        }

        // 把输入框值写回 config 并保存
        private void SaveConfig()
        {
            config.Seconds = (int)numSeconds.Value;
            config.Clicks = (int)numClicks.Value;
            config.Delay = (int)numDelay.Value;
            config.Save();
        }
    }
}
